using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static BBDown.Core.Entity.Entity;
using static BBDown.BBDownUtil;
using static BBDown.Core.Logger;
using System.Linq;
using System.Text.RegularExpressions;
using BBDown.Core;
using BBDown.Core.Entity;
using static BBDown.BBDownDownloadUtil;

using BBDown.Core.Util;
using System.Text.Json;
namespace BBDown;

internal partial class Program
{
    private static void HandleDeprecatedOptions(MyOption myOption)
    {
        if (myOption.AddDfnSuffix)
        {
            LogWarn("--add-dfn-subfix 已被弃用, 建议使用 --file-pattern/-F 或 --multi-file-pattern/-M 来自定义输出文件名格式");
            if (string.IsNullOrEmpty(myOption.FilePattern) && string.IsNullOrEmpty(myOption.MultiFilePattern))
            {
                SinglePageDefaultSavePath += "[<dfn>]";
                MultiPageDefaultSavePath += "[<dfn>]";
                LogWarn($"已切换至 -F \"{SinglePageDefaultSavePath}\" -M \"{MultiPageDefaultSavePath}\"");
            }
        }
        if (myOption.Aria2cProxy != "")
        {
            LogWarn("--aria2c-proxy 已被弃用, 请使用 --aria2c-args 来设置aria2c代理, 本次执行已添加该代理");
            myOption.Aria2cArgs += $" --all-proxy=\"{myOption.Aria2cProxy}\"";
        }
        if (myOption.OnlyHevc)
        {
            LogWarn("--only-hevc/-hevc 已被弃用, 请使用 --encoding-priority 来设置编码优先级, 本次执行已将hevc设置为最高优先级");
            myOption.EncodingPriority = "hevc";
        }
        if (myOption.OnlyAvc)
        {
            LogWarn("--only-avc/-avc 已被弃用, 请使用 --encoding-priority 来设置编码优先级, 本次执行已将avc设置为最高优先级");
            myOption.EncodingPriority = "avc";
        }
        if (myOption.OnlyAv1)
        {
            LogWarn("--only-av1/-av1 已被弃用, 请使用 --encoding-priority 来设置编码优先级, 本次执行已将av1设置为最高优先级");
            myOption.EncodingPriority = "av1";
        }
        if (myOption.NoPaddingPageNum)
        {
            LogWarn("--no-padding-page-num 已被弃用, 建议使用 --file-pattern/-F 或 --multi-file-pattern/-M 来自定义输出文件名格式");
            if (string.IsNullOrEmpty(myOption.FilePattern) && string.IsNullOrEmpty(myOption.MultiFilePattern))
            {
                MultiPageDefaultSavePath = MultiPageDefaultSavePath.Replace("<pageNumberWithZero>", "<pageNumber>");
                LogWarn($"已切换至 -M \"{MultiPageDefaultSavePath}\"");
            }
        }
        if (myOption.BandwidthAscending)
        {
            LogWarn("--bandwith-ascending 已被弃用, 建议使用 --video-ascending 与 --audio-ascending 来指定视频或音频是否升序, 本次执行已将视频与音频均设为升序");
            myOption.VideoAscending = true;
            myOption.AudioAscending = true;
        }
    }

    /// <summary>
    /// 解析用户指定的编码优先级
    /// </summary>
    /// <param name="myOption"></param>
    /// <returns></returns>
    private static Dictionary<string, byte> ParseEncodingPriority(MyOption myOption, out string firstEncoding)
    {
        var encodingPriority = new Dictionary<string, byte>();
        firstEncoding = "";
        if (myOption.EncodingPriority != null)
        {
            var encodingPriorityTemp = myOption.EncodingPriority
                .ToUpper()
                .Replace('，', ',')
                .Replace("-", string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => !string.IsNullOrEmpty(s)).ToList();
            byte index = 0;
            firstEncoding = encodingPriorityTemp.First();
            foreach (string encoding in encodingPriorityTemp)
            {
                if (encodingPriority.ContainsKey(encoding))
                    continue;
                encodingPriority[encoding] = index;
                index++;
            }
        }
        return encodingPriority;
    }

    private static BBDownDanmakuFormat[] ParseDownloadDanmakuFormats(MyOption myOption)
    {
        if (string.IsNullOrEmpty(myOption.DownloadDanmakuFormats)) return BBDownDanmakuFormatInfo.DefaultFormats;

        var formats = myOption.DownloadDanmakuFormats.Replace("，", ",").ToLower().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (formats.Any(format => !BBDownDanmakuFormatInfo.AllFormatNames.Contains(format)))
        {
            LogError($"包含不支持的下载弹幕格式：{myOption.DownloadDanmakuFormats}");
            return BBDownDanmakuFormatInfo.DefaultFormats;
        }
        
        return formats.Select(BBDownDanmakuFormatInfo.FromFormatName).ToArray();
    }

    /// <summary>
    /// 解析用户输入的清晰度规格优先级
    /// </summary>
    /// <param name="myOption"></param>
    /// <returns></returns>
    private static Dictionary<string, int> ParseDfnPriority(MyOption myOption)
    {
        var dfnPriority = new Dictionary<string, int>();
        if (myOption.DfnPriority != null)
        {
            var dfnPriorityTemp = myOption.DfnPriority.Replace("，", ",").Split(',').Select(s => s.ToUpper().Trim()).Where(s => !string.IsNullOrEmpty(s));
            int index = 0;
            foreach (string dfn in dfnPriorityTemp)
            {
                if (dfnPriority.ContainsKey(dfn)) { continue; }
                dfnPriority[dfn] = index;
                index++;
            }
        }
        return dfnPriority;
    }

