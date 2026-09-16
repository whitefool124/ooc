#!/usr/bin/env python3
"""Normalize the Sephiria-direction debug sample to OCC battlefield tiers."""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Worldbuilding/归档/2026-09-14_赛菲莉娅主参考确认/source"
DEST = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaSephiriaDebug"
EVIDENCE = ROOT / "UnityProject/Reports/CombatTestArena/sephiria_debug_evidence"

def quantize(im: Image.Image, colors: int) -> Image.Image:
    alpha = im.getchannel("A").point(lambda v: 255 if v >= 128 else 0)
    rgb = im.convert("RGB").quantize(colors=colors, method=Image.Quantize.FASTOCTREE).convert("RGB")
    out = rgb.convert("RGBA"); out.putalpha(alpha); return out

def save_pair(im: Image.Image, stem: str, colors: int, companion_colors: int) -> None:
    master = quantize(im.resize((64, 64), Image.Resampling.NEAREST), colors)
    low = quantize(im.resize((32, 32), Image.Resampling.NEAREST), companion_colors)
    master.save(DEST / f"{stem}_64.png"); low.save(DEST / f"{stem}_32.png")
    master.resize((256, 256), Image.Resampling.NEAREST).save(EVIDENCE / f"{stem}_1x.png")
    low.resize((128, 128), Image.Resampling.NEAREST).save(EVIDENCE / f"{stem}_32_4x.png")
    gray = master.convert("LA").convert("RGBA"); gray.putalpha(master.getchannel("A")); gray.resize((256,256), Image.Resampling.NEAREST).save(EVIDENCE / f"{stem}_grayscale.png")

def fit_prop(source: Image.Image, canvas_size=(64, 64), target=(48, 42), contact_y=58, colors=14) -> Image.Image:
    alpha = source.getchannel("A").point(lambda v: 255 if v >= 128 else 0)
    bbox = alpha.getbbox()
    if bbox is None: raise ValueError("prop has no visible subject")
    crop = source.crop(bbox); rgb = crop.convert("RGB").quantize(colors=colors, method=Image.Quantize.FASTOCTREE).convert("RGB")
    scale = min(target[0] / crop.width, target[1] / crop.height)
    size = (max(1, round(crop.width*scale)), max(1, round(crop.height*scale)))
    fitted = rgb.resize(size, Image.Resampling.NEAREST).convert("RGBA"); fitted.putalpha(alpha.crop(bbox).resize(size, Image.Resampling.NEAREST))
    canvas = Image.new("RGBA", canvas_size, (0,0,0,0)); x=(canvas_size[0]-size[0])//2; y=contact_y-size[1]; canvas.alpha_composite(fitted,(x,y)); return quantize(canvas,colors)

def fit_prop_to_coarse_grid(
    source: Image.Image,
    canvas_size=(64, 64),
    target=(56, 42),
    contact_y=59,
    colors=11,
) -> Image.Image:
    """Reconstruct a pseudo-pixel source once on a coarse native grid.

    RGB is area-sampled in premultiplied form so removed chroma pixels cannot
    bleed green into the contour. The result is hard-alpha and palette-limited;
    larger deliveries may then use exact integer scaling from this grid.
    """
    source = source.convert("RGBA")
    alpha = source.getchannel("A").point(lambda v: 255 if v >= 128 else 0)
    bbox = alpha.getbbox()
    if bbox is None:
        raise ValueError("prop has no visible subject")
    crop = source.crop(bbox)
    crop_alpha = alpha.crop(bbox)
    scale = min(target[0] / crop.width, target[1] / crop.height)
    size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))

    crop_rgb = crop.convert("RGB")
    premultiplied = Image.new("RGB", crop.size, (0, 0, 0))
    src_rgb = crop_rgb.load(); src_alpha = crop_alpha.load(); pre = premultiplied.load()
    for y in range(crop.height):
        for x in range(crop.width):
            if src_alpha[x, y]:
                pre[x, y] = src_rgb[x, y]
    small_pre = premultiplied.resize(size, Image.Resampling.BOX)
    small_alpha = crop_alpha.resize(size, Image.Resampling.BOX)
    fitted = Image.new("RGBA", size, (0, 0, 0, 0))
    pre_px = small_pre.load(); alpha_px = small_alpha.load(); fitted_px = fitted.load()
    for y in range(size[1]):
        for x in range(size[0]):
            a = alpha_px[x, y]
            if a < 128:
                continue
            r, g, b = pre_px[x, y]
            fitted_px[x, y] = (
                min(255, round(r * 255 / a)),
                min(255, round(g * 255 / a)),
                min(255, round(b * 255 / a)),
                255,
            )
    canvas = Image.new("RGBA", canvas_size, (0, 0, 0, 0))
    x = (canvas_size[0] - size[0]) // 2
    y = contact_y - size[1]
    canvas.alpha_composite(fitted, (x, y))
    return quantize(canvas, colors)

