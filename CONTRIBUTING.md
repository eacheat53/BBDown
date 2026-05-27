# 贡献指南

感谢你对 BBDown 项目的兴趣！在提交贡献之前，请阅读本指南。

## 开发环境

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- 支持的操作系统：Windows / Linux / macOS

## 构建

```bash
# 还原依赖
dotnet restore

# 编译（Debug）
dotnet build

# 编译（Release）
dotnet build -c Release

# 发布单文件（示例：win-x64）
dotnet publish BBDown -r win-x64 -c Release
```

## 代码风格

- 缩进：4 空格
- 编码：UTF-8
- 换行符：跨平台，由 Git 自动处理
- 遵循 `.editorconfig` 中的配置

## 提交 Issue

- 提交前请先搜索，避免重复
- Bug 报告请使用 Bug Report 模板，并提供 `--debug` 日志
- 功能请求请使用 Feature Request 模板

## 提交 Pull Request

1. Fork 本仓库并创建你的分支：`git checkout -b feature/fooBar`
2. 修改代码，确保 `dotnet build` 通过
3. 如有必要，更新相关文档（README、CHANGELOG 等）
4. 提交并推送到你的 Fork：`git push origin feature/fooBar`
5. 在 GitHub 提交 Pull Request，并填写 PR 模板

## PR 审查原则

- 原子性：一个 PR 只做一件事
- 向后兼容：不破坏现有 CLI 参数和配置文件格式
- 文档同步：代码变更需同步更新文档
