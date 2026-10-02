#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Parallel batch: Nature materials (4 workers)."""
from __future__ import annotations

import json
import sys
import traceback
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))

try:
    from dotenv import load_dotenv
except ImportError:
    def load_dotenv(*args, **kwargs):
        return False

load_dotenv(ROOT / ".env")

from engine.material_pool import generate_pool_item

NATURE = [
    ("太陽", "Sun", ["光明", "希望", "活力"]),
    ("月亮", "Moon", ["寧靜", "感情", "思念"]),
    ("太陽光芒", "Sun Rays", ["能量", "成功", "好運"]),
    ("彩虹", "Rainbow", ["希望", "轉機", "幸福"]),
    ("流星", "Shooting Star", ["願望", "機會", "奇蹟"]),
    ("星星", "Stars", ["夢想", "指引", "願望"]),
    ("星河", "Milky Way", ["無限可能", "夢想"]),
    ("祥雲", "Auspicious Clouds", ["吉祥", "貴人", "好運"]),
    ("白雲", "White Clouds", ["自由", "平靜"]),
    ("彩色雲朵", "Colorful Clouds", ["幸福", "童趣", "好運"]),
    ("山峰", "Mountain Peak", ["成就", "穩定", "突破"]),
    ("遠山", "Distant Mountains", ["遠景", "方向", "人生"]),
    ("瀑布", "Waterfall", ["能量", "流動", "洗滌"]),
    ("河流", "River", ["順勢", "財運", "前進"]),
    ("湖泊", "Lake", ["平靜", "智慧", "內心"]),
    ("海浪", "Ocean Waves", ["活力", "突破", "旅行"]),
    ("海面", "Ocean", ["廣闊", "自由", "機會"]),
    ("水波", "Water Ripples", ["財運", "變化", "流動"]),
    ("朝霞", "Morning Glow", ["新開始", "希望"]),
    ("晚霞", "Evening Glow", ["收穫", "圓滿", "回顧"]),
    ("薄霧", "Morning Mist", ["神秘", "轉機", "未知"]),
    ("雨滴", "Raindrops", ["滋養", "淨化", "重生"]),
    ("雨後陽光", "Sun After Rain", ["轉運", "突破", "希望"]),
    ("雷電", "Lightning", ["力量", "突破", "覺醒"]),
    ("雪花", "Snowflakes", ["純潔", "寧靜", "新開始"]),
    ("楓葉飄落", "Falling Leaves", ["秋季", "轉變", "收穫"]),
    ("花瓣飛舞", "Floating Petals", ["浪漫", "祝福", "緣分"]),
    ("風", "Gentle Wind", ["自由", "順勢", "變化"]),
    ("光之粒子", "Golden Light Particles", ["能量", "幸運", "神秘"]),
    ("流光", "Flowing Light", ["時間", "運勢", "人生流動"]),
]

WORKERS = 4


def _one(i: int, name: str, en: str, tags: list):
    print(f"[{i}/{len(NATURE)}] START {name} ({en})", flush=True)
    item = generate_pool_item(
        category="Nature",
        name=name,
        tags=tags + [en],
        role_hints=["environment", "support", "auspicious"],
        use_placeholder=False,
    )
    print(f"[{i}/{len(NATURE)}] OK {item['id']} method={item.get('method')}", flush=True)
    return {"i": i, "ok": True, "id": item["id"], "method": item.get("method"), "file": item.get("file")}


def main():
    log_path = ROOT / "materials" / "batch_nature_log.jsonl"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    log_path.write_text("", encoding="utf-8")

    ok, fail = 0, 0
    print(f"START parallel nature n={len(NATURE)} workers={WORKERS}", flush=True)
    with ThreadPoolExecutor(max_workers=WORKERS) as ex:
        futs = {
            ex.submit(_one, i, name, en, tags): (i, name)
            for i, (name, en, tags) in enumerate(NATURE, 1)
        }
        for fut in as_completed(futs):
            i, name = futs[fut]
            try:
                row = fut.result()
                ok += 1
            except Exception as e:
                fail += 1
                row = {"i": i, "ok": False, "name": name, "error": str(e)}
                print(f"[{i}/{len(NATURE)}] FAIL {name}: {e}", flush=True)
                traceback.print_exc()
            with log_path.open("a", encoding="utf-8") as f:
                f.write(json.dumps(row, ensure_ascii=False) + "\n")

    summary = {"ok": ok, "fail": fail, "total": len(NATURE), "workers": WORKERS}
    (ROOT / "materials" / "batch_nature_summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"DONE {summary}", flush=True)


if __name__ == "__main__":
    main()