def replace_outermost_pixel_with_black(image: Image.Image) -> Image.Image:
    """Recolor the existing outermost opaque texels; never expand the silhouette."""
    image = image.convert("RGBA")
    alpha = image.getchannel("A").point(lambda v: 255 if v >= 128 else 0)
    alpha_pixels = alpha.load(); result = image.copy(); pixels = result.load()
    for y in range(image.height):
        for x in range(image.width):
            if not alpha_pixels[x, y]: continue
            if any(nx < 0 or ny < 0 or nx >= image.width or ny >= image.height or not alpha_pixels[nx, ny]
                   for nx, ny in ((x-1,y),(x+1,y),(x,y-1),(x,y+1))):
                pixels[x,y]=(0,0,0,255)
    result.putalpha(alpha)
    return result

def reserve_pure_black_for_outer_outline(
    image: Image.Image, darkest_material=(24, 26, 32)
) -> Image.Image:
    """Keep pure black exclusive to the final silhouette outline."""
    image = image.convert("RGBA")
    result = image.copy(); pixels = result.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = pixels[x, y]
            if a and (r, g, b) == (0, 0, 0):
                pixels[x, y] = (*darkest_material, a)
    return result

def remove_baked_checker_background(image: Image.Image) -> Image.Image:
    """Recover the largest opaque subject when a generator baked a light checker into RGB."""
    image = image.convert("RGBA"); rgb = image.load(); w,h=image.size
    candidate = bytearray(w*h)
    for y in range(h):
        for x in range(w):
            r,g,b,_=rgb[x,y]
            # Generated checker cells are light, nearly neutral gray/white.
            if not (min(r,g,b) >= 190 and max(r,g,b)-min(r,g,b) <= 10): candidate[y*w+x]=1
    seen=bytearray(w*h); largest=[]
    for start,v in enumerate(candidate):
        if not v or seen[start]: continue
        seen[start]=1; stack=[start]; component=[]
        while stack:
            i=stack.pop(); component.append(i); x=i%w; y=i//w
            for nx,ny in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                if 0<=nx<w and 0<=ny<h:
                    ni=ny*w+nx
                    if candidate[ni] and not seen[ni]: seen[ni]=1; stack.append(ni)
        if len(component)>len(largest): largest=component
    alpha=Image.new("L",(w,h),0); ap=alpha.load()
    for i in largest: ap[i%w,i//w]=255
    image.putalpha(alpha); return image

def remove_chroma_green_background(image: Image.Image) -> Image.Image:
    """Remove a flat green generation matte without altering the subject geometry."""
    image = image.convert("RGBA")
    alpha = Image.new("L", image.size, 255)
    source = image.load(); mask = alpha.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, _ = source[x, y]
            if g >= 100 and g - r >= 40 and g - b >= 40:
                mask[x, y] = 0
    image.putalpha(alpha)
    return image

def main() -> None:
    DEST.mkdir(parents=True, exist_ok=True); EVIDENCE.mkdir(parents=True, exist_ok=True)
    ground = Image.open(SOURCE / "ground_sephiria_v1.png").convert("RGBA").resize((16,16), Image.Resampling.NEAREST)
    save_pair(ground.resize((64,64), Image.Resampling.NEAREST), "sephiria_ground", 8, 6)
    unit = Image.open(SOURCE / "unit_side_v1.png").convert("RGBA")
    # Registered to the OCC unit baseline; retain side-view silhouette and hard alpha.
    alpha = unit.getchannel("A").point(lambda v: 255 if v >= 128 else 0); bbox = alpha.getbbox()
    crop = unit.crop(bbox); rgb = crop.convert("RGB").quantize(colors=24, method=Image.Quantize.FASTOCTREE).convert("RGB")
    scale=min(44/crop.width,48/crop.height); size=(round(crop.width*scale),round(crop.height*scale)); fitted=rgb.resize(size,Image.Resampling.NEAREST).convert("RGBA"); fitted.putalpha(alpha.crop(bbox).resize(size,Image.Resampling.NEAREST)); canvas=Image.new("RGBA",(64,64),(0,0,0,0)); canvas.alpha_composite(fitted,((64-size[0])//2,58-size[1])); canvas.save(DEST/"sephiria_unit_side_64.png"); canvas.resize((32,32),Image.Resampling.NEAREST).save(DEST/"sephiria_unit_side_32.png")
    cover = fit_prop(Image.open(SOURCE / "heavy_cover_grid_aligned_v2.png").convert("RGBA"), target=(46,40), contact_y=54)
    cover.save(DEST/"sephiria_heavy_cover_64.png")
    quantize(cover.resize((32,32),Image.Resampling.NEAREST), 10).save(DEST/"sephiria_heavy_cover_32.png")
    cover2_source = remove_baked_checker_background(Image.open(SOURCE / "heavy_cover_2x2_v1.png"))
    cover2 = fit_prop(cover2_source, canvas_size=(128,128), target=(112,84), contact_y=119, colors=16)
    cover2.save(DEST/"sephiria_heavy_cover_2x2_128.png")
    cover2.resize((64,64),Image.Resampling.NEAREST).save(DEST/"sephiria_heavy_cover_2x2_64.png")
    # Exact target-grid outline family: apply after each master/companion reaches
    # its final dimensions so the ring is one pixel at both resolution tiers.
    for source_name, output_name in [
        ("sephiria_unit_side_64.png", "sephiria_unit_side_post_outline_64.png"),
        ("sephiria_unit_side_32.png", "sephiria_unit_side_post_outline_32.png"),
        ("sephiria_heavy_cover_64.png", "sephiria_heavy_cover_post_outline_64.png"),
        ("sephiria_heavy_cover_32.png", "sephiria_heavy_cover_post_outline_32.png"),
        ("sephiria_heavy_cover_2x2_128.png", "sephiria_heavy_cover_2x2_post_outline_128.png"),
        ("sephiria_heavy_cover_2x2_64.png", "sephiria_heavy_cover_2x2_post_outline_64.png")]:
        replace_outermost_pixel_with_black(Image.open(DEST/source_name)).save(DEST/output_name)
    masters = [DEST/"sephiria_ground_64.png", DEST/"sephiria_unit_side_64.png", DEST/"sephiria_heavy_cover_64.png", DEST/"sephiria_heavy_cover_2x2_128.png"]
    for path in masters:
        im = Image.open(path).convert("RGBA")
        zoom = im.resize((im.width*4, im.height*4), Image.Resampling.NEAREST)
        board = Image.new("RGBA", zoom.size, (180,180,180,255)); px=board.load()
        for y in range(board.height):
            for x in range(board.width):
                if ((x//16)+(y//16))%2: px[x,y]=(220,220,220,255)
        board.alpha_composite(zoom)
        board.save(EVIDENCE/(path.stem+"_contact.png"))

    # Independently regenerated black-outline test family. Normalization only
    # registers and quantizes the model-authored outline; it does not draw one.
    outlined_unit = Image.open(SOURCE / "outlined_unit_side_v2_source.png").convert("RGBA")
    alpha = outlined_unit.getchannel("A").point(lambda v: 255 if v >= 128 else 0); bbox = alpha.getbbox(); crop = outlined_unit.crop(bbox)
    rgb = crop.convert("RGB").quantize(colors=24, method=Image.Quantize.FASTOCTREE).convert("RGB"); scale=min(44/crop.width,48/crop.height); size=(round(crop.width*scale),round(crop.height*scale)); fitted=rgb.resize(size,Image.Resampling.NEAREST).convert("RGBA"); fitted.putalpha(alpha.crop(bbox).resize(size,Image.Resampling.NEAREST)); canvas=Image.new("RGBA",(64,64),(0,0,0,0)); canvas.alpha_composite(fitted,((64-size[0])//2,58-size[1])); canvas.save(DEST/"sephiria_unit_side_outline_64.png")
    outlined_cover = fit_prop(Image.open(SOURCE / "outlined_heavy_cover_v3_source.png").convert("RGBA"), target=(48,42), contact_y=55)
    outlined_cover.save(DEST/"sephiria_heavy_cover_outline_64.png")
    outlined_cover2 = fit_prop(Image.open(SOURCE / "outlined_heavy_cover_2x2_v2_source.png").convert("RGBA"), canvas_size=(128,128), target=(112,84), contact_y=119, colors=16)
    outlined_cover2.save(DEST/"sephiria_heavy_cover_2x2_outline_128.png")
    for path in [DEST/"sephiria_unit_side_outline_64.png", DEST/"sephiria_heavy_cover_outline_64.png", DEST/"sephiria_heavy_cover_2x2_outline_128.png"]:
        im=Image.open(path).convert("RGBA"); zoom=im.resize((im.width*4,im.height*4),Image.Resampling.NEAREST); board=Image.new("RGBA",zoom.size,(180,180,180,255)); px=board.load()
        for y in range(board.height):
            for x in range(board.width):
                if ((x//16)+(y//16))%2: px[x,y]=(220,220,220,255)
        board.alpha_composite(zoom); board.save(EVIDENCE/(path.stem+"_contact.png"))
    for path in [DEST/"sephiria_unit_side_post_outline_64.png", DEST/"sephiria_heavy_cover_post_outline_64.png", DEST/"sephiria_heavy_cover_2x2_post_outline_128.png"]:
        im=Image.open(path).convert("RGBA"); zoom=im.resize((im.width*4,im.height*4),Image.Resampling.NEAREST); board=Image.new("RGBA",zoom.size,(180,180,180,255)); px=board.load()
        for y in range(board.height):
            for x in range(board.width):
                if ((x//16)+(y//16))%2: px[x,y]=(220,220,220,255)
        board.alpha_composite(zoom); board.save(EVIDENCE/(path.stem+"_contact.png"))
    # Native-grid-first experiment: these sources deliberately contain only
    # details intended to survive at 64px. Register once, then replace the
    # existing outermost contour without expanding it.
    native_unit=Image.open(SOURCE/"native_grid_unit_v1_source.png").convert("RGBA"); alpha=native_unit.getchannel("A").point(lambda v:255 if v>=128 else 0); bbox=alpha.getbbox(); crop=native_unit.crop(bbox); rgb=crop.convert("RGB").quantize(colors=16,method=Image.Quantize.FASTOCTREE).convert("RGB"); scale=min(44/crop.width,48/crop.height); size=(round(crop.width*scale),round(crop.height*scale)); fitted=rgb.resize(size,Image.Resampling.NEAREST).convert("RGBA"); fitted.putalpha(alpha.crop(bbox).resize(size,Image.Resampling.NEAREST)); canvas=Image.new("RGBA",(64,64),(0,0,0,0)); canvas.alpha_composite(fitted,((64-size[0])//2,58-size[1])); native_unit_final=replace_outermost_pixel_with_black(canvas); native_unit_final.save(DEST/"sephiria_unit_native_grid_64.png")
    native_cover_source=Image.open(SOURCE/"native_grid_heavy_cover_v5_source.png").convert("RGBA")
    native_cover=fit_prop(native_cover_source,target=(46,40),contact_y=54,colors=14); native_cover=replace_outermost_pixel_with_black(native_cover); native_cover.save(DEST/"sephiria_heavy_cover_native_grid_v5_64.png")
    for path in [DEST/"sephiria_unit_native_grid_64.png",DEST/"sephiria_heavy_cover_native_grid_v5_64.png"]:
        im=Image.open(path).convert("RGBA"); zoom=im.resize((256,256),Image.Resampling.NEAREST); board=Image.new("RGBA",zoom.size,(180,180,180,255)); bp=board.load()
        for y in range(256):
            for x in range(256):
                if ((x//16)+(y//16))%2: bp[x,y]=(220,220,220,255)
        board.alpha_composite(zoom); board.save(EVIDENCE/(path.stem+"_contact.png"))

    # Simplified-prompt experiment. Content generation only fixes the prop's
    # role and map view; target-grid sizing, palette, alpha and outline remain
    # deterministic post-processing responsibilities.
    simple_cover_source = remove_chroma_green_background(
        Image.open(SOURCE / "simple_prompt_heavy_cover_v2_source.png"))
    simple_cover_128 = fit_prop(
        simple_cover_source, canvas_size=(128, 128), target=(112, 84),
        contact_y=119, colors=15)
    simple_cover_128 = replace_outermost_pixel_with_black(simple_cover_128)
    simple_cover_128.save(DEST / "sephiria_heavy_cover_simple_prompt_2x2_128.png")
    simple_cover_64 = quantize(
        simple_cover_128.resize((64, 64), Image.Resampling.NEAREST), 11)
    simple_cover_64 = replace_outermost_pixel_with_black(simple_cover_64)
    simple_cover_64.save(DEST / "sephiria_heavy_cover_simple_prompt_2x2_64.png")
    for path in [
        DEST / "sephiria_heavy_cover_simple_prompt_2x2_128.png",
        DEST / "sephiria_heavy_cover_simple_prompt_2x2_64.png",
    ]:
        im = Image.open(path).convert("RGBA")
        zoom = im.resize((im.width * 4, im.height * 4), Image.Resampling.NEAREST)
        board = Image.new("RGBA", zoom.size, (180, 180, 180, 255)); bp = board.load()
        for y in range(board.height):
            for x in range(board.width):
                if ((x // 16) + (y // 16)) % 2:
                    bp[x, y] = (220, 220, 220, 255)
        board.alpha_composite(zoom)
        board.save(EVIDENCE / (path.stem + "_contact.png"))

    # Same simplified content brief with the screen-axis camera constraint
    # restated explicitly after the preceding generation drifted to isometric.
    axis_cover_source = remove_chroma_green_background(
        Image.open(SOURCE / "simple_prompt_heavy_cover_v4_source.png"))
    axis_cover_128 = fit_prop(
        axis_cover_source, canvas_size=(128, 128), target=(112, 84),
        contact_y=119, colors=15)
    axis_cover_128 = replace_outermost_pixel_with_black(axis_cover_128)
    axis_cover_128.save(DEST / "sephiria_heavy_cover_simple_axis_2x2_128.png")
    axis_cover_64 = quantize(
        axis_cover_128.resize((64, 64), Image.Resampling.NEAREST), 11)
    axis_cover_64 = replace_outermost_pixel_with_black(axis_cover_64)
    axis_cover_64.save(DEST / "sephiria_heavy_cover_simple_axis_2x2_64.png")
    for path in [
        DEST / "sephiria_heavy_cover_simple_axis_2x2_128.png",
        DEST / "sephiria_heavy_cover_simple_axis_2x2_64.png",
    ]:
        im = Image.open(path).convert("RGBA")
        zoom = im.resize((im.width * 4, im.height * 4), Image.Resampling.NEAREST)
        board = Image.new("RGBA", zoom.size, (180, 180, 180, 255)); bp = board.load()
        for y in range(board.height):
            for x in range(board.width):
                if ((x // 16) + (y // 16)) % 2:
                    bp[x, y] = (220, 220, 220, 255)
        board.alpha_composite(zoom)
        board.save(EVIDENCE / (path.stem + "_contact.png"))

    # Candidate regenerated after locking the Sephiria-derived map language:
    # broad readable top plane, screen-down facade, restrained decoration and
    # one dominant silhouette. Pixel dimensions remain a pipeline concern.
    learned_cover_source = remove_chroma_green_background(
        Image.open(SOURCE / "sephiria_learned_heavy_cover_v1_source.png"))
    learned_cover_128 = fit_prop(
        learned_cover_source, canvas_size=(128, 128), target=(112, 84),
        contact_y=119, colors=15)
    learned_cover_128 = replace_outermost_pixel_with_black(learned_cover_128)
    learned_cover_128.save(DEST / "sephiria_heavy_cover_learned_2x2_128.png")
    learned_cover_64 = quantize(
        learned_cover_128.resize((64, 64), Image.Resampling.NEAREST), 11)
    learned_cover_64 = replace_outermost_pixel_with_black(learned_cover_64)
    learned_cover_64.save(DEST / "sephiria_heavy_cover_learned_2x2_64.png")
    for path in [
        DEST / "sephiria_heavy_cover_learned_2x2_128.png",
        DEST / "sephiria_heavy_cover_learned_2x2_64.png",
    ]:
        im = Image.open(path).convert("RGBA")
        zoom = im.resize((im.width * 4, im.height * 4), Image.Resampling.NEAREST)
        board = Image.new("RGBA", zoom.size, (180, 180, 180, 255)); bp = board.load()
        for y in range(board.height):
            for x in range(board.width):
                if ((x // 16) + (y // 16)) % 2:
                    bp[x, y] = (220, 220, 220, 255)
        board.alpha_composite(zoom)
        board.save(EVIDENCE / (path.stem + "_contact.png"))

    # Coarse-pixel source variant: the generator is explicitly asked for large
    # clusters and restrained surface texture before any target-grid fitting.
    coarse_cover_source = remove_chroma_green_background(
        Image.open(SOURCE / "sephiria_learned_heavy_cover_v2_source.png"))
    coarse_cover_128 = fit_prop(
        coarse_cover_source, canvas_size=(128, 128), target=(112, 84),
        contact_y=119, colors=15)
    coarse_cover_128 = replace_outermost_pixel_with_black(coarse_cover_128)
    coarse_cover_128.save(DEST / "sephiria_heavy_cover_coarse_2x2_128.png")
    coarse_cover_64 = quantize(
        coarse_cover_128.resize((64, 64), Image.Resampling.NEAREST), 11)
    coarse_cover_64 = replace_outermost_pixel_with_black(coarse_cover_64)
    coarse_cover_64.save(DEST / "sephiria_heavy_cover_coarse_2x2_64.png")
    for path in [
        DEST / "sephiria_heavy_cover_coarse_2x2_128.png",
        DEST / "sephiria_heavy_cover_coarse_2x2_64.png",
    ]:
        im = Image.open(path).convert("RGBA")
        zoom = im.resize((im.width * 4, im.height * 4), Image.Resampling.NEAREST)
        board = Image.new("RGBA", zoom.size, (180, 180, 180, 255)); bp = board.load()
        for y in range(board.height):
            for x in range(board.width):
                if ((x // 16) + (y // 16)) % 2:
                    bp[x, y] = (220, 220, 220, 255)
        board.alpha_composite(zoom)
        board.save(EVIDENCE / (path.stem + "_contact.png"))

    # Clean production experiment: reconstruct once on a coarse 64-grid, then
    # use exact 2x scaling for the 128 delivery. Outlines are still applied at
    # each final size so both deliveries retain exactly one native black pixel.
    clean_cover_source = remove_chroma_green_background(
        Image.open(SOURCE / "sephiria_clean_heavy_cover_v1_source.png"))
    clean_cover_grid = fit_prop_to_coarse_grid(clean_cover_source)
    clean_cover_grid = reserve_pure_black_for_outer_outline(clean_cover_grid)
    clean_cover_64 = replace_outermost_pixel_with_black(clean_cover_grid)
    clean_cover_64.save(DEST / "sephiria_heavy_cover_clean_2x2_64.png")
    clean_cover_128 = clean_cover_grid.resize((128, 128), Image.Resampling.NEAREST)
    clean_cover_128 = replace_outermost_pixel_with_black(clean_cover_128)
    clean_cover_128.save(DEST / "sephiria_heavy_cover_clean_2x2_128.png")
    for path in [
        DEST / "sephiria_heavy_cover_clean_2x2_128.png",
        DEST / "sephiria_heavy_cover_clean_2x2_64.png",
    ]:
        im = Image.open(path).convert("RGBA")
        zoom = im.resize((im.width * 4, im.height * 4), Image.Resampling.NEAREST)
        board = Image.new("RGBA", zoom.size, (180, 180, 180, 255)); bp = board.load()
        for y in range(board.height):
            for x in range(board.width):
                if ((x // 16) + (y // 16)) % 2:
                    bp[x, y] = (220, 220, 220, 255)
        board.alpha_composite(zoom)
        board.save(EVIDENCE / (path.stem + "_contact.png"))
    print(DEST)

if __name__ == "__main__": main()
