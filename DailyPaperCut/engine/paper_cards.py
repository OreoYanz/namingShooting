"""Paper Info Cards — 日期 / 宜忌色塊 / Logo（繁體、上→下堆疊常駐）."""
from __future__ import annotations

from pathlib import Path
from typing import Sequence, Tuple

from PIL import Image, ImageDraw, ImageFont

from .theme_profiles import stars_from_level


# Top-down stack Y positions (1080 canvas baseline; scaled by height ratio)
STACK_Y = {
    "date": 0.08,
    "yi": 0.30,
    "ji": 0.40,
    "summary": 0.52,  # 忌下方：今日總結
    "logo": 0.82,
}


def _font(size: int, *, bold: bool = False) -> ImageFont.ImageFont:
    if bold:
        candidates = [
            Path(r"C:\Windows\Fonts\msjhbd.ttc"),  # 微軟正黑體 Bold
            Path(r"C:\Windows\Fonts\msyhbd.ttc"),
            Path(r"C:\Windows\Fonts\simhei.ttf"),
            Path(r"C:\Windows\Fonts\mingliub.ttc"),
            Path("/System/Library/Fonts/PingFang.ttc"),
            Path("/usr/share/fonts/truetype/noto/NotoSansCJKtc-Bold.otf"),
            Path(r"C:\Windows\Fonts\msjh.ttc"),
        ]
    else:
        candidates = [
            Path(r"C:\Windows\Fonts\msjhbd.ttc"),
            Path(r"C:\Windows\Fonts\msjh.ttc"),
            Path(r"C:\Windows\Fonts\mingliu.ttc"),
            Path(r"C:\Windows\Fonts\msyh.ttc"),
            Path("/System/Library/Fonts/PingFang.ttc"),
            Path("/usr/share/fonts/truetype/noto/NotoSansCJKtc-Regular.otf"),
        ]
    for p in candidates:
        if p.exists():
            try:
                return ImageFont.truetype(str(p), size=size)
            except Exception:
                continue
    return ImageFont.load_default()


def to_traditional(text: str) -> str:
    """Force Traditional Chinese for on-screen copy."""
    s = str(text or "")
    try:
        from opencc import OpenCC

        return OpenCC("s2t").convert(s)
    except Exception:
        pass
    try:
        import zhconv

        return zhconv.convert(s, "zh-tw")
    except Exception:
        pass
    table = str.maketrans(
        {
            "东": "東", "气": "氣", "门": "門", "开": "開", "关": "關", "发": "發",
            "财": "財", "运": "運", "动": "動", "处": "處", "丧": "喪", "坟": "墳",
            "扫": "掃", "舍": "捨", "术": "術", "医": "醫", "药": "藥", "针": "針",
            "织": "織", "经": "經", "络": "絡", "龙": "龍", "马": "馬", "鸟": "鳥",
            "鱼": "魚", "龟": "龜", "银": "銀", "钱": "錢", "贡": "貢", "纳": "納",
            "进": "進", "远": "遠", "连": "連", "边": "邊", "过": "過", "还": "還",
            "选": "選", "择": "擇", "时": "時", "间": "間", "会": "會", "议": "議",
            "订": "訂", "约": "約", "亲": "親", "继": "繼", "续": "續", "残": "殘",
            "杀": "殺", "斋": "齋", "竖": "豎", "梁": "樑", "启": "啟", "攒": "攢",
            "传": "傳", "书": "書", "词": "詞", "讼": "訟", "狱": "獄", "缝": "縫",
        }
    )
    for a, b in (("词讼", "詞訟"), ("安门", "安門"), ("纳畜", "納畜"), ("装修", "裝修")):
        s = s.replace(a, b)
    return s.translate(table)


def _translucent_block(
    size: Tuple[int, int],
    fill: Tuple[int, int, int, int],
    radius: int = 24,
) -> Image.Image:
    w, h = size
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((0, 0, w - 1, h - 1), radius=radius, fill=fill)
    return im


