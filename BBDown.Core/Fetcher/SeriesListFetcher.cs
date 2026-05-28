using BBDown.Core.Entity;
using BBDown.Core.Util;
using System.Text.Json;
using static BBDown.Core.Entity.Entity;

namespace BBDown.Core.Fetcher;

/// <summary>
/// 列表解析
/// https://space.bilibili.com/23630128/channel/seriesdetail?sid=340933
/// </summary>
public class SeriesListFetcher : IFetcher
{
    public async Task<VInfo> FetchAsync(string id)
    {
        //套用BBDownMediaListFetcher.cs的代码
        //只修改id = id.Substring(12);以及api地址的type=5
        id = id[12..];
        var api = $"https://api.bilibili.com/x/v1/medialist/info?type=5&biz_id={id}&tid=0";
        var json = await HTTPUtil.GetWebSourceAsync(api);
        using var infoJson = JsonDocument.Parse(json);
        var data = infoJson.RootElement.GetPropertySafe("data");
        var listTitle = data.GetStringSafe("title")!;
        var intro = data.GetStringSafe("intro")!;
        long pubTime = data.GetInt64Safe("ctime");

        List<Page> pagesInfo = new();
        bool hasMore = true;
        var oid = "";
        int index = 1;
        while (hasMore)
        {
            var listApi = $"https://api.bilibili.com/x/v2/medialist/resource/list?type=5&oid={oid}&otype=2&biz_id={id}&bvid=&with_current=true&mobi_app=web&ps=20&direction=false&sort_field=1&tid=0&desc=true";
            json = await HTTPUtil.GetWebSourceAsync(listApi);
            using var listJson = JsonDocument.Parse(json);
            data = listJson.RootElement.GetPropertySafe("data");
            hasMore = data.GetBooleanSafe("has_more");
            foreach (var m in data.EnumerateArraySafe("media_list"))
            {
                // 只处理未失效的视频条目（与收藏夹解析逻辑保持一致）
                if (m.TryGetProperty("attr", out var attrElem) && attrElem.GetInt32() != 0)
                    continue;

                var pageCount = m.GetInt32Safe("page");
                var desc = m.GetStringSafe("intro")!;
                var ownerName = m.GetPropertySafe("upper").GetValueAsStringSafe("name");
                var ownerMid = m.GetPropertySafe("upper").GetValueAsStringSafe("mid");
                foreach (var page in m.EnumerateArraySafe("pages"))
                {
                    Page p = new(index++,
                        m.GetValueAsStringSafe("id"),
                        page.GetValueAsStringSafe("id"),
                        "", //epid
                        pageCount == 1 ? m.GetValueAsStringSafe("title") : $"{m.GetValueAsStringSafe("title")}_P{page.GetValueAsStringSafe("page")}_{page.GetValueAsStringSafe("title")}", //单P使用外层标题 多P则拼接内层子标题
                        page.TryGetProperty("duration", out var dur) ? dur.GetInt32() : 0,
                        page.TryGetProperty("dimension", out var dim) && dim.TryGetProperty("width", out var w) && dim.TryGetProperty("height", out var h) ? $"{w}x{h}" : "",
                        m.GetInt64Safe("pubtime"),
                        m.GetValueAsStringSafe("cover"),
                        desc,
                        ownerName,
                        ownerMid);
                    if (!pagesInfo.Contains(p)) pagesInfo.Add(p);
                    else index--;
                }
                oid = m.GetValueAsStringSafe("id");
            }
        }

        var info = new VInfo
        {
            Title = listTitle.Trim(),
            Desc = intro.Trim(),
            Pic = "",
            PubTime = pubTime,
            PagesInfo = pagesInfo,
            IsBangumi = false
        };

        return info;
    }
}