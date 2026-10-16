import html
import re
from pathlib import Path
from apply_doc_updates import ARCHIVE, LOCAL, DOC, ROOT, call, fetch

original = (ARCHIVE / "feishu_before_full.md").read_text(encoding="utf-8")
paragraph = next(p for p in original.split("\n\n") if p.startswith("1920×1080参考画布，上部地图使用全宽"))
r = fetch("emphasis_before", "本段整帧320×180")
block = next(e for e in r if e.tag == "p")
prefix = "本段整帧320×180、六倍整数放大及二维绘制规则仅适用于未迁移场景；已迁移HD-2D使用A.2.4高清输出及品质门槛。源资产原生尺寸、32 PPU与逻辑占格分别遵循当前资产分类。"
parts = re.split(r"(\*\*.*?\*\*)", paragraph)
content = "<p>" + html.escape(prefix) + "".join("<b>" + html.escape(p[2:-2]) + "</b>" if p.startswith("**") else html.escape(p) for p in parts) + "</p>"
patch = ARCHIVE / "restore_emphasis.xml"
patch.write_text(content, encoding="utf-8")
call("restore_emphasis", "+update", "--doc", DOC, "--command", "block_replace", "--block-id", block.get("id"), "--content", "@./" + patch.relative_to(ROOT).as_posix())
fetch("emphasis_readback", "本段整帧320×180")
final = call("feishu_final_full", "+fetch", "--doc", DOC, "--doc-format", "markdown")
cloud = final["content"]
local = LOCAL.read_text(encoding="utf-8")
start, end = "## A.2 像素与布局", "## A.3"
LOCAL.write_text(local[:local.index(start)] + cloud[cloud.index(start):cloud.index(end, cloud.index(start))] + local[local.index(end, local.index(start)):], encoding="utf-8")
print("Preserved original bold emphasis and synchronized affected art section; revision", final["revision_id"])