def _place_card(
    canvas_size: Tuple[int, int],
    plate: Image.Image,
    top_y: int,
) -> Tuple[Image.Image, int, int]:
    out = Image.new("RGBA", canvas_size, (0, 0, 0, 0))
    card_w, card_h = plate.size
    x = (canvas_size[0] - card_w) // 2
    shadow = _translucent_block((card_w, card_h), (60, 45, 30, 55), radius=22)
    out.alpha_composite(shadow, (x + 3, top_y + 5))
    out.alpha_composite(plate, (x, top_y))
    return out, x, top_y


def render_date_card(
    line1: str,
    line2: str,
    canvas_size: Tuple[int, int] = (1080, 1080),
    top_y: int | None = None,
) -> Image.Image:
    """兩行日期：中華民國…農曆… / 干支年[生肖]月日."""
    l1 = to_traditional(line1)
    l2 = to_traditional(line2)
    card_w, card_h = 860, 150
    plate = _translucent_block((card_w, card_h), (255, 252, 245, 235), radius=26)
    y = top_y if top_y is not None else int(canvas_size[1] * STACK_Y["date"])
    out, x, y = _place_card(canvas_size, plate, y)
    draw = ImageDraw.Draw(out)
    max_inner = card_w - 48
    size1, size2 = 36, 32
    f1 = _font(size1, bold=True)
    while size1 >= 26 and draw.textlength(l1, font=f1) > max_inner:
        size1 -= 2
        f1 = _font(size1, bold=True)
    f2 = _font(size2, bold=True)
    while size2 >= 24 and draw.textlength(l2, font=f2) > max_inner:
        size2 -= 2
        f2 = _font(size2, bold=True)
    tw1 = draw.textlength(l1, font=f1)
    tw2 = draw.textlength(l2, font=f2)
    draw.text((x + (card_w - tw1) / 2, y + 28), l1, font=f1, fill=(80, 48, 32, 255))
    draw.text((x + (card_w - tw2) / 2, y + 88), l2, font=f2, fill=(100, 75, 50, 255))
    return out


def render_yi_ji_block(
    heading: str,
    items: Sequence[str],
    *,
    tone: str = "yi",
    canvas_size: Tuple[int, int] = (1080, 1080),
    top_y: int | None = None,
) -> Image.Image:
    """單行：宜：內容 / 忌：內容（不換行）+ 較不透明色塊."""
    mark = "忌" if tone == "ji" else "宜"
    lines = [to_traditional(str(x).strip()) for x in items if str(x).strip()][:8]
    body = "、".join(lines) if lines else "—"
    text = f"{mark}：{body}"

    if tone == "ji":
        fill = (220, 220, 220, 235)  # 灰色
        ink = (90, 90, 90, 255)
        default_y = STACK_Y["ji"]
    else:
        fill = (255, 210, 210, 235)  # 紅色（宜）
        ink = (150, 45, 40, 255)
        default_y = STACK_Y["yi"]

    probe = ImageDraw.Draw(Image.new("RGBA", (1, 1)))
    max_w = int(canvas_size[0] * 0.82)
    size = 36
    bf = _font(size, bold=True)
    while size >= 22:
        bf = _font(size, bold=True)
        if probe.textlength(text, font=bf) <= max_w - 48:
            break
        size -= 2

    text_w = int(probe.textlength(text, font=bf))
    card_w = min(max_w, max(420, text_w + 56))
    card_h = 92
    plate = _translucent_block((card_w, card_h), fill, radius=22)
    y = top_y if top_y is not None else int(canvas_size[1] * default_y)
    out, x, y = _place_card(canvas_size, plate, y)
    draw = ImageDraw.Draw(out)
    tw = draw.textlength(text, font=bf)
    draw.text((x + (card_w - tw) / 2, y + (card_h - size) / 2 - 2), text, font=bf, fill=ink)
    return out


