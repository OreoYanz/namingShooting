"""ShortsTypography — large, phone-readable paper cards for 9:16 Shorts (V3).

Independent of GIF `paper_cards.STACK_Y` / small square sizes.
"""
from __future__ import annotations

from typing import Sequence, Tuple

from PIL import Image, ImageDraw, ImageFont

from .paper_cards import _font, _place_card, _translucent_block, to_traditional
from .theme_profiles import stars_from_level


# Shorts-only type scale (px @ 1080 wide)
TYPE = {
    "hook_title": 64,
    "hook_date": 48,
    "fortune": 56,
    "stars": 50,
    "yi_heading": 46,
    "yi_items": 36,
    "logo": 46,
    "logo_sub": 32,
}


def _paper_plate(
    size: Tuple[int, int],
    fill: Tuple[int, int, int, int],
    radius: int = 32,
) -> Image.Image:
    """Multi-layer soft paper plate (no black translucent bars)."""
    w, h = size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    # back sheet
    back = _translucent_block((w, h), (90, 70, 50, 40), radius=radius + 4)
    out.alpha_composite(back, (0, 4))
    mid = _translucent_block((w - 6, h - 6), (255, 250, 240, 90), radius=radius)
    out.alpha_composite(mid, (3, 2))
    plate = _translucent_block((w - 10, h - 10), fill, radius=max(8, radius - 2))
    out.alpha_composite(plate, (5, 4))
    return out


def render_hook_card(
    date_mmdd: str,
    title: str = "今日運勢",
    canvas_size: Tuple[int, int] = (1080, 1920),
    top_y: int | None = None,
) -> Image.Image:
    """Short hook: date + 今日運勢 (first ~1s)."""
    d = to_traditional(date_mmdd)
    t = to_traditional(title)
    card_w = int(canvas_size[0] * 0.78)
    card_h = 200
    plate = _paper_plate((card_w, card_h), (255, 252, 245, 230), radius=36)
    y = top_y if top_y is not None else int(canvas_size[1] * 0.08)
    out, x, y = _place_card(canvas_size, plate, y)
    draw = ImageDraw.Draw(out)
    fd = _font(TYPE["hook_date"], bold=True)
    ft = _font(TYPE["hook_title"], bold=True)
    tw1 = draw.textlength(d, font=fd)
    tw2 = draw.textlength(t, font=ft)
    draw.text((x + (card_w - tw1) / 2, y + 36), d, font=fd, fill=(110, 75, 45, 255))
    draw.text((x + (card_w - tw2) / 2, y + 100), t, font=ft, fill=(70, 42, 28, 255))
    return out


def render_fortune_paper_card(
    level: str,
    stars: int | None = None,
    canvas_size: Tuple[int, int] = (1080, 1920),
    top_y: int | None = None,
    label: str = "今日運勢",
) -> Image.Image:
    """Paper fortune card: stars + 今日大吉 (no black bar)."""
    level_t = to_traditional(level)
    label_t = to_traditional(label)
    n = stars if stars is not None else stars_from_level(str(level))
    n = max(1, min(5, int(n)))
    star_line = "★" * n + "☆" * (5 - n)
    headline = f"今日{level_t}" if not level_t.startswith("今日") else level_t

    card_w = int(canvas_size[0] * 0.82)
    card_h = 260
    # soft cream / gold-tinted paper
    plate = _paper_plate((card_w, card_h), (255, 248, 232, 235), radius=40)
    y = top_y if top_y is not None else int(canvas_size[1] * 0.58)
    out, x, y = _place_card(canvas_size, plate, y)
    draw = ImageDraw.Draw(out)

    fl = _font(32, bold=True)
    fs = _font(TYPE["stars"], bold=True)
    fh = _font(TYPE["fortune"], bold=True)
    tw0 = draw.textlength(label_t, font=fl)
    tw1 = draw.textlength(star_line, font=fs)
    tw2 = draw.textlength(headline, font=fh)
    draw.text((x + (card_w - tw0) / 2, y + 28), label_t, font=fl, fill=(140, 100, 60, 255))
    draw.text((x + (card_w - tw1) / 2, y + 78), star_line, font=fs, fill=(210, 160, 50, 255))
    draw.text((x + (card_w - tw2) / 2, y + 160), headline, font=fh, fill=(80, 45, 28, 255))
    return out


