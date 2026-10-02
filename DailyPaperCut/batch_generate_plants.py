#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Batch-generate Plants into the material pool (OpenAI images)."""
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
PLANTS = [
    ("牡丹", "Peony", ["富貴", "繁榮", "吉祥"]),
    ("蓮花", "Lotus", ["清淨", "圓滿", "智慧"]),
    ("梅花", "Plum Blossom", ["堅韌", "希望", "迎春"]),
    ("桃花", "Peach Blossom", ["感情", "人緣", "桃花運"]),
    ("桂花", "Osmanthus", ["富貴", "收穫", "好運"]),
    ("菊花", "Chrysanthemum", ["長壽", "平安", "高雅"]),
    ("蘭花", "Orchid", ["高雅", "品格", "人緣"]),
    ("竹子", "Bamboo", ["平安", "節節高升"]),
    ("松樹", "Pine Tree", ["長壽", "穩定", "堅韌"]),
    ("柳樹", "Willow", ["柔韌", "順遂", "生命力"]),
    ("櫻花", "Cherry Blossom", ["美好", "希望", "相遇"]),
    ("向日葵", "Sunflower", ["陽光", "正能量", "成功"]),
    ("百合", "Lily", ["和諧", "純潔", "家庭"]),
    ("紫藤", "Wisteria", ["浪漫", "長久", "幸福"]),
    ("薔薇", "Rose", ["愛情", "人緣", "美好"]),
    ("茉莉花", "Jasmine", ["純潔", "溫柔", "好人緣"]),
    ("水仙", "Narcissus", ["新年", "希望", "好運"]),
    ("山茶花", "Camellia", ["堅定", "美麗", "幸福"]),
    ("荷葉", "Lotus Leaf", ["圓滿", "平靜", "清雅"]),
    ("葫蘆藤", "Gourd Vine", ["福氣", "健康", "守護"]),
    ("葡萄藤", "Grape Vine", ["豐收", "富足", "家庭"]),
    ("石榴", "Pomegranate", ["多子多福", "繁榮"]),
    ("柿子樹", "Persimmon Tree", ["事事如意", "好事"]),
    ("枇杷樹", "Loquat Tree", ["豐收", "健康", "家庭"]),
    ("楓樹", "Maple Tree", ["秋季", "轉變", "成熟"]),
    ("銀杏樹", "Ginkgo Tree", ["長壽", "智慧", "守護"]),
    ("四葉草", "Four-leaf Clover", ["幸運", "希望", "機會"]),
    ("幸福樹", "Happiness Tree", ["幸福", "家庭", "好運"]),
    ("金錢樹", "Money Tree", ["財運", "富足", "招財"]),
    ("發財樹", "Pachira Tree", ["財富", "事業", "成長"]),
]


def main():
    log_path = ROOT / "materials" / "batch_plants_log.jsonl"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    ok, fail = 0, 0
    print(f"START batch plants n={len(PLANTS)}", flush=True)
    for i, (name, en, tags) in enumerate(PLANTS, 1):
        print(f"[{i}/{len(PLANTS)}] generating {name} ({en}) ...", flush=True)
        try:
            item = generate_pool_item(
                category="Plants",
                name=name,
                tags=tags + [en],
                role_hints=["support", "environment", "hero"],
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
        if i < len(PLANTS):
            time.sleep(1.2)
    summary = {"ok": ok, "fail": fail, "total": len(PLANTS)}
    (ROOT / "materials" / "batch_plants_summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"DONE {summary}", flush=True)


if __name__ == "__main__":
    main()
