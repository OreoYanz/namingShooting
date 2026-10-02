#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Parallel batch: Season materials (4 workers)."""
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

SEASON = [
    ("春日嫩芽", "Spring Sprouts", ["新生", "希望", "開始"]),
    ("春日花瓣", "Spring Petals", ["浪漫", "活力", "桃花"]),
    ("春雨", "Spring Rain", ["滋養", "成長", "轉機"]),
    ("春燕", "Spring Swallow", ["歸來", "新開始"]),
    ("春日蝴蝶", "Spring Butterfly", ["相遇", "轉變"]),
    ("夏日陽光", "Summer Sunshine", ["活力", "成功", "熱情"]),
    ("夏日海浪", "Summer Waves", ["自由", "旅行", "活力"]),
    ("夏日荷葉", "Summer Lotus Leaves", ["平靜", "清新"]),
    ("夏日冰飲", "Summer Cool Drink", ["輕鬆", "歡樂"]),
    ("夏日螢火蟲", "Summer Fireflies", ["浪漫", "希望"]),
    ("秋日楓葉", "Autumn Maple Leaves", ["收穫", "轉變"]),
    ("秋日銀杏", "Autumn Ginkgo Leaves", ["智慧", "成熟"]),
    ("秋日稻穗", "Autumn Rice Ears", ["豐收", "財富"]),
    ("秋日果實", "Autumn Fruits", ["成果", "富足"]),
    ("秋日南瓜", "Autumn Pumpkin", ["豐收", "溫暖"]),
    ("秋日落葉", "Falling Autumn Leaves", ["放下", "轉變"]),
    ("秋日微風", "Autumn Breeze", ["順勢", "變化"]),
    ("冬日雪花", "Winter Snowflakes", ["純潔", "新開始"]),
    ("冬日霜花", "Winter Frost Flowers", ["堅韌", "沉澱"]),
    ("冬日松枝", "Winter Pine Branch", ["長青", "守護"]),
    ("冬日暖陽", "Winter Sunlight", ["溫暖", "希望"]),
    ("冬日熱茶", "Winter Tea", ["平靜", "療癒"]),
    ("冬日雪景", "Winter Snow Scene", ["寧靜", "純潔"]),
    ("四季風", "Seasonal Wind", ["流動", "變化"]),
    ("四季彩蝶", "Seasonal Butterfly", ["成長", "蛻變"]),
    ("四季花環", "Seasonal Flower Wreath", ["圓滿", "循環"]),
    ("四季樹", "Four Seasons Tree", ["成長", "人生"]),
    ("四季日曆", "Seasonal Calendar", ["時間", "流年"]),
    ("節氣輪盤", "Solar Term Wheel", ["節氣", "時間", "流轉"]),
    ("四季流光", "Four Seasons Light", ["時間", "運勢", "人生流轉"]),
]

WORKERS = 4


def _one(i: int, name: str, en: str, tags: list):
    print(f"[{i}/{len(SEASON)}] START {name} ({en})", flush=True)
    item = generate_pool_item(
        category="Season",
        name=name,
        tags=tags + [en],
        role_hints=["support", "environment", "auspicious"],
        use_placeholder=False,
    )
    print(f"[{i}/{len(SEASON)}] OK {item['id']} method={item.get('method')}", flush=True)
    return {"i": i, "ok": True, "id": item["id"], "method": item.get("method"), "file": item.get("file")}


def main():
    log_path = ROOT / "materials" / "batch_season_log.jsonl"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    log_path.write_text("", encoding="utf-8")

    ok, fail = 0, 0
    print(f"START parallel season n={len(SEASON)} workers={WORKERS}", flush=True)
    with ThreadPoolExecutor(max_workers=WORKERS) as ex:
        futs = {
            ex.submit(_one, i, name, en, tags): (i, name)
            for i, (name, en, tags) in enumerate(SEASON, 1)
        }
        for fut in as_completed(futs):
            i, name = futs[fut]
            try:
                row = fut.result()
                ok += 1
            except Exception as e:
                fail += 1
                row = {"i": i, "ok": False, "name": name, "error": str(e)}
                print(f"[{i}/{len(SEASON)}] FAIL {name}: {e}", flush=True)
                traceback.print_exc()
            with log_path.open("a", encoding="utf-8") as f:
                f.write(json.dumps(row, ensure_ascii=False) + "\n")

    summary = {"ok": ok, "fail": fail, "total": len(SEASON), "workers": WORKERS}
    (ROOT / "materials" / "batch_season_summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"DONE {summary}", flush=True)


if __name__ == "__main__":
    main()
