using System;
using BBDown.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using static BBDown.Core.Entity.Entity;
using static BBDown.Core.Util.HTTPUtil;
using System.Collections.Concurrent;

namespace BBDown;

internal static class BBDownDownloadUtil
{
    public class DownloadConfig
    {
        public bool UseAria2c { get; set; } = false;
        public string Aria2cArgs { get; set; } = string.Empty;
        public bool ForceHttp { get; set; } = false;
        public bool MultiThread { get; set; } = false;
        public DownloadTask? RelatedTask { get; set; } = null;
    }

    private static async Task RangeDownloadToTmpAsync(int id, string url, string tmpName, long fromPosition, long? toPosition, Action<int, long, long> onProgress, bool failOnRangeNotSupported = false, CancellationToken token = default)
    {
        DateTimeOffset? lastTime = File.Exists(tmpName) ? new FileInfo(tmpName).LastWriteTimeUtc : null;
        using var fileStream = new FileStream(tmpName, FileMode.OpenOrCreate);
        fileStream.Seek(0, SeekOrigin.End);
        if (toPosition > 0 && fileStream.Position == toPosition - fromPosition + 1)
        {
            // 已下载完成 直接汇报进度并跳过下载
            onProgress(id, fileStream.Position, fileStream.Position);
            return;
        }
        var downloadedBytes = fromPosition + fileStream.Position;

        using var httpRequestMessage = new HttpRequestMessage();
        if (!url.Contains("platform=android_tv_yst") && !url.Contains("platform=android"))
            httpRequestMessage.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com");
        httpRequestMessage.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
        httpRequestMessage.Headers.TryAddWithoutValidation("Cookie", Core.Config.Current.Cookie);
        httpRequestMessage.Headers.Range = new(downloadedBytes, toPosition);
        httpRequestMessage.Headers.IfRange = lastTime != null ? new(lastTime.Value) : null;
        httpRequestMessage.RequestUri = new(url);

        using var response = (await AppHttpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, token)).EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.OK) // server doesn't response a partial content
        {
            if (failOnRangeNotSupported && (downloadedBytes > 0 || toPosition != null)) throw new NotSupportedException("Range request is not supported.");
            downloadedBytes = 0;
            fileStream.Seek(0, SeekOrigin.Begin);
        }

        using var stream = await response.Content.ReadAsStreamAsync(token);
        var totalBytes = downloadedBytes + (response.Content.Headers.ContentLength ?? long.MaxValue - downloadedBytes);

        const int blockSize = 1048576 / 4;
        var buffer = new byte[blockSize];

        while (downloadedBytes < totalBytes)
        {
            var recevied = await stream.ReadAsync(buffer, token);
            if (recevied == 0) break;
            await fileStream.WriteAsync(buffer.AsMemory(0, recevied), token);
            await fileStream.FlushAsync(token);
            downloadedBytes += recevied;
            onProgress(id, downloadedBytes - fromPosition, totalBytes);
        }

        if (response.Content.Headers.ContentLength != null && (response.Content.Headers.ContentLength != new FileInfo(tmpName).Length))
            throw new InvalidOperationException("Retry...");
    }

    private static readonly Dictionary<string, SemaphoreSlim> _downloadLocks = new();
    private static readonly object _lockFactory = new();

    private static SemaphoreSlim GetDownloadLock(string path)
    {
        lock (_lockFactory)
        {
            if (!_downloadLocks.TryGetValue(path, out var semaphore))
            {
                semaphore = new SemaphoreSlim(1, 1);
                _downloadLocks[path] = semaphore;
            }
            return semaphore;
        }
    }

    public static async Task DownloadFileAsync(string url, string path, DownloadConfig config, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(url)) return;
        var downloadLock = GetDownloadLock(path);
        await downloadLock.WaitAsync(token);
        try
        {
        if (config.ForceHttp) url = ReplaceUrl(url);
        Logger.LogDebug("Start downloading: {0}", url);
        string desDir = Path.GetDirectoryName(path)!;
        if (!string.IsNullOrEmpty(desDir) && !Directory.Exists(desDir)) Directory.CreateDirectory(desDir);
        if (config.UseAria2c)
        {
            await BBDownAria2c.DownloadFileByAria2cAsync(url, path, config.Aria2cArgs);
            if (File.Exists(path + ".aria2") || !File.Exists(path))
                throw new InvalidOperationException("aria2下载可能存在错误");
            Console.WriteLine();
            return;
        }
        int retry = 0;
        string tmpName = Path.Combine(desDir, Path.GetFileNameWithoutExtension(path) + ".tmp");
        while (retry < 3)
        {
        try
        {
            using var progress = new ProgressBar(config.RelatedTask);
            await RangeDownloadToTmpAsync(0, url, tmpName, 0, null, (_, downloaded, total) => progress.Report((double)downloaded / total, downloaded), token: token);
            File.Move(tmpName, path, true);
            break;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or InvalidOperationException)
        {
            throw; // non-retryable: bad input, unsupported feature, logic error
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException)
        {
            int backoffMs = retry * 3000;
            Logger.LogDebug("下载失败(第{0}次重试, {1}ms后): {2}", retry + 1, backoffMs, ex.Message);
            await Task.Delay(backoffMs, token);
            if (++retry == 3) throw;
        }
        }
        }
        finally
        {
            downloadLock.Release();
        }
    }

    public static async Task MultiThreadDownloadFileAsync(string url, string path, DownloadConfig config, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(url)) return;
        var downloadLock = GetDownloadLock(path);
        await downloadLock.WaitAsync(token);
        try
        {
        if (config.ForceHttp) url = ReplaceUrl(url);
        Logger.LogDebug("Start downloading: {0}", url);
        if (config.UseAria2c)
        {
            await BBDownAria2c.DownloadFileByAria2cAsync(url, path, config.Aria2cArgs);
            if (File.Exists(path + ".aria2") || !File.Exists(path))
                throw new InvalidOperationException("aria2下载可能存在错误");
            Console.WriteLine();
            return;
        }
        long fileSize = await GetFileSizeAsync(url, token);
        Logger.LogDebug("文件大小：{0} bytes", fileSize);
        //已下载过, 跳过下载
        if (File.Exists(path) && new FileInfo(path).Length == fileSize)
        {
            Logger.LogDebug("文件已下载过, 跳过下载");
            return;
        }
        List<Clip> allClips = GetAllClips(url, fileSize);
        int total = allClips.Count;
        Logger.LogDebug("分段数量：{0}", total);
        ConcurrentDictionary<int, long> clipProgress = new();
        foreach (var i in allClips) clipProgress[i.index] = 0;

        using var progress = new ProgressBar(config.RelatedTask);
        progress.Report(0);
        await Parallel.ForEachAsync(allClips, token, async (clip, _) =>
        {
            int retry = 0;
            string tmp = Path.Combine(Path.GetDirectoryName(path)!, clip.index.ToString("00000") + "_" + Path.GetFileNameWithoutExtension(path) + (Path.GetExtension(path).EndsWith(".mp4") ? ".vclip" : ".aclip"));
            while (retry < 3)
            {
            try
            {
                await RangeDownloadToTmpAsync(clip.index, url, tmp, clip.from, clip.to == -1 ? null : clip.to, (index, downloaded, _) =>
                {
                    clipProgress[index] = downloaded;
                    progress.Report(fileSize > 0 ? (double)clipProgress.Values.Sum() / fileSize : 0, clipProgress.Values.Sum());
                }, true, _);
                break;
            }
            catch (NotSupportedException)
            {
                if (++retry == 3) throw new NotSupportedException("服务器可能并不支持多线程下载, 请使用 --multi-thread false 关闭多线程");
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                throw; // non-retryable
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException)
            {
                int backoffMs = retry * 3000;
                Logger.LogDebug("分段下载失败(第{0}次重试, {1}ms后): {2}", retry + 1, backoffMs, ex.Message);
                await Task.Delay(backoffMs, _);
                if (++retry == 3) throw new IOException($"分段 {clip.index} 下载失败，请检查网络或关闭多线程重试", ex);
            }
            }
        });
        }
        finally
        {
            downloadLock.Release();
        }
    }

    //此函数主要是切片下载逻辑
    private static List<Clip> GetAllClips(string url, long fileSize)
    {
        List<Clip> clips = [];
        int index = 0;
        long counter = 0;
        long perSize = 20L * 1024 * 1024;
        while (fileSize > 0)
        {
            long segmentSize = Math.Min(perSize, fileSize);
            Clip c = new()
            {
                index = index,
                from = counter,
                to = fileSize > perSize ? counter + segmentSize - 1 : -1
            };
            clips.Add(c);
            fileSize -= segmentSize;
            counter += segmentSize;
            index++;
        }
        return clips;
    }

    private static async Task<long> GetFileSizeAsync(string url, CancellationToken token = default)
    {
        using var httpRequestMessage = new HttpRequestMessage();
        if (!url.Contains("platform=android_tv_yst") && !url.Contains("platform=android"))
            httpRequestMessage.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com");
        httpRequestMessage.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
        httpRequestMessage.Headers.TryAddWithoutValidation("Cookie", Core.Config.Current.Cookie);
        httpRequestMessage.RequestUri = new(url);
        using var response = (await AppHttpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, token))
            .EnsureSuccessStatusCode();
        long totalSizeBytes = response.Content.Headers.ContentLength ?? 0;

        return totalSizeBytes;
    }

    /// <summary>
    /// 将下载地址强制转换为HTTP
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static string ReplaceUrl(string url)
    {
        if (url.Contains(".mcdn.bilivideo.cn:"))
        {
            Logger.LogDebug("对[*.mcdn.bilivideo.cn:xxx]域名不做处理");
            return url;
        }

        Logger.LogDebug("将https更改为http");
        return url.Replace("https:", "http:");
    }
}