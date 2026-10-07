# -*- coding: utf-8 -*-
"""Fail if docs/site contain clear Simplified Chinese forms."""
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

# Simplified-only glyphs (not shared with Taiwan Traditional usage).
SIMP_CHARS = set(
    "国发对开关长门问过还进这从为们读语论议记设证识详调"
    "财质购费资车转运选适钟钱页风马验简体农网录询"
)
SIMP_CHARS.add("\u9ec4")  # 黄
SIMP_CHARS.add("讳")

SIMP_WORDS = (
    "什么",
    "怎么",
    "怎么样",
    "发现",
    "网络",
    "简体",
    "农历",
    "黄历",
    "忌讳",
)


def main() -> int:
    hits: list[str] = []
    for root_name in ("docs", "site"):
        root = ROOT / root_name
        if not root.exists():
            continue
        for path in root.rglob("*"):
            if path.suffix.lower() not in {".html", ".js", ".json", ".xml", ".md", ".txt", ".css"}:
                continue
            try:
                text = path.read_text(encoding="utf-8")
            except Exception:
                continue
            found_chars = sorted({ch for ch in text if ch in SIMP_CHARS})
            found_words = [w for w in SIMP_WORDS if w in text]
            # Special: 土黄 with simplified 黄
            if "土\u9ec4" in text:
                found_words.append("土黄(简)")
            if found_chars or found_words:
                rel = path.relative_to(ROOT).as_posix()
                detail = []
                if found_chars:
                    detail.append("chars=" + "".join(found_chars))
                if found_words:
                    detail.append("words=" + ",".join(found_words))
                hits.append(f"{rel}: {'; '.join(detail)}")

    if hits:
        print("Simplified Chinese detected:")
        for h in hits:
            print(" ", h)
        return 1
    print("OK: docs/site Traditional Chinese check passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
