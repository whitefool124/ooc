import argparse
import hashlib
import json
import re
from pathlib import Path

from PIL import Image, ImageDraw


def normalize(source: Path, destination: Path, width_cells: int, height_cells: int) -> dict:
    image = Image.open(source).convert("RGBA")
    alpha = image.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        raise ValueError(f"source has no visible pixels: {source}")
    cropped = image.crop(bbox)
    target_size = (width_cells * 32, height_cells * 32)
    margin = 2
    scale = max(1, min((target_size[0] - margin * 2) // cropped.width,
                       (target_size[1] - margin * 2) // cropped.height))
    if scale > 1:
        cropped = cropped.resize((cropped.width * scale, cropped.height * scale), Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", target_size, (0, 0, 0, 0))
    offset = ((target_size[0] - cropped.width) // 2, (target_size[1] - cropped.height) // 2)
    canvas.alpha_composite(cropped, offset)
    destination.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(destination, optimize=False)
    colors = len(canvas.getcolors(maxcolors=1 << 24) or [])
    alpha_values = sorted(set(canvas.getchannel("A").tobytes()))
    return {"size": list(target_size), "visible_bbox": list(canvas.getchannel("A").getbbox()),
            "colors_rgba": colors, "alpha_values": alpha_values}


def write_meta(template: str, destination: Path, slug: str) -> None:
    guid = hashlib.md5(("occ.inventory.footprint." + slug).encode("utf-8")).hexdigest()
    content = re.sub(r"guid: [0-9a-f]{32}", "guid: " + guid, template, count=1)
    content = content.replace("aegis_fold_0", slug + "_0")
    destination.write_text(content, encoding="utf-8", newline="\n")


def write_qa_sheet(entries: list[tuple[str, Path]], destination: Path) -> None:
    columns, cell_width, cell_height = 4, 220, 150
    rows = (len(entries) + columns - 1) // columns
    sheet = Image.new("RGBA", (columns * cell_width, rows * cell_height), (20, 24, 28, 255))
    draw = ImageDraw.Draw(sheet)
    for index, (slug, path) in enumerate(entries):
        x = (index % columns) * cell_width
        y = (index // columns) * cell_height
        image = Image.open(path).convert("RGBA")
        for cy in range(y + 24, y + 124, 8):
            for cx in range(x + 8, x + 212, 8):
                shade = 48 if ((cx - x) // 8 + (cy - y) // 8) % 2 == 0 else 62
                draw.rectangle((cx, cy, cx + 7, cy + 7), fill=(shade, shade, shade, 255))
        offset = (x + (cell_width - image.width) // 2, y + 24 + (100 - image.height) // 2)
        sheet.alpha_composite(image, offset)
        draw.text((x + 8, y + 5), f"{slug}  {image.width}x{image.height}", fill=(222, 232, 236, 255))
        draw.rectangle((x + 7, y + 23, x + 212, y + 124), outline=(76, 188, 201, 255))
    destination.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(destination, optimize=False)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", required=True)
    parser.add_argument("--spec", required=True)
    parser.add_argument("--report", required=True)
    parser.add_argument("--qa", required=True)
    args = parser.parse_args()
    root = Path(args.root).resolve()
    spec = json.loads(Path(args.spec).read_text(encoding="utf-8"))
    template_path = root / spec["meta_template"]
    template = template_path.read_text(encoding="utf-8")
    report = {"items": {}, "passed": True}
    qa_entries = []
    for item in spec["items"]:
        source = root / item["source"]
        destination = root / item["output"]
        result = normalize(source, destination, item["width"], item["height"])
        write_meta(template, Path(str(destination) + ".meta"), item["slug"])
        qa_entries.append((item["slug"], destination))
        expected = [item["width"] * 32, item["height"] * 32]
        result["expected"] = expected
        result["passed"] = result["size"] == expected and all(value in (0, 255) for value in result["alpha_values"])
        report["items"][item["id"]] = result
        report["passed"] = report["passed"] and result["passed"]
    report_path = Path(args.report)
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    write_qa_sheet(qa_entries, Path(args.qa))
    if not report["passed"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
