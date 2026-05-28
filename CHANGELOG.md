# 变更日志

本文件遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/) 规范，版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)。

## [未发布]

### 新增

- API 服务器并发数自定义：`BBDown serve --max-concurrent <n>`
- CLI 自定义参数：
  - `--muxer-timeout <分钟>` — 混流超时（默认 30）
  - `--retry-count <n>` — 网络请求重试次数（默认 3）
  - `--retry-delay <毫秒>` — 重试间隔基数（默认 3000）
  - `--thread-segment-size <MB>` — 多线程下载分片大小（默认 20）
- Cookie 过期检测与明确提示（区分"未登录"vs"Cookie 已过期"）
- 下载链路 `CancellationToken` 贯通（CLI Ctrl+C / API 请求取消）
- `.tmp` 文件断点续传支持（完整临时文件自动移动，写入增量校验修复）
- API 服务器文件日志（`bbdown-api.log`）
- `JsonElementExtensions` 安全 JSON 访问器（10 个扩展方法）
- 单元测试骨架：`BBDown.Tests`（`BilibiliBvConverterTests` / `UrlResolverTests` / `FormatHelperTests`）
- 核心方法拆分：`UrlResolver.cs` / `ExternalToolHelper.cs`

### 变更

- 升级依赖：QRCoder 1.6.0 → 1.8.0
- 升级依赖：Google.Protobuf 3.28.3 → 3.34.1
- 升级依赖：Grpc.Tools 2.67.0 → 2.80.0
- 迁移 CLI 框架：System.CommandLine（已归档）→ Spectre.Console.Cli 0.55.0
- `Config` 全局状态重构：`AppSettings` record + 线程安全读写锁
- `HttpClient` 连接池刷新：`SocketsHttpHandler.PooledConnectionLifetime = 5min`
- 规范化 API 文档文件名：`json-api-doc.md` → `API.md`
- 重试策略精细化：指数退避 + 不可重试异常短路（`ArgumentException` / `InvalidOperationException` / `NotSupportedException`）

### 修复

- Windows 下 FFmpeg/MP4Box 混流时弹出命令行窗口（`CreateNoWindow = true`）
- 跨平台目录创建逻辑（`Path.GetDirectoryName` 替代 `Contains('/')`）
- 下载重试时的异常信息丢失问题（增加 `LogDebug`）
- API 服务器 Webhook 回调的未观察异常风险
- `Parser.GetMaxQn` 中 `int.Parse` 未处理非数字输入 → `int.TryParse`
- `BBDownMuxer.EscapeString` 双引号转义逻辑错误
- 多处 `First()` 调用在空序列时抛 `InvalidOperationException`
- `Page.bvid` getter 中 `long.Parse(aid)` 未处理非数字 aid
- `MergeFLV` 空数组保护
- `SpaceVideoFetcher` 中 `GetValidFileName` 与 `BBDownUtil` 的重复实现合并到 `BBDown.Core.Util.PathUtil`
- `Path.GetDirectoryName` 返回 null 时的安全防护
- `AppHelper.DoReqAsync` 参数未校验直接 `Convert.ToInt64`
- 文化敏感字符串操作（`ToLower()` → `ToLowerInvariant()`）防止土耳其 locale bug
- 多处 `JsonDocument` / `HttpResponseMessage` 资源泄漏
- `BBDownDownloadUtil` 进度回调中除零风险防护
- FFmpeg/MP4Box 混流死锁（消费 stdout 防止缓冲区满）
- 并发下载目标文件碰撞（按路径 `SemaphoreSlim` 排他锁）
- API 服务器错误信息泄露（默认隐藏 `ErrorMessage`，仅 debug 模式暴露详情）

## [1.6.3] - 2025-05-06

### 修复

- `DelayPerPage` 选项在 System.CommandLine beta4 下错误地要求必填

## [1.6.2] - 2025-03-16

### 修复

- Dockerfile 构建流程优化
- 多处 `JsonDocument` 未正确释放的问题
- `NormalInfoFetcher` 中 `TryGetProperty` 安全性

## [1.6.1] - 2025-02-08

### 新增

- 支持 ASS 弹幕格式输出
- 合集/系列链接新格式兼容（space.bilibili.com/*/lists/*）

### 修复

- 修正 `GetWebLocationAsync` HEAD 请求兼容性

## [1.6.0] - 2024-12-15

### 新增

- Widevine DRM 原生 C# 解密支持（无需 Python）
- API 服务器模式（`BBDown serve`）
- 配置文件支持（`BBDown.config`）

### 变更

- 重构 gRPC APP 接口请求体
- 增加对多音频轨（背景音频、配音）的支持

---

[未发布]: https://github.com/AliverAnme/BBDown/compare/v1.6.3...HEAD
[1.6.3]: https://github.com/AliverAnme/BBDown/releases/tag/v1.6.3
[1.6.2]: https://github.com/AliverAnme/BBDown/releases/tag/v1.6.2
[1.6.1]: https://github.com/AliverAnme/BBDown/releases/tag/v1.6.1
[1.6.0]: https://github.com/AliverAnme/BBDown/releases/tag/v1.6.0