def render_summary_card(
    text: str,
    canvas_size: Tuple[int, int] = (1080, 1080),
    top_y: int | None = None,
    title: str = "今日總結",
) -> Image.Image:
    """忌下方的今日總結字卡（單行主文，過長則縮小字級）。"""
    title = to_traditional(title)
    body = to_traditional(str(text or "").strip()) or "—"
    # 去掉多餘換行，壓成單行
    body = " ".join(body.replace("\n", " ").split())
    line = f"{title}：{body}"

    probe = ImageDraw.Draw(Image.new("RGBA", (1, 1)))
    max_w = int(canvas_size[0] * 0.88)
    size = 32
    font = _font(size, bold=True)
    while size >= 20:
        font = _font(size, bold=True)
        if probe.textlength(line, font=font) <= max_w - 48:
            break
        size -= 2

    text_w = int(probe.textlength(line, font=font))
    card_w = min(max_w, max(480, text_w + 56))
    card_h = 92
    plate = _translucent_block((card_w, card_h), (255, 245, 230, 240), radius=22)  # 暖米色
    y = top_y if top_y is not None else int(canvas_size[1] * STACK_Y["summary"])
    out, x, y = _place_card(canvas_size, plate, y)
    draw = ImageDraw.Draw(out)
    tw = draw.textlength(line, font=font)
    draw.text((x + (card_w - tw) / 2, y + (card_h - size) / 2 - 2), line, font=font, fill=(110, 70, 40, 255))
    return out


def render_logo_slogan_card(
    brand: str = "名序",
    slogan: str = "新生兒命名 ‧ 專業改名 ‧ 流年運勢",
    canvas_size: Tuple[int, int] = (1080, 1080),
    top_y: int | None = None,
) -> Image.Image:
    brand = to_traditional(brand)
    slogan = to_traditional(slogan)
    # 單行：名序 | slogan
    line = f"{brand} | {slogan}" if slogan else brand

    probe = ImageDraw.Draw(Image.new("RGBA", (1, 1)))
    max_w = int(canvas_size[0] * 0.92)
    size = 36
    font = _font(size, bold=True)
    while size >= 18:
        font = _font(size, bold=True)
        if probe.textlength(line, font=font) <= max_w - 40:
            break
        size -= 2

    text_w = int(probe.textlength(line, font=font))
    card_w = min(max_w, max(480, text_w + 48))
    card_h = 88
    plate = _translucent_block((card_w, card_h), (255, 252, 247, 240), radius=24)
    y = top_y if top_y is not None else int(canvas_size[1] * STACK_Y["logo"])
    out, x, y = _place_card(canvas_size, plate, y)
    draw = ImageDraw.Draw(out)
    tw = draw.textlength(line, font=font)
    draw.text((x + (card_w - tw) / 2, y + (card_h - size) / 2 - 2), line, font=font, fill=(95, 65, 40, 255))
    return out


def render_fortune_card(theme: str, fortune_level, canvas_size: Tuple[int, int] = (1080, 1080)) -> Image.Image:
    n = stars_from_level(str(fortune_level)) if not isinstance(fortune_level, int) else fortune_level
    return render_date_card(to_traditional(str(theme)), f"{'★' * int(n)}", canvas_size)


def render_list_card(
    heading: str,
    items: Sequence[str],
    *,
    mark: str = "✓",
    canvas_size: Tuple[int, int] = (1080, 1080),
    tone: str = "yi",
) -> Image.Image:
    return render_yi_ji_block(heading, items, tone=tone, canvas_size=canvas_size)


def render_logo_card(text: str = "名序", canvas_size: Tuple[int, int] = (1080, 1080)) -> Image.Image:
    parts = text.split("｜", 1) if "｜" in text else text.split("|", 1)
    brand = parts[0].strip() or "名序"
    slogan = parts[1].strip() if len(parts) > 1 else "新生兒命名 ‧ 專業改名 ‧ 流年運勢"
    return render_logo_slogan_card(brand=brand, slogan=slogan, canvas_size=canvas_size)


def apply_overlay(base: Image.Image, overlay: Image.Image, opacity: float, scale: float = 1.0) -> Image.Image:
    if opacity <= 0.01:
        return base
    ov = overlay
    if abs(scale - 1.0) > 0.01:
        nw = max(1, int(ov.size[0] * scale))
        nh = max(1, int(ov.size[1] * scale))
        resized = ov.resize((nw, nh), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", ov.size, (0, 0, 0, 0))
        canvas.alpha_composite(resized, ((ov.size[0] - nw) // 2, (ov.size[1] - nh) // 2))
        ov = canvas
    if opacity < 0.999:
        r, g, b, a = ov.split()
        a = a.point(lambda x, o=opacity: int(x * opacity))
        ov = Image.merge("RGBA", (r, g, b, a))
    out = base.copy()
    out.alpha_composite(ov)
    return out
