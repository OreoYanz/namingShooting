# -*- coding: utf-8 -*-
"""Generate C# supporting data from Python SQLite / data files."""
from __future__ import annotations

import csv
import json
import os
import re
import sqlite3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "data"
OUT = ROOT / "winforms" / "_gen"
OUT.mkdir(parents=True, exist_ok=True)

SQLITE = Path(os.environ.get("APPDATA", "")) / "TaiwanNamingHelper" / "naming.sqlite3"


def gen_wuge81() -> None:
    w81 = json.loads((DATA / "wuge_81.json").read_text(encoding="utf-8"))
    luck = {it["number"]: it["luck"] for it in w81["items"]}
    text = {it["number"]: it["text"] for it in w81["items"]}
    lines = [
        "using System.Collections.Generic;",
        "",
        "namespace Mingxu.Core.Constants",
        "{",
        "public static class Wuge81Table",
        "{",
        "    public static readonly Dictionary<int, string> Luck = new Dictionary<int, string>()",
        "    {",
    ]
    for k, v in sorted(luck.items()):
        lines.append(f'        [{k}] = "{v}",')
    lines += [
        "    };",
        "",
        "    public static readonly Dictionary<int, string> Text = new Dictionary<int, string>()",
        "    {",
    ]
    for k, v in sorted(text.items()):
        vv = v.replace("\\", "\\\\").replace('"', '\\"')
        lines.append(f'        [{k}] = "{vv}",')
    lines += ["    };", "}", "}", ""]
    (OUT / "Wuge81Table.cs").write_text("\n".join(lines), encoding="utf-8")


def _csv_safe(s: str) -> str:
    return (s or "").replace("\r", " ").replace("\n", " ").strip()


def gen_character_seed_from_sqlite(conn: sqlite3.Connection) -> int:
    cols = (
        "char", "stroke", "wuxing", "candidate", "tone", "pinyin", "zhuyin",
        "meaning", "radical", "frequency", "rarity", "gender_tag",
        "modern", "classical", "literary", "elegant", "cute", "neutral", "strong", "soft",
    )
    sql = """
        SELECT char, stroke, element, candidate, tone, pinyin, pronunciation,
               meaning, radical, frequency, rarity, gender_tag,
               style_modern, style_classical, style_literary, style_elegant,
               style_cute, style_neutral, style_strong, style_soft
        FROM characters
        WHERE stroke > 0
        ORDER BY candidate DESC, char
    """
    rows = []
    for r in conn.execute(sql):
        tone = r[4]
        try:
            tone_i = int(tone or 0)
        except (TypeError, ValueError):
            tone_i = 0
        tone_s = "" if tone_i == 0 else str(tone_i)
        rows.append([
            r[0], int(r[1] or 0), r[2] or "", int(r[3] or 0), tone_s,
            _csv_safe(r[5] or ""), _csv_safe(r[6] or ""),
            _csv_safe((r[7] or "")[:160]),
            _csv_safe(r[8] or ""),
            int(r[9] or 50), int(r[10] or 50), (r[11] or "U"),
            int(r[12] or 50), int(r[13] or 50), int(r[14] or 50), int(r[15] or 50),
            int(r[16] or 50), int(r[17] or 50), int(r[18] or 50), int(r[19] or 50),
        ])
    seed = OUT / "characters.seed.csv"
    with seed.open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f)
        w.writerow(cols)
        w.writerows(rows)
    print("characters", len(rows), "->", seed)
    return len(rows)


def gen_hot_names(conn: sqlite3.Connection) -> None:
    hot_rows = []
    try:
        for year, gender, given, rank in conn.execute(
            "SELECT year, gender, given_name, rank FROM hot_names ORDER BY year DESC, gender, rank"
        ):
            hot_rows.append({
                "year": int(year),
                "gender": gender or "",
                "given": given,
                "rank": int(rank or 0),
            })
    except sqlite3.Error as ex:
        print("hot_names skip:", ex)
    (OUT / "hot_names.json").write_text(
        json.dumps(hot_rows, ensure_ascii=False, indent=0), encoding="utf-8"
    )
    print("hot_names", len(hot_rows))

    classic = DATA / "classic_hot_names.json"
    if classic.exists():
        (OUT / "classic_hot.json").write_text(classic.read_text(encoding="utf-8"), encoding="utf-8")
    else:
        items = []
        try:
            for row in conn.execute("SELECT given_name, gender FROM classic_hot"):
                items.append({"given_name": row[0], "gender": row[1] or ""})
        except sqlite3.Error:
            pass
        (OUT / "classic_hot.json").write_text(
            json.dumps(items, ensure_ascii=False), encoding="utf-8"
        )


def gen_compound_surnames() -> None:
    path = ROOT / "naming_method" / "core" / "constants.py"
    text = path.read_text(encoding="utf-8")
    start = text.find("COMPOUND_SURNAMES")
    chunk = text[start : start + 4000] if start >= 0 else ""
    names = []
    for m in re.finditer(r'["\']([^"\']{2,3})["\']', chunk):
        n = m.group(1)
        if all("\u4e00" <= c <= "\u9fff" for c in n):
            names.append(n)
    names = sorted(set(names))
    (OUT / "compound_surnames.json").write_text(
        json.dumps(names, ensure_ascii=False), encoding="utf-8"
    )
    print("compound_surnames", len(names))


def main() -> None:
    if not SQLITE.exists():
        raise SystemExit(f"缺少字庫 SQLite：{SQLITE}\n請先執行 Python 版一次以 seed 資料庫。")
    gen_wuge81()
    conn = sqlite3.connect(str(SQLITE))
    try:
        gen_character_seed_from_sqlite(conn)
        gen_hot_names(conn)
    finally:
        conn.close()
    gen_compound_surnames()
    print("generated into", OUT)


if __name__ == "__main__":
    main()
