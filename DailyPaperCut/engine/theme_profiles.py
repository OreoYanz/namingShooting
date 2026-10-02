"""Theme profiles — map daily theme strings to Hero / Accent / support slots."""
from __future__ import annotations

from typing import Any, Dict, List


THEME_PROFILES: Dict[str, Dict[str, Any]] = {
    "財運": {
        "hero_categories": ["Hero"],
        "support_categories": ["Nature", "Plants"],
        "accent_categories": ["Accent"],
        "preferred_tags": ["財運", "富足", "水", "金", "wealth", "gold", "koi", "錦鯉", "金幣", "元寶", "fortune"],
        "template": "fortune_scene",
        "label": "今日財運",
    },
    "事業": {
        "hero_categories": ["Hero"],
        "support_categories": ["Plants", "Nature"],
        "accent_categories": ["Accent"],
        "preferred_tags": ["事業", "成功", "成長", "突破", "career", "flag", "mountain", "高峰", "旗幟", "階梯"],
        "template": "career_scene",
        "label": "今日事業",
    },
    "愛情": {
        "hero_categories": ["Hero"],
        "support_categories": ["Nature", "Plants"],
        "accent_categories": ["Accent"],
        "preferred_tags": ["愛情", "桃花", "緣分", "幸福", "love", "peach", "butterfly", "蝴蝶", "玫瑰", "紅線"],
        "template": "love_scene",
        "label": "今日緣分",
    },
    "綜合": {
        "hero_categories": ["Hero"],
        "support_categories": ["Nature", "Plants"],
        "accent_categories": ["Accent"],
        "preferred_tags": [],
        "template": "balanced_scene",
        "label": "今日運勢",
    },
}

# Keyword → profile key (matched against actual fortune theme strings)
_THEME_KEYWORDS = [
    ("財運", ["財", "錢", "富", "金", "水流", "聚財", "開運", "錦鯉", "收入"]),
    ("事業", ["事業", "功", "升", "職", "學業", "創作", "突破", "旗", "峰", "累積", "穩步"]),
    ("愛情", ["愛", "情", "緣", "桃花", "心意", "家和", "人緣", "雙"]),
]


def resolve_theme_profile(theme: str) -> Dict[str, Any]:
    """Map any existing theme string onto a profile without renaming source data."""
    text = theme or ""
    for key, words in _THEME_KEYWORDS:
        if any(w in text for w in words):
            profile = dict(THEME_PROFILES[key])
            profile["profileKey"] = key
            profile["sourceTheme"] = theme
            return profile
    profile = dict(THEME_PROFILES["綜合"])
    profile["profileKey"] = "綜合"
    profile["sourceTheme"] = theme
    return profile


def stars_from_level(level: str) -> int:
    mapping = {"大吉": 5, "吉": 4, "中吉": 3, "平": 2, "慎": 1}
    return mapping.get(level, 3)
