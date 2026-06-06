[![img](https://img.shields.io/github/stars/AliverAnme/BBDown?label=%E7%82%B9%E8%B5%9E)](https://github.com/AliverAnme/BBDown)  [![img](https://img.shields.io/github/last-commit/AliverAnme/BBDown?label=%E6%9C%80%E8%BF%91%E6%8F%90%E4%BA%A4)](https://github.com/AliverAnme/BBDown)  [![img](https://img.shields.io/github/release/AliverAnme/BBDown?label=%E6%9C%80%E6%96%B0%E7%89%88%E6%9C%AC)](https://github.com/AliverAnme/BBDown/releases)  [![img](https://img.shields.io/github/license/AliverAnme/BBDown?label=%E8%AE%B8%E5%8F%AF%E8%AF%81)](https://github.com/AliverAnme/BBDown)  [![Build Latest](https://github.com/AliverAnme/BBDown/actions/workflows/build_latest.yml/badge.svg)](https://github.com/AliverAnme/BBDown/actions/workflows/build_latest.yml)

> 本项目仅供个人学习、研究和非商业性用途。用户在使用本工具时，需自行确保遵守相关法律法规，特别是与版权相关的法律条款。开发者不对因使用本工具而产生的任何版权纠纷或法律责任承担责任。请用户在使用时谨慎，确保其行为合法合规，并仅在有合法授权的情况下使用相关内容。

# BBDown
一个命令行式哔哩哔哩下载器. Bilibili Downloader.

# 注意
本软件混流时需要外部程序：

* 普通视频：[ffmpeg](https://www.gyan.dev/ffmpeg/builds/) ，或 [mp4box](https://gpac.wp.imt.fr/downloads/)
* 杜比视界：ffmpeg5.0以上或新版mp4box.

# 下载
Release版本：https://github.com/AliverAnme/BBDown/releases

自动构建的测试版本：https://github.com/AliverAnme/BBDown/actions

# 开始使用
运行 `BBDown --help` 查看完整的可用参数与命令列表：

```bash
BBDown --help
```

部分核心参数速查：

| 短选项 | 长选项 | 说明 |
|--------|--------|------|
| `-t` | `--use-tv-api` | 使用 TV 端解析模式 |
| `-a` | `--use-app-api` | 使用 APP 端解析模式 |
| `-I` | `--only-show-info` | 仅解析而不下载 |
| `-i` | `--interactive` | 交互式选择清晰度 |
| `-d` | `--download-danmaku` | 下载弹幕 |
| `-e` | `--encoding-priority` | 编码优先级（如 `hevc,av1,avc`） |
| `-q` | `--dfn-priority` | 画质优先级 |
| `-p` | `--select-page` | 选择分 P（如 `-p 1,3,5-10`） |
| `-F` | `--file-pattern` | 自定义单 P 文件名格式 |
| `-M` | `--multi-file-pattern` | 自定义多 P 文件名格式 |
| `-c` | `--cookie` | 设置 Cookie |
| | `--muxer-timeout` | 混流超时时长（分钟，默认 30） |
| | `--retry-count` | 网络请求失败重试次数（默认 3） |
| | `--retry-delay` | 重试间隔基础毫秒数（默认 3000） |
| | `--thread-segment-size` | 多线程下载分片大小（MB，默认 20） |
| | `--config-file` | 指定配置文件路径 |

Commands:
- `login` — APP 扫码登录 WEB 账号
- `logintv` — APP 扫码登录 TV 账号
- `serve` — 以 API 服务器模式运行
  - `-l, --listen` — 监听地址（默认 `http://0.0.0.0:23333`）
  - `--max-concurrent` — 最大并发下载数（默认 3）

# 功能
- [x] 番剧下载(Web|TV|App)
- [x] 课程下载(Web)
- [x] 普通内容下载(Web|TV|App)
- [x] 合集/列表/收藏夹/个人空间解析
- [x] 多分P自动下载
- [x] 选择指定分P进行下载
- [x] 选择指定清晰度进行下载
- [x] 下载外挂字幕并转换为srt/ass格式
- [x] 自动合并音频+视频流+字幕流+**章节信息**`(使用ffmpeg或mp4box)`
- [x] 单独下载视频/音频/字幕
- [x] 二维码登录账号
- [x] 多线程下载（支持自定义分片大小）
- [x] 支持调用aria2c下载
- [x] 支持AVC/HEVC/AV1编码
- [x] **支持8K/HDR/杜比视界/杜比全景声下载**
- [x] **Widevine DRM 解密 (原生C#实现, 无需 Python)**
- [x] 自定义存储文件名
- [x] **API 服务器模式**（`BBDown serve`，支持并发限制与文件日志）
- [x] **配置文件支持**（`BBDown.config`）
- [x] **Ctrl+C 下载中断**（CancellationToken 全链路贯通）
- [x] **.tmp 断点续传**（崩溃后自动识别已下载的临时文件）

# TODO

## 已完成 ✅

- [x] API 服务器下载任务队列限制（`SemaphoreSlim(3)` 并发控制）
- [x] `HttpClient` DNS 刷新配置（`SocketsHttpHandler.PooledConnectionLifetime = 5min`）
- [x] `BBDownMuxer.RunExe` 超时机制（30 分钟上限 + 强制终止）
- [x] 下载目标文件并发排他锁（按路径 `SemaphoreSlim`）
- [x] 异常粒度精细化（28 处 `Exception` → 语义化类型）
- [x] 重试策略精细化（指数退避 + 不可重试异常短路）
- [x] 下载链路 `CancellationToken` 贯通（CLI Ctrl+C / API 请求取消）
- [x] `Config` 全局静态可变状态重构（`AppSettings` record + 线程安全锁）
- [x] `.tmp` 文件断点续传（完整临时文件自动移动，写入校验修复）
- [x] 日志系统适配 API 服务器（`Logger.LogFilePath` → `bbdown-api.log`）
- [x] 支持更多自定义选项（`--muxer-timeout` / `--retry-count` / `--retry-delay` / `--thread-segment-size`）
- [x] 零测试覆盖 → 单元测试骨架（`BBDown.Tests` + `BilibiliBvConverterTests` / `UrlResolverTests` / `FormatHelperTests`）
- [x] 拆分下载/解析核心方法（`UrlResolver.cs` / `ExternalToolHelper.cs`）
- [x] API 并发数自定义（`--max-concurrent` 选项暴露 API 服务器并发限制）
- [x] Cookie 过期检测与明确提示（区分 "未登录" vs "Cookie 已过期"）
- [x] JSON 解析统一错误包装（`JsonElementExtensions` + 全项目 200+ 处调用迁移至 safe accessor）

## 待完成

。。。

# 使用教程

<details>
<summary>配置文件 (NEW)</summary> 

---

在`1.4.9`或更高版本中，BBDown支持读取本地配置文件以简化命令行的手动输入。

如果用户没有指定`--config-file`，则默认读取程序同目录下的`BBDown.config`文件；若用户指定，则读取特定文件。

一个典型的配置文件:
```config
#本文件是BBDown程序的配置文件
#以#开头的都会被程序忽略
#然后剩余非空白内容程序逐行读取，对于一个选项，其参数应当在下一行出现

#例如下面将设置输出文件名格式
--file-pattern
<videoTitle>[<dfn>]

--multi-file-pattern
<videoTitle>/[P<pageNumberWithZero>]<pageTitle>[<dfn>]

#下面设置下载多个分P时，每个分P的下载间隔为2秒
--delay-per-page
2

#开启弹幕下载功能
--download-danmaku
```

</details>

<details>
<summary>自定义输出文件名格式 (NEW)</summary> 

---

在`1.4.9`或更高版本中，BBDown支持用户自定义合并时的文件名组成。
|  代码   | 含义  |
|  ----  | ----  |
`<videoTitle>`|视频主标题
`<pageNumber>`|视频分P序号
`<pageNumberWithZero>`|视频分P序号(前缀补零)
`<pageTitle>`|视频分P标题
`<bvid>`|视频BV号
`<aid>`|视频aid
`<cid>`|视频cid
`<dfn>`|视频清晰度
`<res>`|视频分辨率
`<fps>`|视频帧率
`<videoCodecs>`|视频编码
`<videoBandwidth>`|视频码率
`<audioCodecs>`|音频编码
`<audioBandwidth>`|音频码率
`<ownerName>`|上传者名称(下载番剧时，该值为"")
`<ownerMid>`|上传者mid(下载番剧时，该值为"")
`<publishDate>`|发布时间(yyyy-MM-dd_HH-mm-ss)
`<apiType>`|API类型（TV/APP/INTL/WEB）

</details>

<details>
<summary>WEB/TV鉴权</summary>  

---
  
扫码登录网页账号：
```
BBDown login
```
然后按照提示操作

扫码登录云视听小电视账号：
```
BBDown logintv
```
然后按照提示操作
 
*PS: 如果登录报错`The type initializer for 'Gdip' threw an exception`，请参考 [#37](https://github.com/AliverAnme/BBDown/issues/37) 解决*

手动加载网页cookie：
```
BBDown -c "SESSDATA=******" "https://www.bilibili.com/video/BV1qt4y1X7TW"
```
手动加载云视听小电视token：
```
BBDown -tv -token "******" "https://www.bilibili.com/video/BV1qt4y1X7TW"
```

</details>

<details>
<summary>APP鉴权</summary>  

---

> 根据 [#123](https://github.com/AliverAnme/BBDown/issues/123#issuecomment-877583825) ，可以填写TV登录产生的`access_token`来给APP接口使用。可复制`BBDownTV.data`到`BBDownApp.data`使程序自动读取.

目前程序无法自动获取鉴权信息，推荐通过**抓包**来获取.

在请求Header中寻找键为`authorization`的项，其值形为`identify_v1 5227************1`，其中的`5227************1`就是token(access_key)

获取后手动通过`-token`命令加载, 或写入`BBDownApp.data`使程序自动读取.
  
```
BBDown -app -token "******" "https://www.bilibili.com/video/BV1qt4y1X7TW"
```

</details>

<details>
<summary>Widevine DRM 解密</summary>

---

BBDown 目前以**原生 C#** 实现了 Widevine CDM，可自动获取解密密钥并解密 B站 DRM 保护内容，**无需 Python/pywidevine**。

**准备**
1. 获取一个 `device.wvd` 文件（Widevine 设备文件，需自行提取或从可信来源获取）
2. 将 `device.wvd` 放在以下任一位置：
   - 程序所在目录
   - 环境变量 `PATH` 中的目录
   - macOS: `/opt/homebrew/bin` / Linux: `/usr/local/bin` / Windows: 程序目录

**使用**
```shell
# 下载 DRM 保护的视频（自动解密）
BBDown --decrypt-drm "https://www.bilibili.com/cheese/play/ep1243104"
```

如需手动指定 `device.wvd` 路径，可将其放置在工作目录或 PATH 环境变量中的目录里。

**原理说明**
- 使用 `drm_tech_type=2` 请求标准 Widevine 流
- 从 B站许可证服务器获取密钥（兼容性取决于 device.wvd 的 `security_level`）
- 解密后与普通视频一样进行混流输出

</details>

<details>
<summary>常用命令</summary>  

---

下载普通视频：
```
BBDown "https://www.bilibili.com/video/BV1qt4y1X7TW"
```
使用TV接口下载(粉丝量大的UP主基本上是无水印片源)：
```
BBDown -tv "https://www.bilibili.com/video/BV1qt4y1X7TW"
```
当分P过多时，默认会隐藏展示全部的分P信息，你可以使用如下命令来显示所有每一个分P。
```
BBDown --show-all "https://www.bilibili.com/video/BV1At41167aj"
```
选择下载某些分P的三种情况：
* 单个分P：10
```
BBDown "https://www.bilibili.com/video/BV1At41167aj?p=10"
BBDown -p 10 "https://www.bilibili.com/video/BV1At41167aj"
```
* 多个分P：1,2,10
```
BBDown -p 1,2,10 "https://www.bilibili.com/video/BV1At41167aj"
```
* 范围分P：1-10
```
BBDown -p 1-10 "https://www.bilibili.com/video/BV1At41167aj"
```
下载番剧全集：
```
BBDown -p ALL "https://www.bilibili.com/bangumi/play/ss33073"
```

</details>

<details>
<summary>API服务器</summary>

启动服务器（自定义监听地址和端口）：

```shell
BBDown serve -l http://0.0.0.0:12450
```

API服务器不支持HTTPS配置，如果有需要请自行使用nginx等反向代理进行配置

API详细请参考 [API.md](./API.md)
</details>

# 演示
![1](https://user-images.githubusercontent.com/20772925/88686407-a2001480-d129-11ea-8aac-97a0c71af115.gif)

下载完毕后在当前目录查看MP4文件：

![2](https://user-images.githubusercontent.com/20772925/88478901-5e1cdc00-cf7e-11ea-97c1-154b9226564e.png)

## 开发构建

```bash
# 克隆仓库
git clone https://github.com/AliverAnme/BBDown.git
cd BBDown

# 还原依赖并编译
dotnet restore
dotnet build

# 运行
BBDown/bin/Debug/net9.0/BBDown --help
```

详细贡献指南请参考 [CONTRIBUTING.md](./CONTRIBUTING.md)。

## 更新日志

查看 [CHANGELOG.md](./CHANGELOG.md) 了解版本历史。

## 许可证

本项目基于 [MIT 许可证](./LICENSE) 开源。

## 安全

安全漏洞报告请参考 [SECURITY.md](./SECURITY.md)。请勿通过公开 Issue 报告安全问题。

## 社区

- [贡献指南](./CONTRIBUTING.md)
- [行为准则](./CODE_OF_CONDUCT.md)
- [Discussions](https://github.com/AliverAnme/BBDown/discussions)

# 致谢

本项目继承自 [nilaoda/BBDown](https://github.com/nilaoda/BBDown)，在此感谢原作者的开创性工作。

### 本分支额外致谢
* https://github.com/spectreconsole/spectre.console

### 原作者致谢
* https://github.com/codebude/QRCoder
* https://github.com/icsharpcode/SharpZipLib
* https://github.com/protocolbuffers/protobuf
* https://github.com/grpc/grpc
* https://github.com/SocialSisterYi/bilibili-API-collect
* https://github.com/SeeFlowerX/bilibili-grpc-api
* https://github.com/FFmpeg/FFmpeg
* https://github.com/gpac/gpac
* https://github.com/aria2/aria2
