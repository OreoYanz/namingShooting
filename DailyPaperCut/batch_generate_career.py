#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Parallel batch: Career materials (deduped list, 4 workers)."""
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

# User pasted the same 10 twice — keep unique only.
CAREER = [
    ("山峰", "Mountain Peak", ["登峰", "成就", "事業高度"]),
    ("階梯", "Success Stairs", ["步步高升", "升遷"]),
    ("登山者", "Mountain Climber", ["突破", "努力", "成長"]),
    ("航船", "Sailing Ship", ["前進", "事業航程"]),
    ("燈塔", "Lighthouse", ["方向", "目標", "指引"]),
    ("道路", "Career Path", ["人生方向", "選擇"]),
    ("指路箭頭", "Direction Arrow", ["決策", "方向"]),
    ("旗幟", "Victory Flag", ["成功", "達成目標"]),
    ("皇冠", "Crown", ["領導", "榮耀", "升遷"]),
    ("勳章", "Achievement Medal", ["成就", "肯定"]),
]

WORKERS = 4


def _one(i: int, name: str, en: str, tags: list):
    print(f"[{i}/{len(CAREER)}] START {name} ({en})", flush=True)
    item = generate_pool_item(
        category="Career",
        name=name,
        tags=tags + [en],
        role_hints=["hero", "support", "auspicious"],
        use_placeholder=False,
    )
    print(f"[{i}/{len(CAREER)}] OK {item['id']} method={item.get('method')}", flush=True)
    return {"i": i, "ok": True, "id": item["id"], "method": item.get("method"), "file": item.get("file")}


def main():
    log_path = ROOT / "materials" / "batch_career_log.jsonl"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    # clear previous log for this run
    log_path.write_text("", encoding="utf-8")

    ok, fail = 0, 0
    print(f"START parallel career n={len(CAREER)} workers={WORKERS}", flush=True)
    with ThreadPoolExecutor(max_workers=WORKERS) as ex:
        futs = {
            ex.submit(_one, i, name, en, tags): (i, name)
            for i, (name, en, tags) in enumerate(CAREER, 1)
        }
        for fut in as_completed(futs):
            i, name = futs[fut]
            try:
                row = fut.result()
                ok += 1
            except Exception as e:
                fail += 1
                row = {"i": i, "ok": False, "name": name, "error": str(e)}
                print(f"[{i}/{len(CAREER)}] FAIL {name}: {e}", flush=True)
                traceback.print_exc()
            with log_path.open("a", encoding="utf-8") as f:
                f.write(json.dumps(row, ensure_ascii=False) + "\n")

    summary = {"ok": ok, "fail": fail, "total": len(CAREER), "workers": WORKERS}
    (ROOT / "materials" / "batch_career_summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"DONE {summary}", flush=True)


if __name__ == "__main__":
    main()
