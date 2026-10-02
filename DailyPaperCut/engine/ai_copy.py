"""Social / daily copy generation via OpenAI. Never expose 'AI' to consumers."""
from __future__ import annotations

import json
import os
from typing import Any, Dict, Optional

from . import load_settings


def _client():
    try:
        from dotenv import load_dotenv
        from . import ROOT
        load_dotenv(ROOT / ".env")
    except ImportError:
        pass
    settings = load_settings()
    key_env = settings["openai"]["apiKeyEnv"]
    api_key = os.environ.get(key_env, "").strip()
    if not api_key:
        return None, None
    try:
        from openai import OpenAI
    except ImportError:
        return None, None
    base = os.environ.get("OPENAI_BASE_URL") or settings["openai"].get("baseUrl")
    client = OpenAI(api_key=api_key, base_url=base)
    return client, settings


def _fallback_summary(daily: Dict[str, Any]) -> str:
    level = daily.get("fortuneLevel", "平")
    theme = daily.get("mainTheme", "今日開運")
    secondary = daily.get("secondaryTheme") or ""
    yi = [str(x).strip() for x in (daily.get("yi") or []) if str(x).strip()]
    ji = [str(x).strip() for x in (daily.get("ji") or []) if str(x).strip()]
    yi0 = yi[0] if yi else ""
    ji0 = ji[0] if ji else ""
    templates = [
        f"今日{level}，以「{theme}」為軸，宜{yi0 or '守中'}、忌{ji0 or '躁進'}。",
        f"「{theme}」當令：先把{yi0 or '節奏'}做穩，避開{ji0 or '衝動'}。",
        f"運勢{level}，往{theme}靠近；{yi0 or '順勢'}可為，{ji0 or '硬碰'}先緩。",
    ]
    if secondary and secondary != theme:
        templates.append(f"今日{level}｜{theme}・{secondary}，宜{yi0 or '穩步'}忌{ji0 or '妄動'}。")
    seed = sum(ord(c) for c in f"{daily.get('date','')}{theme}{level}")
    text = templates[seed % len(templates)]
    return text[:28]


def _fallback_copy(daily: Dict[str, Any], settings: Dict[str, Any]) -> Dict[str, str]:
    theme = daily.get("mainTheme", "今日開運")
    level = daily.get("fortuneLevel", "平")
    yi = "、".join(daily.get("yi") or [])
    ji = "、".join(daily.get("ji") or [])
    tags = " ".join(settings.get("hashtags") or [])
    cta = settings["cta"]
    base = (
        f"【名序｜每日吉祥】{daily.get('date')}\n"
        f"今日運勢：{level}\n主題：{theme}\n宜：{yi}\n忌：{ji}\n"
        f"{daily.get('shortMessage', '')}\n"
        f"{cta['line']}\n{cta['product']}\n{cta['url']}"
    )
    return {
        "youtube": base + f"\n\n{tags}",
        "instagram": f"名序｜今日{level}\n{theme}\n宜 {yi}\n忌 {ji}\n\n{cta['line']}\n{cta['url']}\n{tags}",
        "facebook": base + f"\n\n{tags}",
        "threads": f"今日{level}｜{theme}\n宜：{yi}\n忌：{ji}\n{cta['line']}\n{cta['url']}\n{tags}",
        "shortMessage": daily.get("shortMessage") or f"今日{level}，守住節奏也能遇見好運。",
        "closingMessage": daily.get("closingMessage") or "名序｜知名・知運・知人生",
        "summaryText": daily.get("summaryText") or _fallback_summary(daily),
    }


def generate_social_copy(daily: Dict[str, Any], fortune: Dict[str, Any]) -> Dict[str, str]:
    settings = load_settings()
    client, _ = _client()
    if client is None:
        return _fallback_copy(daily, settings)

    model = settings["openai"]["copyModel"]
    tags = ", ".join(settings.get("hashtags") or [])
    cta = settings["cta"]
    prompt = {
        "role": "user",
        "content": (
            "你是名序品牌社群編輯。請依命理資料撰寫社群文案。"
            "禁止出現 AI、ChatGPT、自動生成等字眼。必須使用繁體中文。"
            "回傳 JSON，鍵：youtube, instagram, facebook, threads, shortMessage, closingMessage, summaryText。\n"
            f"日期：{daily.get('date')}\n"
            f"干支：{fortune.get('dayGanZhi')}\n"
            f"吉凶：{fortune.get('fortuneLevel')}（{fortune.get('fortuneScore')}）\n"
            f"主題：{daily.get('mainTheme')} / {daily.get('secondaryTheme')}\n"
            f"宜：{fortune.get('yi')}\n忌：{fortune.get('ji')}\n"
            f"幸運色：{fortune.get('luckyColor')} 方位：{fortune.get('luckyDirection')} 時段：{fortune.get('luckyTime')}\n"
            f"CTA：{cta['line']} / {cta['product']} / {cta['url']}\n"
            f"Hashtags：{tags}\n"
            "youtube 可較長；instagram 中等偏短；threads 最短；facebook 溫暖敘事。\n"
            "summaryText：給畫面字卡用的「今日總結」一句話。"
            "必須同時呼應當日主題與宜忌（可點出宜做什麼、忌避什麼的感覺），"
            "繁體中文、溫暖有畫面、不要口號堆疊、不要 emoji、不要引號包裹整句；"
            "長度 16～26 個中文字（含標點），只寫一句。"
        ),
    }
    try:
        resp = client.chat.completions.create(
            model=model,
            messages=[
                {
                    "role": "system",
                    "content": "只輸出 JSON 物件，不要 Markdown。品牌名為名序。全文繁體中文。",
                },
                prompt,
            ],
            temperature=0.85,
        )
        text = resp.choices[0].message.content or ""
        text = text.strip()
        if text.startswith("```"):
            text = text.strip("`")
            if text.startswith("json"):
                text = text[4:].strip()
        data = json.loads(text)
        out = _fallback_copy(daily, settings)
        out.update({k: str(data[k]) for k in out.keys() if k in data and data[k]})
        # 字卡長度保險
        summary = " ".join(str(out.get("summaryText") or "").replace("\n", " ").split())
        if len(summary) > 28:
            summary = summary[:27] + "…"
        out["summaryText"] = summary or _fallback_summary(daily)
        return out
    except Exception:
        return _fallback_copy(daily, settings)


def choose_theme(fortune: Dict[str, Any], blocked_themes: set) -> Dict[str, str]:
    cands = list(fortune.get("themeCandidates") or ["今日安穩"])
    main = next((t for t in cands if t not in blocked_themes), cands[0])
    secondary = next((t for t in cands if t != main), cands[0])
    return {"mainTheme": main, "secondaryTheme": secondary}
