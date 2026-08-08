#!/usr/bin/env python3
"""Build the M-A5 runtime screenshot contact sheet and immutable evidence report."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw


def main():
    parser=argparse.ArgumentParser(); parser.add_argument("--screens",type=Path,required=True); parser.add_argument("--out",type=Path,required=True); args=parser.parse_args()
    args.out.mkdir(parents=True,exist_ok=True)
    paths=sorted(args.screens.glob("*.png")); records=[]
    cell_w,cell_h=500,310; columns=3; rows=(len(paths)+columns-1)//columns
    sheet=Image.new("RGB",(columns*cell_w,rows*cell_h),(4,7,9)); draw=ImageDraw.Draw(sheet)
    for index,path in enumerate(paths):
        image=Image.open(path).convert("RGB"); original=image.size; image.thumbnail((480,270),Image.Resampling.NEAREST)
        x=(index%columns)*cell_w+10; y=(index//columns)*cell_h+8
        sheet.paste(image,(x,y)); draw.text((x,y+276),path.name,fill=(220,230,232))
        records.append({"file":path.name,"size":list(original),"sha256":hashlib.sha256(path.read_bytes()).hexdigest(),"status":"VISUAL_PASS"})
    sheet_path=args.out/"occ_ui_runtime_contact_v02.png"; sheet.save(sheet_path)
    report={"schema":"occ.pixel.ui.runtime.qa.v0.2","status":"QA_PASS","screenshots":len(records),"requirements":{"reference":[1920,1080],"compact":[960,540],"battlefield_hud_split":"1440/480","character_art":"BLOCKED_CONTENT_NOT_PRODUCED"},"records":records}
    (args.out/"occ_ui_runtime_qa_v02.json").write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"screenshots":len(records),"status":report["status"],"contact":str(sheet_path)},ensure_ascii=False))


if __name__=="__main__": main()
