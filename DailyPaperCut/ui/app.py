"""每日剪紙工作台 — Streamlit"""
from __future__ import annotations

import json
import sys
from pathlib import Path

import streamlit as st

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

from engine import day_dir, load_json
from engine.pipeline import generate_daily


st.set_page_config(page_title="名序｜每日剪紙工作台", layout="wide")
st.title("名序｜每日吉祥剪紙工作台")
st.caption("第一階段：產生 → 預覽 → 人工確認 → 發布（不自動發布）")

col_a, col_b = st.columns([1, 2])
with col_a:
    date_in = st.text_input("日期 YYYYMMDD", value="20261002")
    if st.button("Generate Daily", type="primary"):
        with st.spinner("產生中…"):
            try:
                result = generate_daily(date_in)
                st.session_state["last_result"] = result
                st.success(f"已產生 {result['date']}")
            except Exception as e:
                st.error(str(e))

    st.divider()
    st.write("局部重跑（讀取當日已有資料後再產）")
    c1, c2 = st.columns(2)
    with c1:
        st.button("重新產生素材", disabled=True, help="下一階段開放")
        st.button("重新產生場景", disabled=True)
        st.button("重新產生 GIF", disabled=True)
    with c2:
        st.button("重新產生 Shorts", disabled=True)
        st.button("重新產生文案", disabled=True)
        if st.button("確認"):
            base = day_dir(date_in.replace("/", "").replace("-", ""))
            pub = base / "publish" / "publish.json"
            if pub.exists():
                data = load_json(pub)
                data["status"] = "confirmed"
                from datetime import datetime
                data["confirmedAt"] = datetime.now().isoformat(timespec="seconds")
                pub.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
                st.success("已標記確認（仍未自動發布）")
        if st.button("發布"):
            st.warning("第一階段不自動發布。確認後請人工上傳各平台。")

with col_b:
    result = st.session_state.get("last_result")
    key = date_in.replace("/", "").replace("-", "")
    base = day_dir(key)
    daily_path = base / "data" / "daily.json"
    if result is None and daily_path.exists():
        daily = load_json(daily_path)
        fortune = load_json(base / "data" / "fortune.json") if (base / "data" / "fortune.json").exists() else {}
    elif result:
        daily = result["daily"]
        fortune = result["fortune"]
    else:
        daily, fortune = {}, {}

    if daily:
        st.subheader("今日吉凶")
        st.write(f"**{daily.get('fortuneLevel')}**（{daily.get('fortuneScore')}）｜{daily.get('dayGanZhi')}")
        st.write(f"主題：{daily.get('mainTheme')}／{daily.get('secondaryTheme')}")
        st.write("宜：", "、".join(daily.get("yi") or []))
        st.write("忌：", "、".join(daily.get("ji") or []))

        scene = base / "scene" / f"scene_{key}.jpg"
        if scene.exists():
            st.subheader("場景")
            st.image(str(scene), use_container_width=True)

        preview = base / "short" / f"daily_{key}_preview.jpg"
        if preview.exists():
            st.subheader("Shorts Preview")
            st.image(str(preview), use_container_width=True)

        gif = base / "gif" / f"daily_{key}.gif"
        if gif.exists():
            st.subheader("GIF Preview")
            st.image(str(gif))

        st.subheader("文案 Preview")
        for name in ("youtube", "instagram", "facebook", "threads"):
            p = base / "social" / f"{name}.txt"
            if p.exists():
                with st.expander(name):
                    st.text(p.read_text(encoding="utf-8"))
    else:
        st.info("選擇日期後按 Generate Daily")
