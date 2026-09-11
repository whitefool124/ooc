
# drive +export-download

认证、身份、scope 或配置问题时读取 [`../lark-shared/SKILL.md`](../../lark-shared/SKILL.md)；常规业务沿用既定身份并显式传 `--as`，不预先重登。高风险确认按完整会话中已有的具体授权处理；真正的权限或审批拒绝不得绕过。

根据导出任务产物的 `file_token` 下载本地文件。通常与 `drive +task_result --scenario export` 配合使用。

## 命令

```bash
# 使用服务端返回的文件名下载到当前目录
lark-cli drive +export-download \
  --file-token "<EXPORTED_FILE_TOKEN>"

# 下载到指定目录
lark-cli drive +export-download \
  --file-token "<EXPORTED_FILE_TOKEN>" \
  --output-dir ./exports

# 指定本地文件名
lark-cli drive +export-download \
  --file-token "<EXPORTED_FILE_TOKEN>" \
  --file-name "weekly-report.pdf" \
  --output-dir ./exports

# 允许覆盖
lark-cli drive +export-download \
  --file-token "<EXPORTED_FILE_TOKEN>" \
  --overwrite
```

## 参数

| 参数 | 必填 | 说明 |
|------|------|------|
| `--file-token` | 是 | 导出完成后的产物 token |
| `--file-name` | 否 | 覆盖默认文件名 |
| `--output-dir` | 否 | 本地输出目录，默认当前目录 |
| `--overwrite` | 否 | 覆盖已存在文件 |

## 使用顺序

1. 用 `drive +export` 发起导出
2. 如果返回 `ticket` / `next_command`，用 `drive +task_result --scenario export --ticket <ticket> --file-token <source_token>` 继续查
3. 查到 `file_token` 后，用 `drive +export-download` 下载

## 参考

- [lark-drive](../SKILL.md) -- 云空间（云盘/云存储）全部命令
- [lark-shared](../../lark-shared/SKILL.md) -- 认证和全局参数
