#!/bin/bash

# 获取脚本所在目录
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
BINARY="$DIR/BBDown/bin/Release/net10.0/linux-x64/publish/BBDown"
URLS_FILE="$DIR/一剂否定性的投稿视频.txt"

# 默认包含音视频，也可以在运行此脚本时传入参数覆盖默认选项（例如传入 --video-only）
EXTRA_ARGS="$@"

# 注册 Ctrl+C 信号处理，直接退出脚本
trap "echo '批量下载被手动中断，正在退出...'; exit 130" INT

if [ ! -f "$URLS_FILE" ]; then
    echo "未找到链接列表文件: $URLS_FILE"
    exit 1
fi

echo "开始批量下载..."
while IFS= read -r url || [ -n "$url" ]; do
    # 跳过空行和以 # 开头的行
    [[ -z "$url" || "$url" =~ ^# ]] && continue
    echo "=================================================="
    echo "正在下载: $url"
    echo "=================================================="
    "$BINARY" "$url" $EXTRA_ARGS < /dev/null
    
    # 检查命令返回值，如果被 Ctrl+C 中断（通常返回 130 或其它非 0 状态且接收到了信号），直接退出脚本
    exit_code=$?
    if [ $exit_code -eq 130 ]; then
        echo "检测到中断，停止批量任务。"
        exit 130
    fi
done < "$URLS_FILE"

echo "批量下载完成！"