def render_yi_ji_shorts(
    heading: str,
    items: Sequence[str],
    *,
    tone: str = "yi",
    canvas_size: Tuple[int, int] = (1080, 1920),
    top_y: int | None = None,
    max_items: int = 3,
) -> Image.Image:
    """Large 宜/忌 block — max 3 items, phone-readable."""
    lines = [to_traditional(str(x).strip()) for x in items if str(x).strip()][:max_items]
    body = "・".join(lines) if lines else "—"
    if tone == "ji":
        mark = "✕ 今日忌"
        fill = (235, 235, 235, 235)
        ink = (70, 70, 70, 255)
        mark_ink = (100, 60, 60, 255)
    else:
        mark = "✓ 今日宜"
        fill = (255, 228, 220, 235)
        ink = (90, 40, 35, 255)
        mark_ink = (160, 50, 40, 255)

    mark = to_traditional(mark)
    card_w = int(canvas_size[0] * 0.84)
    card_h = 220
    plate = _paper_plate((card_w, card_h), fill, radius=36)
    y = top_y if top_y is not None else int(canvas_size[1] * 0.72)
    out, x, y = _place_card(canvas_size, plate, y)
    draw = ImageDraw.Draw(out)

    fh = _font(TYPE["yi_heading"], bold=True)
    fb = _font(TYPE["yi_items"], bold=True)
    # shrink body if needed
    max_inner = card_w - 56
    size = TYPE["yi_items"]
    while size >= 28 and draw.textlength(body, font=fb) > max_inner:
        size -= 2
        fb = _font(size, bold=True)

    tw1 = draw.textlength(mark, font=fh)
    tw2 = draw.textlength(body, font=fb)
    draw.text((x + (card_w - tw1) / 2, y + 40), mark, font=fh, fill=mark_ink)
    draw.text((x + (card_w - tw2) / 2, y + 120), body, font=fb, fill=ink)
    return out


def render_brand_shorts(
    brand: str = "名序",
    subtitle: str = "每日運勢",
    canvas_size: Tuple[int, int] = (1080, 1920),
    top_y: int | None = None,
) -> Image.Image:
    """Final-second brand — soft fade target; no CTA / shop copy."""
    brand = to_traditional(brand)
    subtitle = to_traditional(subtitle)
    card_w = int(canvas_size[0] * 0.70)
    card_h = 160
    plate = _paper_plate((card_w, card_h), (255, 252, 247, 220), radius=32)
    y = top_y if top_y is not None else int(canvas_size[1] * 0.91)
    # keep within safe bottom
    y = min(y, int(canvas_size[1] * 0.90))
    out, x, y = _place_card(canvas_size, plate, y)
    draw = ImageDraw.Draw(out)
    fb = _font(TYPE["logo"], bold=True)
    fs = _font(TYPE["logo_sub"], bold=True)
    tw1 = draw.textlength(brand, font=fb)
    tw2 = draw.textlength(subtitle, font=fs)
    draw.text((x + (card_w - tw1) / 2, y + 28), brand, font=fb, fill=(75, 50, 35, 255))
    draw.text((x + (card_w - tw2) / 2, y + 92), subtitle, font=fs, fill=(120, 90, 60, 255))
    return out


def format_mmdd(daily: dict) -> str:
    """Prefer M/D from dateKey / date."""
    key = str(daily.get("dateKey") or "")
    if len(key) == 8 and key.isdigit():
        return f"{int(key[4:6])}/{int(key[6:8])}"
    iso = str(daily.get("date") or "")
    if len(iso) >= 10 and iso[4] == "-" and iso[7] == "-":
        return f"{int(iso[5:7])}/{int(iso[8:10])}"
    return key or iso or "今日"
