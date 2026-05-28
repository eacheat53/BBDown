using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using BBDown.Core;
using BBDown.Core.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
namespace BBDown;

public class BBDownApiServer
{
    private WebApplication? app;
    private readonly object _taskLock = new();
    private readonly List<DownloadTask> runningTasks = [];
    private readonly List<DownloadTask> finishedTasks = [];
    private readonly SemaphoreSlim _concurrencyLimiter = new(3, 3); // max 3 concurrent downloads

    public void SetUpServer()
    {
        if (app is not null) return;
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.ConfigureHttpJsonOptions((options) =>
        {
            options.SerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine(options.SerializerOptions.TypeInfoResolver, AppJsonSerializerContext.Default);
        });
        builder.Services.AddCors((options) =>
        {
            options.AddPolicy("AllowAnyOrigin",
                policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
        });
        app = builder.Build();
        app.UseCors("AllowAnyOrigin");
        var taskStatusApi = app.MapGroup("/get-tasks");
        taskStatusApi.MapGet("/", handler: () =>
        {
            lock (_taskLock)
            {
                return Results.Json(new DownloadTaskCollection(runningTasks, finishedTasks), AppJsonSerializerContext.Default.DownloadTaskCollection);
            }
        });
        taskStatusApi.MapGet("/running", handler: () =>
        {
            lock (_taskLock)
            {
                return Results.Json(runningTasks, AppJsonSerializerContext.Default.ListDownloadTask);
            }
        });
        taskStatusApi.MapGet("/finished", handler: () =>
        {
            lock (_taskLock)
            {
                return Results.Json(finishedTasks, AppJsonSerializerContext.Default.ListDownloadTask);
            }
        });
        taskStatusApi.MapGet("/{id}", (string id, CancellationToken token) =>
        {
            DownloadTask? task, rtask;
            lock (_taskLock)
            {
                task = finishedTasks.FirstOrDefault(a => a.Aid == id);
                rtask = runningTasks.FirstOrDefault(a => a.Aid == id);
            }
            if (rtask is not null) task = rtask;
            if (task is null)
            {
                return Results.NotFound();
            }
            return Results.Json(task, AppJsonSerializerContext.Default.DownloadTask);
        });
        app.MapPost("/add-task", (MyOptionBindingResult<ServeRequestOptions> bindingResult, CancellationToken token) =>
        {
            if (!bindingResult.IsValid)
            {
                return Results.BadRequest("输入有误");
            }
            var req = bindingResult.Result!;
            _ = AddDownloadTaskAsync(req, req.CallBackWebHook, token);
            return Results.Ok();
        });
        var finishedRemovalApi = app.MapGroup("remove-finished");
        finishedRemovalApi.MapGet("/", () =>
        {
            lock (_taskLock) { finishedTasks.RemoveAll(t => true); }
            return Results.Ok();
        });
        finishedRemovalApi.MapGet("/failed", () =>
        {
            lock (_taskLock) { finishedTasks.RemoveAll(t => !t.IsSuccessful); }
            return Results.Ok();
        });
        finishedRemovalApi.MapGet("/{id}", (string id) =>
        {
            lock (_taskLock) { finishedTasks.RemoveAll(t => t.Aid == id); }
            return Results.Ok();
        });
    }

    public void Run(string url)
    {
        if (app is null) return;
        bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
            && uriResult.Scheme == Uri.UriSchemeHttp;
        if (!result)
        {
            Logger.LogError($"{url} 不是合法的 http URL，url 示例：http://0.0.0.0:5000");
            Logger.LogWarn("如果您需要 https，请额外配置反向代理");
            Environment.ExitCode = 1;
            return;
        }
        app.Run(url);
    }

    private async Task<DownloadTask> AddDownloadTaskAsync(MyOption option, string? callBackWebHook = null, CancellationToken cancellationToken = default)
    {
        var aid = await BBDownUtil.GetAvIdAsync(option.Url);
        DownloadTask? runningTask;
        lock (_taskLock) { runningTask = runningTasks.FirstOrDefault(t => t.Aid == aid); }
        if (runningTask is not null)
        {
            return runningTask;
        };
        var task = new DownloadTask(aid, option.Url, DateTimeOffset.Now.ToUnixTimeSeconds());
        lock (_taskLock) { runningTasks.Add(task); }
        await _concurrencyLimiter.WaitAsync(cancellationToken);
        try
        {
            var (encodingPriority, dfnPriority, firstEncoding, downloadDanmaku, downloadDanmakuFormats, input, savePathFormat, lang, aidOri, delay) = Program.SetUpWork(option);
            var (fetchedAid, vInfo, apiType) = await Program.GetVideoInfoAsync(option, aidOri, input);
            task.Title = vInfo.Title;
            task.Pic = vInfo.Pic;
            task.VideoPubTime = vInfo.PubTime;
            await Program.DownloadPagesAsync(option, vInfo, encodingPriority, dfnPriority, firstEncoding, downloadDanmaku, downloadDanmakuFormats,
                        input, savePathFormat, lang, fetchedAid, delay, apiType, task, cancellationToken);
            task.IsSuccessful = true;
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or IOException or InvalidOperationException)
        {
            bool debugMode = option.Debug || Config.Current.DebugLog;
            var displayMsg = debugMode ? e.ToString() : e.Message;
            if (debugMode)
            {
                task.ErrorMessage = displayMsg;
            }
            Logger.LogError($"{aid} 下载失败");
            Logger.LogDebug("异常详情: {0}", displayMsg);
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
        task.TaskFinishTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        if (task.IsSuccessful)
        {
            task.Progress = 1f;
            var elapsed = task.TaskFinishTime - task.TaskCreateTime;
            task.DownloadSpeed = elapsed > 0
                ? (double)(task.TotalDownloadedBytes / elapsed)
                : 0;
        }
        lock (_taskLock)
        {
            runningTasks.Remove(task);
            finishedTasks.Add(task);
        }

        // Webhook 回调
        if (!string.IsNullOrEmpty(callBackWebHook))
        {
            string? jsonContent = JsonSerializer.Serialize(task, AppJsonSerializerContext.Default.DownloadTask);
            try
            {
                await HTTPUtil.AppHttpClient.PostAsync(callBackWebHook, new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json"));
            }
            catch (Exception e) when (e is HttpRequestException)
            {
                Logger.LogDebug("回调失败: {0}", e.Message);
            }
        }

        return task;
    }
}

public record DownloadTask(string Aid, string Url, long TaskCreateTime)
{
    [JsonInclude]
    public string? Title = null;
    [JsonInclude]
    public string? Pic = null;
    [JsonInclude]
    public long? VideoPubTime = null;
    [JsonInclude]
    public long? TaskFinishTime = null;
    [JsonInclude]
    public double Progress = 0f;
    [JsonInclude]
    public double DownloadSpeed = 0f;
    [JsonInclude]
    public double TotalDownloadedBytes = 0f;
    [JsonInclude]
    public bool IsSuccessful = false;
    [JsonInclude]
    public string? ErrorMessage = null;

    [JsonInclude]
    public List<string> SavePaths = new();
};
public record DownloadTaskCollection(List<DownloadTask> Running, List<DownloadTask> Finished);

record struct MyOptionBindingResult<T>(T? Result, Exception? Exception)
{
    public bool IsValid => Exception is null;

    public static async ValueTask<MyOptionBindingResult<T>> BindAsync(HttpContext httpContext)
    {
        try
        {
            JsonTypeInfo? jsonTypeInfo = SourceGenerationContext.Default.GetTypeInfo(typeof(T));
            if (jsonTypeInfo is null)
            {
                return new(default, new InvalidOperationException($"Cannot find TypeInfo for type {typeof(T)}"));
            }
            var item = await httpContext.Request.ReadFromJsonAsync(jsonTypeInfo);

            if (item is null) return new(default, new NoNullAllowedException());

            return new((T)item, null);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return new(default, ex);
        }
    }
}

[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(DownloadTask))]
[JsonSerializable(typeof(List<DownloadTask>))]
[JsonSerializable(typeof(DownloadTaskCollection))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{

}

[JsonSerializable(typeof(MyOption))]
[JsonSerializable(typeof(ServeRequestOptions))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
