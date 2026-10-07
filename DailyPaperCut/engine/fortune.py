"""FortuneEngine — 公共日曆吉凶，宜忌取自農曆通書資料（lunar_python）。"""
from __future__ import annotations

from datetime import date
from typing import Any, Dict, List


from .paper_cards import to_traditional


LEVELS = [
    (90, "大吉"),
    (78, "吉"),
    (65, "中吉"),
    (52, "平"),
    (0, "慎"),
]


def _level(score: int) -> str:
    for threshold, name in LEVELS:
        if score >= threshold:
            return name
    return "平"


def _score_from_yi_ji(yi: List[str], ji: List[str], gan_zhi: str) -> int:
    # Deterministic public score from almanac richness + ganzhi hash
    base = 55 + min(25, len(yi) * 4) - min(18, len(ji) * 5)
    base += sum(ord(c) for c in gan_zhi) % 17
    return max(40, min(96, base))


def _lucky_color_from_stem(stem: str) -> str:
    mapping = {
        "甲": "木綠", "乙": "木青", "丙": "火紅", "丁": "火橙", "戊": "土黃",
        "己": "土褐", "庚": "金白", "辛": "金銀", "壬": "水藍", "癸": "水墨",
    }
    return to_traditional(mapping.get(stem, "米白"))


def _lucky_direction_from_branch(branch: str) -> str:
    mapping = {
        "子": "北", "丑": "東北", "寅": "東", "卯": "東", "辰": "東南", "巳": "南",
        "午": "南", "未": "西南", "申": "西", "酉": "西", "戌": "西北", "亥": "北",
    }
    return mapping.get(branch, "東")


def _lucky_time_from_branch(branch: str) -> str:
    hours = {
        "子": "23:00-01:00", "丑": "01:00-03:00", "寅": "03:00-05:00", "卯": "05:00-07:00",
        "辰": "07:00-09:00", "巳": "09:00-11:00", "午": "11:00-13:00", "未": "13:00-15:00",
        "申": "15:00-17:00", "酉": "17:00-19:00", "戌": "19:00-21:00", "亥": "21:00-23:00",
    }
    return hours.get(branch, "09:00-11:00")


def _theme_candidates(level: str, month: int, yi: List[str]) -> List[str]:
    season = ["冬藏", "立春氣象", "春生", "春暖", "夏長", "盛夏", "秋收前奏", "金秋", "秋實", "初冬", "冬藏", "歲末"][month - 1]
    pool = [
        "順水流財", "貴人靠近", "穩步累積", "心意相通", "家和氣旺",
        "學業精進", "創作萌發", "健康安養", season, "祥雲護行",
    ]
    # bias by first yi keyword
    if yi:
        pool.insert(0, f"{yi[0]}開運")
    if level in ("大吉", "吉"):
        return pool[:3]
    if level == "慎":
        return ["守中求穩", "整理節奏", pool[0]]
    return pool[:3]


def generate_fortune(d: date) -> Dict[str, Any]:
    try:
        from lunar_python import Solar
    except ImportError as e:
        raise RuntimeError("請安裝 lunar-python：pip install lunar-python") from e

    solar = Solar.fromYmd(d.year, d.month, d.day)
    lunar = solar.getLunar()
    gan_zhi = lunar.getDayInGanZhi()
    stem = gan_zhi[0] if gan_zhi else "甲"
    branch = gan_zhi[1] if len(gan_zhi) > 1 else "子"

    yi = [to_traditional(x) for x in (lunar.getDayYi() or [])]
    ji = [to_traditional(x) for x in (lunar.getDayJi() or [])]
    # keep readable length for social/video
    yi_show = yi[:6] if yi else ["祈福", "修造"]
    ji_show = ji[:4] if ji else ["安葬"]
    yi_show = [to_traditional(x) for x in yi_show]
    ji_show = [to_traditional(x) for x in ji_show]

    score = _score_from_yi_ji(yi_show, ji_show, gan_zhi)
    level = _level(score)

    roc_year = d.year - 1911
    lunar_md = f"農曆{lunar.getMonthInChinese()}月{lunar.getDayInChinese()}"
    # 字卡第一行：115年10月2日 農曆八月廿二
    date_line1 = f"{roc_year}年{d.month}月{d.day}日 {lunar_md}"
    year_gz = lunar.getYearInGanZhi() or ""
    month_gz = lunar.getMonthInGanZhi() or ""
    day_gz = gan_zhi or ""
    animal = to_traditional(lunar.getYearShengXiao() or "")
    # 字卡第二行：丙午 [馬] 年丁酉月己酉日
    date_line2 = f"{year_gz} [{animal}] 年{month_gz}月{day_gz}日"

    roc_text = f"{roc_year}年{d.month}月{d.day}日"
    lunar_text = lunar_md

    return {
        "date": d.isoformat(),
        "rocDate": roc_text,
        "lunarDate": lunar_text,
        "dateLine1": date_line1,
        "dateLine2": date_line2,
        "yearGanZhi": year_gz,
        "monthGanZhi": month_gz,
        "dayGanZhi": day_gz,
        "dayAnimalHint": animal,
        "fortuneLevel": level,
        "fortuneScore": score,
        "yi": yi_show,
        "ji": ji_show,
        "yiFull": yi,
        "jiFull": ji,
        "luckyColor": _lucky_color_from_stem(stem),
        "luckyDirection": _lucky_direction_from_branch(branch),
        "luckyTime": _lucky_time_from_branch(branch),
        "luckyElements": [stem, branch, "紙"],
        "themeCandidates": _theme_candidates(level, d.month, yi_show),
        "source": "FortuneEngine.v2.lunar_python",
        "note": "公共日曆吉凶；宜忌來自農曆通書資料，非個人命盤。",
    }
