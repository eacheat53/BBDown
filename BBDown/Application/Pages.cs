using System;
using System.Collections.Generic;
using System.Linq;
using static BBDown.Core.Entity.Entity;
using static BBDown.BBDownUtil;
using static BBDown.Core.Logger;
using System.Text.RegularExpressions;
using BBDown.Core.Entity;

using BBDown.Core.Util;
using System.Text.Json;
using BBDown.Core;
namespace BBDown;

internal partial class Program
{
    private static List<string>? GetSelectedPages(MyOption myOption, VInfo vInfo, string input)
    {
        List<string>? selectedPages = null;
        List<Page> pagesInfo = vInfo.PagesInfo;
        string selectPage = myOption.SelectPage.ToUpper().Trim().Trim(',');

        if (string.IsNullOrEmpty(selectPage))
        {
            //如果用户没有选择分P, 根据epid或query param来确定某一集
            if (!string.IsNullOrEmpty(vInfo.Index))
            {
                selectedPages = [vInfo.Index];
                Log("程序已自动选择你输入的集数, 如果要下载其他集数请自行指定分P(如可使用-p ALL代表全部)");
            }
            else if (!string.IsNullOrEmpty(GetQueryString("p", input)))
            {
                selectedPages = [GetQueryString("p", input)];
                Log("程序已自动选择你输入的集数, 如果要下载其他集数请自行指定分P(如可使用-p ALL代表全部)");
            }
        }
        else if (selectPage != "ALL")
        {
            selectedPages = new List<string>();

            //选择最新分P
            string lastPage = pagesInfo.Count.ToString();
            foreach (string key in new[] { "LAST", "NEW", "LATEST" })
            {
                selectPage = selectPage.Replace(key, lastPage);
            }

            try
            {
                if (selectPage.Contains('-'))
                {
                    string[] tmp = selectPage.Split('-');
                    int start = int.Parse(tmp[0]);
                    int end = int.Parse(tmp[1]);
                    for (int i = start; i <= end; i++)
                    {
                        selectedPages.Add(i.ToString());
                    }
                }
                else
                {
                    foreach (var s in selectPage.Split(','))
                    {
                        selectedPages.Add(s);
                    }
                }
            }
            catch { LogError("解析分P参数时失败了~"); selectedPages = null; };
        }

        return selectedPages;
    }

    /// <summary>
    /// 处理CDN域名
    /// </summary>
    /// <param name="myOption"></param>
    /// <param name="video"></param>
    /// <param name="audio"></param>
    private static void HandlePcdn(MyOption myOption, Video? selectedVideo, Audio? selectedAudio)
    {
        if (myOption.UposHost == "")
        {
            //处理PCDN
            if (!myOption.AllowPcdn)
            {
                var pcdnReg = PcdnRegex();
                if (selectedVideo != null && pcdnReg.IsMatch(selectedVideo.baseUrl))
                {
                    LogWarn($"检测到视频流为PCDN, 尝试强制替换为{BACKUP_HOST}……");
                    selectedVideo.baseUrl = pcdnReg.Replace(selectedVideo.baseUrl, $"://{BACKUP_HOST}/");
                }
                if (selectedAudio != null && pcdnReg.IsMatch(selectedAudio.baseUrl))
                {
                    LogWarn($"检测到音频流为PCDN, 尝试强制替换为{BACKUP_HOST}……");
                    selectedAudio.baseUrl = pcdnReg.Replace(selectedAudio.baseUrl, $"://{BACKUP_HOST}/");
                }
            }

            var akamReg = AkamRegex();
            if (selectedVideo != null && Config.AREA != "" && selectedVideo.baseUrl.Contains("akamaized.net"))
            {
                LogWarn($"检测到视频流为外国源, 尝试强制替换为{BACKUP_HOST}……");
                selectedVideo.baseUrl = akamReg.Replace(selectedVideo.baseUrl, $"://{BACKUP_HOST}/");
            }
            if (selectedAudio != null && Config.AREA != "" && selectedAudio.baseUrl.Contains("akamaized.net"))
            {
                LogWarn($"检测到音频流为外国源, 尝试强制替换为{BACKUP_HOST}……");
                selectedAudio.baseUrl = akamReg.Replace(selectedAudio.baseUrl, $"://{BACKUP_HOST}/");
            }
        }
        else
        {
            if (selectedVideo != null)
            {
                LogWarn($"尝试将视频流强制替换为{myOption.UposHost}……");
                selectedVideo.baseUrl = UposRegex().Replace(selectedVideo.baseUrl, $"://{myOption.UposHost}/");
            }
            if (selectedAudio != null)
            {
                LogWarn($"尝试将音频流强制替换为{myOption.UposHost}……");
                selectedAudio.baseUrl = UposRegex().Replace(selectedAudio.baseUrl, $"://{myOption.UposHost}/");
            }
        }
    }

    /// <summary>
    /// 打印解析到的各个轨道信息
    /// </summary>
    /// <param name="parsedResult"></param>
    /// <param name="pageDur"></param>
}