    /// <summary>
    /// 寻找并设置所需的二进制文件
    /// </summary>
    /// <param name="myOption"></param>
    /// <exception cref="Exception"></exception>
    private static void FindBinaries(MyOption myOption)
    {
        if (!string.IsNullOrEmpty(myOption.FFmpegPath) && File.Exists(myOption.FFmpegPath))
        {
            BBDownMuxer.FFMPEG = myOption.FFmpegPath;
        }

        if (!string.IsNullOrEmpty(myOption.Mp4boxPath) && File.Exists(myOption.Mp4boxPath))
        {
            BBDownMuxer.MP4BOX = myOption.Mp4boxPath;
        }

        if (!string.IsNullOrEmpty(myOption.Aria2cPath) && File.Exists(myOption.Aria2cPath))
        {
            BBDownAria2c.ARIA2C = myOption.Aria2cPath;
        }
        //寻找ffmpeg或mp4box
        if (!myOption.SkipMux)
        {
            if (myOption.UseMP4box)
            {
                if (string.IsNullOrEmpty(BBDownMuxer.MP4BOX) || !File.Exists(BBDownMuxer.MP4BOX))
                {
                    var binPath = FindExecutable("mp4box") ?? FindExecutable("MP4box");
                    if (string.IsNullOrEmpty(binPath))
                        throw new Exception("找不到可执行的mp4box文件");
                    BBDownMuxer.MP4BOX = binPath;
                }
            }
            else if (string.IsNullOrEmpty(BBDownMuxer.FFMPEG) || !File.Exists(BBDownMuxer.FFMPEG))
            {
                var binPath = FindExecutable("ffmpeg");
                if (string.IsNullOrEmpty(binPath))
                    throw new Exception("找不到可执行的ffmpeg文件");
                BBDownMuxer.FFMPEG = binPath;
            }
        }

        //寻找aria2c
        if (myOption.UseAria2c)
        {
            if (string.IsNullOrEmpty(BBDownAria2c.ARIA2C) || !File.Exists(BBDownAria2c.ARIA2C))
            {
                var binPath = FindExecutable("aria2c");
                if (string.IsNullOrEmpty(binPath))
                    throw new Exception("找不到可执行的aria2c文件");
                BBDownAria2c.ARIA2C = binPath;
            }

        }
    }

    /// <summary>
    /// 处理有冲突的选项
    /// </summary>
    /// <param name="myOption"></param>
    private static void HandleConflictingOptions(MyOption myOption)
    {
        //手动选择时不能隐藏流
        if (myOption.Interactive)
        {
            myOption.HideStreams = false;
        }
        //audioOnly和videoOnly同时开启则全部忽视
        if (myOption.AudioOnly && myOption.VideoOnly)
        {
            myOption.AudioOnly = false;
            myOption.VideoOnly = false;
        }
        if (myOption.SkipSubtitle)
        {
            myOption.SubOnly = false;
        }
    }

    /// <summary>
    /// 设置用户输入的自定义工作目录
    /// </summary>
    /// <param name="myOption"></param>
    private static void ChangeWorkingDir(MyOption myOption)
    {
        if (!string.IsNullOrEmpty(myOption.WorkDir))
        {
            //解释环境变量
            myOption.WorkDir = Environment.ExpandEnvironmentVariables(myOption.WorkDir);
            var dir = Path.GetFullPath(myOption.WorkDir);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            //设置工作目录
            Environment.CurrentDirectory = dir;
            LogDebug("切换工作目录至：{0}", dir);
        }
    }

    /// <summary>
    /// 加载用户的认证信息（cookie或token）
    /// </summary>
    /// <param name="myOption"></param>
    private static void LoadCredentials(MyOption myOption)
    {
        if (string.IsNullOrEmpty(Config.COOKIE) && File.Exists(Path.Combine(APP_DIR, "BBDown.data")))
        {
            Log("加载本地cookie...");
            LogDebug("文件路径：{0}", Path.Combine(APP_DIR, "BBDown.data"));
            Config.COOKIE = File.ReadAllText(Path.Combine(APP_DIR, "BBDown.data"));
        }
        if (string.IsNullOrEmpty(Config.TOKEN) && File.Exists(Path.Combine(APP_DIR, "BBDownTV.data")) && myOption.UseTvApi)
        {
            Log("加载本地token...");
            LogDebug("文件路径：{0}", Path.Combine(APP_DIR, "BBDownTV.data"));
            Config.TOKEN = File.ReadAllText(Path.Combine(APP_DIR, "BBDownTV.data"));
            Config.TOKEN = Config.TOKEN.Replace("access_token=", "");
        }
        if (string.IsNullOrEmpty(Config.TOKEN) && File.Exists(Path.Combine(APP_DIR, "BBDownApp.data")) && myOption.UseAppApi)
        {
            Log("加载本地token...");
            LogDebug("文件路径：{0}", Path.Combine(APP_DIR, "BBDownApp.data"));
            Config.TOKEN = File.ReadAllText(Path.Combine(APP_DIR, "BBDownApp.data"));
            Config.TOKEN = Config.TOKEN.Replace("access_token=", "");
        }
    }

}
