# -*- coding: utf-8 -*-
"""Rebuild winforms/_gen/hot_names.json from MOI 112 namestat + forward fill."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "winforms" / "_gen" / "hot_names.json"

# 內政部《全國姓名統計分析》112年版表五十六（基準日：112/6/30）
# https://www.ris.gov.tw/documents/data/5/2/112namestat.pdf
M_100_109 = ["承恩", "宥廷", "品睿", "宸睿", "宇恩", "宇翔", "承翰", "宥辰", "柏睿", "睿恩"]
F_100_109 = ["品妍", "子晴", "詠晴", "品妤", "禹彤", "羽彤", "芯語", "宥蓁", "語彤", "苡晴"]
M_110_112 = ["恩碩", "宥廷", "子睿", "承恩", "品睿", "宇恩", "宸睿", "睿恩", "子宸", "子恩"]
F_110_112 = ["品妍", "苡菲", "雨霏", "芸菲", "芯語", "苡安", "玥彤", "羽彤", "子晴", "禹彤"]

# 113 年起官方尚未出版新版姓名統計；延續 110–112/6 官方榜，
# 並把 2025 年民間流通前段名單中、與官方榜可銜接者併入（非戶政司年刊）。
M_114 = ["恩碩", "宥廷", "品睿", "子睿", "俊熙", "承恩", "宇恩", "宸睿", "睿恩", "子宸"]
F_114 = ["品妍", "心玥", "雨霏", "苡菲", "子晴", "芸菲", "芯語", "苡安", "玥彤", "羽彤"]
M_115 = ["恩碩", "宥廷", "子睿", "品睿", "承恩", "宇恩", "允辰", "宸睿", "沐陽", "子恩"]
F_115 = ["品妍", "苡菲", "雨霏", "苡安", "心玥", "子晴", "芯語", "玥彤", "語棠", "芸菲"]


def rows(year: int, gender: str, names: list[str]) -> list[dict]:
    return [
        {"year": year, "gender": gender, "given": name, "rank": i}
        for i, name in enumerate(names, start=1)
    ]


def main() -> None:
    data: list[dict] = []
    # 109：落在 100–109 世代官方榜
    data += rows(109, "F", F_100_109)
    data += rows(109, "M", M_100_109)
    # 110–112：官方 110 年至 112 年 6 月榜（分年沿用同一官方世代榜）
    for y in (110, 111, 112):
        data += rows(y, "F", F_110_112)
        data += rows(y, "M", M_110_112)
    # 113：尚無年刊，先沿用最新官方榜
    data += rows(113, "F", F_110_112)
    data += rows(113, "M", M_110_112)
    # 114–115：官方榜 + 民間流通熱門名銜接，涵蓋至 2026 上半年
    data += rows(114, "F", F_114)
    data += rows(114, "M", M_114)
    data += rows(115, "F", F_115)
    data += rows(115, "M", M_115)

    OUT.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    years = sorted({x["year"] for x in data})
    print("wrote", OUT, "entries", len(data), "years", years)


if __name__ == "__main__":
    main()
