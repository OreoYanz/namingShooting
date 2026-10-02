#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Batch-generate Animals into the material pool (OpenAI images)."""
from __future__ import annotations

import json
import sys
import time
import traceback
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

# (name_zh, name_en, tags)
ANIMALS = [
    ("龍", "Dragon", ["吉祥", "權勢", "祥瑞"]),
    ("鳳凰", "Phoenix", ["重生", "榮耀", "好運"]),
    ("錦鯉", "Koi Fish", ["財運", "好運"]),
    ("兔子", "Rabbit", ["溫柔", "機會", "活力"]),
    ("鹿", "Deer", ["福氣", "平安", "長壽"]),
    ("蝴蝶", "Butterfly", ["轉變", "自由", "感情"]),
    ("孔雀", "Peacock", ["美麗", "榮耀", "人緣"]),
    ("白鶴", "Crane", ["長壽", "吉祥"]),
    ("喜鵲", "Magpie", ["喜事", "好消息"]),
    ("燕子", "Swallow", ["歸來", "家庭", "希望"]),
    ("貓", "Cat", ["陪伴", "招財", "幸運"]),
    ("招財貓", "Maneki-neko", ["財運", "招財"]),
    ("狐狸", "Fox", ["智慧", "機敏"]),
    ("熊貓", "Panda", ["和諧", "平安"]),
    ("大象", "Elephant", ["穩定", "智慧", "好運"]),
    ("大象守護", "Elephant Guardian", ["穩定", "守護"]),
    ("老虎", "Tiger", ["勇氣", "力量"]),
    ("獅子", "Lion", ["領導", "勇氣"]),
    ("馬", "Horse", ["事業", "速度", "成功"]),
    ("羊", "Sheep", ["溫和", "和氣", "福氣"]),
    ("牛", "Ox", ["勤勞", "穩定", "收穫"]),
    ("猴子", "Monkey", ["聰明", "靈活"]),
    ("狗", "Dog", ["守護", "忠誠"]),
    ("海豚", "Dolphin", ["幸福", "智慧", "人際"]),
    ("鯨魚", "Whale", ["寬廣", "力量", "守護"]),
    ("天鵝", "Swan", ["愛情", "優雅", "和諧"]),
    ("蜻蜓", "Dragonfly", ["轉機", "自由", "好運"]),
    ("蜜蜂", "Bee", ["勤奮", "合作", "收穫"]),
    ("金魚", "Goldfish", ["財富", "富足"]),
    ("烏龜", "Turtle", ["長壽", "穩定", "守成"]),
]


def main():
    log_path = ROOT / "materials" / "batch_animals_log.jsonl"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    ok, fail = 0, 0
    print(f"START batch animals n={len(ANIMALS)}", flush=True)
    for i, (name, en, tags) in enumerate(ANIMALS, 1):
        print(f"[{i}/{len(ANIMALS)}] generating {name} ({en}) ...", flush=True)
        try:
            item = generate_pool_item(
                category="Animals",
                name=name,
                tags=tags + [en],
                role_hints=["hero", "support"],
                use_placeholder=False,
            )
            ok += 1
            row = {"i": i, "ok": True, "id": item["id"], "method": item.get("method"), "file": item.get("file")}
            print(f"  OK {item['id']} method={item.get('method')}", flush=True)
        except Exception as e:
            fail += 1
            row = {"i": i, "ok": False, "name": name, "error": str(e)}
            print(f"  FAIL {name}: {e}", flush=True)
            traceback.print_exc()
        with log_path.open("a", encoding="utf-8") as f:
            f.write(json.dumps(row, ensure_ascii=False) + "\n")
        # gentle pacing for rate limits
        if i < len(ANIMALS):
            time.sleep(1.2)
    summary = {"ok": ok, "fail": fail, "total": len(ANIMALS)}
    (ROOT / "materials" / "batch_animals_summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"DONE {summary}", flush=True)


if __name__ == "__main__":
    main()
