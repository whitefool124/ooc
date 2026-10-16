"""Delivery processing only: preserve independent sources, crop/resize, QA records."""
import hashlib
import json
from pathlib import Path
import shutil
from PIL import Image, ImageOps, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "ArtSource/desk_candidate_coarse_20261010"
GEN = Path("C:/Users/FNHF/.codex/generated_images/01a12387-274a-76a3-9465-c7f69fa32080")
FILES = {"wood": "exec-681c5120-e431-442f-9761-e11c48b792f8.png",
         "plaster": "exec-c1695d99-3f25-4c22-ac9c-a7ea5f30b64a.png",
         "cup": "exec-5f71b2b2-4810-49a8-8bf7-cbb901bc9113.png"}


def rel(p): return p.relative_to(ROOT).as_posix()
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()


def main():
    contract = json.loads((ROOT / "Tools/OCCArt/occ_art_contract_v1.json").read_text(encoding="utf-8-sig"))
    for key, file in FILES.items():
        folder = DEST / key
        for child in ("raw", "delivery", "qa"):
            (folder / child).mkdir(parents=True, exist_ok=True)
        raw = folder / "raw/source.png"
        if not raw.exists(): shutil.copy2(GEN / file, raw)
        im = Image.open(raw).convert("RGBA")
        operations = []
        if key == "cup":
            alpha = im.getchannel("A").point(lambda a: 0 if a < 64 else a)
            im.putalpha(alpha)
            bounds = alpha.getbbox()
            if not bounds: raise RuntimeError("Empty cup alpha")
            im = im.crop(bounds)
            operations += ["remove alpha below 64 (no color/shape repaint)", {"crop": bounds}]
            width = 128
            im = im.resize((width, round(im.height * width / im.width)), Image.Resampling.NEAREST)
            im = ImageOps.expand(im, border=2, fill=(0, 0, 0, 0))
        else:
            im = im.resize((256, 256), Image.Resampling.NEAREST)
            im = ImageOps.expand(im, border=1, fill=(0, 0, 0, 0))
        operations += ["nearest delivery resize under noncombat library exception", "transparent safety border"]
        delivery = folder / ("delivery/" + key + ".png")
        im.save(delivery)
        evidence = {}
        for name, qa in [("one_x", im), ("four_x", im.resize((im.width*4, im.height*4), Image.Resampling.NEAREST)),
                         ("grayscale", ImageOps.grayscale(im).convert("RGBA"))]:
            if name == "grayscale": qa.putalpha(im.getchannel("A"))
            path = folder / ("qa/" + name + ".png"); qa.save(path); evidence[name] = rel(path)
        checker = Image.new("RGBA", im.size)
        draw = ImageDraw.Draw(checker)
        for y in range(0, im.height, 8):
            for x in range(0, im.width, 8):
                draw.rectangle((x,y,x+7,y+7), fill=(192,192,192,255) if (x//8+y//8)%2 else (64,64,64,255))
        checker.alpha_composite(im)
        path = folder / "qa/checker.png"; checker.save(path); evidence["checker"] = rel(path)
        # A current scene capture will supply this after application; never fake it.
        evidence["application_contact"] = None
        manifest = {"schema": "occ-art-manifest-v1", "contract_version": contract["version"],
            "asset_id": DEST.name + "_" + key, "role": "hd2d_library_display", "status": "QA_PENDING",
            "provenance": {"source_channel": "codex_builtin_imagegen", "source_descriptor": "Independent built-in generation; noncombat desk texture/card", "source_path": rel(raw), "source_sha256": sha(raw)},
            "delivery": {"output_path": rel(delivery), "output_sha256": sha(delivery), "native_ppu": None, "logical_cells": None, "palette_max": 1000000, "required_color_families": []},
            "application": {"rendering_mode": "noncombat_hd2d_layered_sprite", "camera_projection": "perspective", "runtime_draw_rect": "real desk " + ("camera-facing cup card foot anchored to desktop" if key == "cup" else "polygon material UV inset excluding safety border")},
            "processing": {"operations": operations, "pixel_repainting": False, "original_size": list(Image.open(raw).size), "delivery_size": list(im.size), "modeling_policy": "2026-10-10 final user correction"},
            "evidence": evidence, "human_review": {"overall": "PENDING", "reviewer": "", "date": "", "notes": "Application and human approval pending; not FORMAL."}}
        (folder / "manifest.occ-art-manifest-v1.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    rejected = DEST / "cup/raw/rejected_halo_r1.png"
    if not rejected.exists(): shutil.copy2(GEN / "exec-98404edc-a05f-4fff-b87d-da9fa5e2ac87.png", rejected)
    (DEST / "cup/raw/rejected_halo_r1.txt").write_text("Rejected: external diffuse halo; regenerated independently. Not imported.\n", encoding="utf-8")
    print("Three processed QA_PENDING deliveries prepared; application evidence still pending.")


if __name__ == "__main__": main()
