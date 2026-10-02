#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Parallel batch: Fortune materials (4 workers)."""
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

FORTUNE = [
    ("元寶", "Gold Ingot", ["財富", "聚財"]),
    ("金幣", "Gold Coins", ["收入", "財運"]),
    ("聚寶盆", "Treasure Bowl", ["聚財", "財源"]),
    ("金元寶樹", "Golden Money Tree", ["財富成長"]),
    ("紅包", "Red Envelope", ["福氣", "財運"]),
    ("福袋", "Fortune Bag", ["福氣", "好運"]),
    ("如意", "Ruyi Scepter", ["如意", "順遂"]),
    ("錦鯉", "Lucky Koi", ["好運", "財運"]),
    ("招財貓", "Lucky Cat", ["招財", "生意"]),
    ("金色祥雲", "Golden Auspicious Cloud", ["吉祥", "貴人"]),
    ("聚財金葫蘆", "Golden Gourd", ["福氣", "守財"]),
    ("金色聚寶箱", "Golden Treasure Chest", ["財富", "寶藏"]),
    ("金色鑰匙", "Golden Key", ["機會", "財富之門"]),
    ("財富之門", "Fortune Gate", ["新機會", "財運"]),
    ("金色皇冠", "Golden Crown", ["成功", "地位"]),
    ("金色獎盃", "Golden Trophy", ["成就", "勝利"]),
    ("四葉幸運草", "Four-leaf Clover", ["幸運", "機會"]),
    ("幸運星", "Lucky Star", ["好運", "願望"]),
    ("彩虹", "Rainbow", ["希望", "轉機"]),
    ("流星", "Shooting Star", ["願望", "機會"]),
    ("金色蝴蝶", "Golden Butterfly", ["轉機", "財運"]),
    ("金色鳳凰", "Golden Phoenix", ["重生", "成功"]),
    ("金色鹿", "Golden Deer", ["福氣", "貴人"]),
    ("金色白鶴", "Golden Crane", ["長壽", "祥瑞"]),
    ("金色竹子", "Golden Bamboo", ["節節高升"]),
    ("豐收稻穗", "Harvest Rice", ["豐收", "成果"]),
    ("豐收果籃", "Harvest Basket", ["收穫", "富足"]),
    ("金色葡萄", "Golden Grapes", ["豐盛", "累積"]),
    ("寶石寶箱", "Jewel Treasure Chest", ["財富", "珍貴"]),
    ("金色聚財流水", "Golden Wealth Stream", ["財源流動", "源源不絕"]),
]

WORKERS = 4


def _one(i: int, name: str, en: str, tags: list):
    print(f"[{i}/{len(FORTUNE)}] START {name} ({en})", flush=True)
    item = generate_pool_item(
        category="Fortune",
        name=name,
        tags=tags + [en],
        role_hints=["auspicious", "support", "hero"],
        use_placeholder=False,
    )
    print(f"[{i}/{len(FORTUNE)}] OK {item['id']} method={item.get('method')}", flush=True)
    return {"i": i, "ok": True, "id": item["id"], "method": item.get("method"), "file": item.get("file")}


def main():
    log_path = ROOT / "materials" / "batch_fortune_log.jsonl"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    log_path.write_text("", encoding="utf-8")

    ok, fail = 0, 0
    print(f"START parallel fortune n={len(FORTUNE)} workers={WORKERS}", flush=True)
    with ThreadPoolExecutor(max_workers=WORKERS) as ex:
        futs = {
            ex.submit(_one, i, name, en, tags): (i, name)
            for i, (name, en, tags) in enumerate(FORTUNE, 1)
        }
        for fut in as_completed(futs):
            i, name = futs[fut]
            try:
                row = fut.result()
                ok += 1
            except Exception as e:
                fail += 1
                row = {"i": i, "ok": False, "name": name, "error": str(e)}
                print(f"[{i}/{len(FORTUNE)}] FAIL {name}: {e}", flush=True)
                traceback.print_exc()
            with log_path.open("a", encoding="utf-8") as f:
                f.write(json.dumps(row, ensure_ascii=False) + "\n")

    summary = {"ok": ok, "fail": fail, "total": len(FORTUNE), "workers": WORKERS}
    (ROOT / "materials" / "batch_fortune_summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"DONE {summary}", flush=True)


if __name__ == "__main__":
    main()
