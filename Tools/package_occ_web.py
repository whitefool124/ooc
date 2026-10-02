"""Package a successful Unity Web build without editing Unity serialized assets."""
import argparse
import hashlib
import json
import shutil
import zipfile
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("source", type=Path)
parser.add_argument("destination", type=Path)
args = parser.parse_args()
source, destination = args.source.resolve(), args.destination.resolve()
if not (source / "index.html").is_file():
    raise SystemExit("Missing successful Unity Web build")
if destination.exists():
    raise SystemExit("Destination already exists; preserve prior verified deliveries")
shutil.copytree(source, destination, ignore=shutil.ignore_patterns("*DoNotShip*"))
build = destination / "Build"
def one(pattern):
    files = list(build.glob(pattern))
    if len(files) != 1:
        raise SystemExit(f"Expected one {pattern}, got {files}")
    return "Build/" + files[0].name
loader = one("*.loader.js")
config = {
    "dataUrl": one("*.data.unityweb"),
    "frameworkUrl": one("*.framework.js.unityweb"),
    "codeUrl": one("*.wasm.unityweb"),
    "streamingAssetsUrl": "StreamingAssets",
    "companyName": "OCC", "productName": "OCC 学院战棋", "productVersion": "2026-10-02",
    "matchWebGLToCanvasSize": False, "devicePixelRatio": 1, "autoSyncPersistentDataPath": True,
}
html = '''<!doctype html>
<html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>OCC 学院战棋</title><link rel="icon" href="TemplateData/favicon.ico"><style>
*{box-sizing:border-box}html,body{margin:0;width:100%;height:100%;background:#171a1a;color:#e7dfc9;font-family:system-ui,sans-serif}
body{display:grid;place-items:center;overflow:hidden}#game{width:min(100vw,177.7778vh);height:min(100vh,56.25vw);position:relative}
canvas{display:block;width:100%;height:100%;background:#171a1a;image-rendering:pixelated;outline:none}
#welcome{position:absolute;inset:0;display:grid;place-items:center;background:#171a1a}article{max-width:600px;padding:32px;text-align:center}
h1{font-size:36px;letter-spacing:4px}p{line-height:1.8;color:#c4bfae}button{font:inherit;background:#e7dfc9;color:#252b2b;border:2px solid #948268;padding:14px 38px;cursor:pointer}
button:focus-visible{outline:3px solid #7bd5d3}button:disabled{cursor:wait;opacity:.7}progress{width:100%;margin-top:24px;accent-color:#7bd5d3}#status{white-space:pre-wrap;font-size:14px}
</style></head><body><main id="game"><canvas id="unity-canvas" width="1920" height="1080" tabindex="0" aria-label="OCC 游戏画面"></canvas>
<section id="welcome"><article><h1>OCC · 学院战棋</h1><p>观察敌人意图，利用术式与地形，在学院试炼中建立自己的构筑。</p>
<button id="start">开始游戏</button><progress id="progress" max="1" value="0" hidden></progress><p id="status">建议使用电脑浏览器，鼠标操作。存档保存在当前浏览器中。</p></article></section></main>
<script src="__LOADER__"></script><script>
const start=document.getElementById('start'),status=document.getElementById('status'),bar=document.getElementById('progress'),canvas=document.getElementById('unity-canvas');
const config=__CONFIG__;
config.showBanner=(message,type)=>{console[type==='error'?'error':'warn'](message);if(type==='error'){status.textContent=message;document.getElementById('welcome').style.display='grid';}};
start.addEventListener('click',async()=>{start.disabled=true;bar.hidden=false;status.textContent='正在加载游戏…';
try{window.occGame=await createUnityInstance(canvas,config,p=>{bar.value=p;status.textContent='正在加载游戏… '+Math.round(p*100)+'%';});
document.getElementById('welcome').style.display='none';canvas.focus();document.documentElement.dataset.gameReady='true';}
catch(error){status.textContent='加载失败：'+error.message+'。请通过 HTTP/HTTPS 打开此页面并重试。';start.disabled=false;console.error(error);}});
</script></body></html>'''
(destination / "index.html").write_text(html.replace("__LOADER__", loader).replace("__CONFIG__", json.dumps(config, ensure_ascii=False)), encoding="utf-8")
(destination / "serve.py").write_text('''from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
import os
os.chdir(Path(__file__).resolve().parent)
print("OCC preview: http://127.0.0.1:8088")
ThreadingHTTPServer(("127.0.0.1",8088),SimpleHTTPRequestHandler).serve_forever()
''', encoding="utf-8")
(destination / "使用说明.txt").write_text("OCC H5 候选试玩版\n部署整个目录到 HTTP/HTTPS 静态站点，入口为 index.html；不能用 file:// 双击运行。\n本地可用 Python 运行 serve.py 后访问 http://127.0.0.1:8088。\n点击开始游戏后允许声音播放；新建存档、选择维克多、学院地图、首战出发。\n鼠标选择格子与术式，滚轮调节战场视野；暂离保存，原地址再次打开可继续。\n浏览器存档按站点保存；清除站点数据、换浏览器或更换站点不会共享原档。\n本包不代表第一阶段已完成完整验收，也尚未上传比赛。\n", encoding="utf-8")
credits = Path(__file__).resolve().parent.parent / "ArtSource/Audio/AUDIO_CREDITS.md"
if credits.is_file():
    shutil.copy2(credits, destination / "AUDIO_CREDITS.md")
repo = Path(__file__).resolve().parent.parent
for src, name in [
    (repo / "ArtSource/unit_refresh_20261002/integration.json", "ART_PROVENANCE.json"),
    (repo / "Worldbuilding/归档/2026-10-02_新单位美术实装与AIGCC准备/参赛材料草稿.md", "参赛材料草稿.md"),
]:
    if src.is_file():
        shutil.copy2(src, destination / name)
files = sorted(p for p in destination.rglob("*") if p.is_file())
(destination / "SHA256SUMS.txt").write_text("\n".join(hashlib.sha256(p.read_bytes()).hexdigest()+"  "+p.relative_to(destination).as_posix() for p in files)+"\n", encoding="utf-8")
archive = destination.with_suffix(".zip")
with zipfile.ZipFile(archive, "x", zipfile.ZIP_DEFLATED) as zipped:
    for path in sorted(destination.rglob("*")):
        if path.is_file():
            zipped.write(path, path.relative_to(destination).as_posix())
total = sum(p.stat().st_size for p in destination.rglob("*") if p.is_file())
summary={"directory":str(destination),"zip":str(archive),"uncompressed_bytes":total,"zip_bytes":archive.stat().st_size,"under_300mb":total<=300000000 and archive.stat().st_size<=300000000}
destination.with_suffix(".package.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(summary, ensure_ascii=False))
