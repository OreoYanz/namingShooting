"""Render SEO-friendly static HTML for daily paper-cut pages."""
from __future__ import annotations

import html
import json
from functools import lru_cache
from pathlib import Path
from typing import Any, Dict, List, Sequence
from xml.sax.saxutils import escape as xml_escape

SITE_BASE = "https://oreoyanz.github.io/namingShooting"
_GLOSSARY_PATH = Path(__file__).resolve().parents[2] / "config" / "yi_ji_glossary.json"


def _esc(s: Any) -> str:
    return html.escape("" if s is None else str(s), quote=True)


def _roc_from_key(date_key: str) -> str:
    key = "".join(ch for ch in str(date_key) if ch.isdigit())
    if len(key) != 8:
        return str(date_key)
    y, m, d = int(key[:4]), int(key[4:6]), int(key[6:8])
    return f"{y - 1911}年{m}月{d}日"


def _join_list(items: Sequence[Any]) -> str:
    return "、".join(str(x) for x in (items or []) if str(x).strip())


@lru_cache(maxsize=1)
def load_yi_ji_glossary() -> Dict[str, str]:
    try:
        data = json.loads(_GLOSSARY_PATH.read_text(encoding="utf-8"))
    except Exception:
        return {}
    return {str(k): str(v) for k, v in (data or {}).items() if str(k).strip() and str(v).strip()}


def _explain_term(term: str, glossary: Dict[str, str]) -> str:
    tip = glossary.get(term) or glossary.get(term.replace("樑", "梁"))
    if tip:
        return tip
    return f"「{term}」為傳統通書用語，可依字面理解其吉凶所指。"


def format_yi_ji_html(items: Sequence[Any], *, tone: str = "yi") -> str:
    """Render 宜/忌 terms as hover/focus tip chips with vernacular explanations."""
    glossary = load_yi_ji_glossary()
    parts: List[str] = []
    for raw in items or []:
        term = str(raw).strip()
        if not term:
            continue
        tip = _explain_term(term, glossary)
        parts.append(
            f'<span class="yi-term yi-term--{tone}" tabindex="0" '
            f'data-tip="{_esc(tip)}" title="{_esc(tip)}">{_esc(term)}</span>'
        )
    return "、".join(parts) if parts else "—"


def render_day_html(
    payload: Dict[str, Any],
    *,
    prev_key: str = "",
    next_key: str = "",
) -> str:
    """HTML for /daily/YYYYMMDD.html (paths relative to daily/)."""
    key = str(payload.get("dateKey") or "")
    roc = payload.get("rocDate") or _roc_from_key(key)
    lunar = payload.get("lunarDate") or ""
    line2 = payload.get("dateLine2") or ""
    level = payload.get("fortuneLevel") or ""
    theme = payload.get("mainTheme") or ""
    secondary = payload.get("secondaryTheme") or ""
    yi_items = [str(x).strip() for x in (payload.get("yi") or []) if str(x).strip()]
    ji_items = [str(x).strip() for x in (payload.get("ji") or []) if str(x).strip()]
    yi = _join_list(yi_items)
    ji = _join_list(ji_items)
    yi_html = format_yi_ji_html(yi_items, tone="yi")
    ji_html = format_yi_ji_html(ji_items, tone="ji")
    summary = payload.get("summaryText") or payload.get("shortMessage") or ""
    note = payload.get("fortuneNote") or ""
    brand = payload.get("brandName") or "名序"
    media = payload.get("media") or {}
    gif = media.get("gifDated") or media.get("gif") or f"assets/daily/daily_{key}.gif"
    preview = media.get("previewDated") or media.get("preview") or f"assets/daily/daily_{key}_preview.jpg"
    last = (
        media.get("lastDated")
        or media.get("shareImage")
        or media.get("last")
        or f"assets/daily/daily_{key}_last.jpg"
    )
    share_image = last or preview or gif
    # pages live under daily/, so assets are one level up
    gif_href = f"../{gif}"
    page_url = f"{SITE_BASE}/daily/{key}.html"
    title = f"名序｜每日吉祥 {roc}"
    h1 = f"名序｜每日吉祥｜{roc}"
    theme_line = f"今日{level}" + (f"｜{theme}" if theme else "") + (f"・{secondary}" if secondary else "")
    desc_bits = [f"{roc}每日吉祥剪紙", theme_line, f"宜：{yi}" if yi else "", summary]
    description = "。".join(b for b in desc_bits if b)[:160]

    ld_article = {
        "@context": "https://schema.org",
        "@type": "Article",
        "headline": title,
        "description": description,
        "datePublished": payload.get("date") or None,
        "dateModified": payload.get("updatedAt") or None,
        "inLanguage": "zh-Hant",
        "author": {"@type": "Organization", "name": brand},
        "publisher": {
            "@type": "Organization",
            "name": brand,
            "logo": {"@type": "ImageObject", "url": f"{SITE_BASE}/assets/logo.png"},
        },
        "image": [f"{SITE_BASE}/{share_image}", f"{SITE_BASE}/{preview}", f"{SITE_BASE}/{gif}"],
        "mainEntityOfPage": page_url,
    }
    ld_crumb = {
        "@context": "https://schema.org",
        "@type": "BreadcrumbList",
        "itemListElement": [
            {"@type": "ListItem", "position": 1, "name": "首頁", "item": f"{SITE_BASE}/"},
            {"@type": "ListItem", "position": 2, "name": "每日吉祥", "item": f"{SITE_BASE}/daily.html"},
            {"@type": "ListItem", "position": 3, "name": roc, "item": page_url},
        ],
    }
    nav_bits = []
    if prev_key:
        nav_bits.append(f'<a href="{_esc(prev_key)}.html">← {_esc(_roc_from_key(prev_key))}</a>')
    nav_bits.append('<a href="../daily.html">彙整</a>')
    if next_key:
        nav_bits.append(f'<a href="{_esc(next_key)}.html">{_esc(_roc_from_key(next_key))} →</a>')
    nav_html = "　".join(nav_bits)

    return f"""<!DOCTYPE html>
<html lang="zh-Hant">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>{_esc(title)}</title>
  <meta name="description" content="{_esc(description)}" />
  <meta name="robots" content="index,follow,max-image-preview:large" />
  <meta name="keywords" content="名序,每日吉祥,剪紙,{_esc(theme)},宜忌,開運" />
  <link rel="canonical" href="{_esc(page_url)}" />
  <meta name="theme-color" content="#f7f4ef" />
  <meta property="og:locale" content="zh_TW" />
  <meta property="og:type" content="article" />
  <meta property="og:site_name" content="名序" />
  <meta property="og:title" content="{_esc(title)}" />
  <meta property="og:description" content="{_esc(description)}" />
  <meta property="og:url" content="{_esc(page_url)}" />
  <meta property="og:image" content="{_esc(SITE_BASE + '/' + share_image)}" />
  <meta name="twitter:card" content="summary_large_image" />
  <meta name="twitter:title" content="{_esc(title)}" />
  <meta name="twitter:description" content="{_esc(description)}" />
  <meta name="twitter:image" content="{_esc(SITE_BASE + '/' + share_image)}" />
  <link rel="icon" href="../assets/logo.png" type="image/png" />
  <link rel="stylesheet" href="../css/site.css?v=1.0.42" />
  <style>
    .daily-wrap {{ max-width: 720px; margin: 0 auto; }}
    .daily-card {{ background:#fffefb; border:1px solid #e4ddd2; padding:1.1rem 1.2rem; margin:0 0 1rem; }}
    .daily-card h2 {{ margin:0 0 0.6rem; font-size:1.05rem; letter-spacing:0.08em; }}
    .daily-meta {{ color:#5c656d; line-height:1.7; margin:0; }}
    .daily-yi {{ color:#96322c; }}
    .daily-ji {{ color:#666; }}
    .yi-term {{
      position: relative;
      display: inline-block;
      cursor: help;
      border-bottom: 1px dotted currentColor;
      outline: none;
    }}
    .yi-term::after {{
      content: attr(data-tip);
      position: absolute;
      left: calc(100% + 0.55rem);
      top: 50%;
      transform: translateY(-50%);
      min-width: 10rem;
      max-width: 14rem;
      padding: 0.45rem 0.6rem;
      background: #2b3036;
      color: #f7f4ef;
      font-size: 0.82rem;
      font-weight: 400;
      line-height: 1.45;
      letter-spacing: 0.02em;
      white-space: normal;
      border-radius: 4px;
      box-shadow: 0 8px 24px rgba(26,31,36,0.18);
      opacity: 0;
      pointer-events: none;
      z-index: 20;
      transition: opacity 0.12s ease;
    }}
    .yi-term::before {{
      content: "";
      position: absolute;
      left: calc(100% + 0.2rem);
      top: 50%;
      transform: translateY(-50%);
      border: 6px solid transparent;
      border-right-color: #2b3036;
      opacity: 0;
      pointer-events: none;
      z-index: 21;
      transition: opacity 0.12s ease;
    }}
    .yi-term:hover::after,
    .yi-term:focus::after,
    .yi-term:hover::before,
    .yi-term:focus::before {{
      opacity: 1;
    }}
    @media (max-width: 640px) {{
      .yi-term::after {{
        left: 50%;
        top: auto;
        bottom: calc(100% + 0.45rem);
        transform: translateX(-50%);
        max-width: min(14rem, 70vw);
      }}
      .yi-term::before {{
        left: 50%;
        top: auto;
        bottom: calc(100% - 0.15rem);
        transform: translateX(-50%);
        border: 6px solid transparent;
        border-top-color: #2b3036;
        border-right-color: transparent;
      }}
    }}
    .daily-summary {{ font-size:1.05rem; line-height:1.6; margin:0; }}
    .daily-media img {{ width:100%; border:1px solid #e4ddd2; background:#f0ebe3; display:block; }}
    .daily-actions {{ display:flex; flex-wrap:wrap; gap:0.5rem; }}
    .daily-actions .btn {{ margin:0; }}
    .daily-nav {{ display:flex; flex-wrap:wrap; gap:0.75rem; justify-content:space-between; margin:1rem 0 0; }}
  </style>
  <script type="application/ld+json">{json.dumps(ld_article, ensure_ascii=False)}</script>
  <script type="application/ld+json">{json.dumps(ld_crumb, ensure_ascii=False)}</script>
</head>
<body data-page="daily-day" data-asset-base="../">
  <a class="skip-link" href="#main">跳至主要內容</a>
  <header id="site-header"></header>
  <main id="main">
    <section class="page-hero"><div class="container daily-wrap">
      <p class="lead"><a href="../daily.html">每日吉祥</a> ／ {_esc(roc)}</p>
      <h1>{_esc(h1)}</h1>
      <p class="lead">{_esc(roc)}{_esc('　' + lunar if lunar else '')}</p>
    </div></section>
    <section class="section"><div class="container daily-wrap">
      <article class="daily-card">
        <h2>日期</h2>
        <p class="daily-meta">{_esc(roc)}{_esc('　' + lunar if lunar else '')}<br/>{_esc(line2)}</p>
      </article>
      <article class="daily-card">
        <h2>運勢與主題</h2>
        <p class="daily-meta">{_esc(theme_line)}</p>
        <p class="daily-meta daily-yi">宜：{yi_html}</p>
        <p class="daily-meta daily-ji">忌：{ji_html}</p>
        <p class="daily-meta" style="margin-top:0.55rem;font-size:0.9rem">將滑鼠移到詞語上（手機可點一下）可看白話解釋。</p>
      </article>
      <article class="daily-card">
        <h2>今日總結</h2>
        <p class="daily-summary">{_esc(summary or '—')}</p>
      </article>
      <article class="daily-card daily-media">
        <h2>今日剪紙</h2>
        <img src="{_esc(gif_href)}" alt="{_esc(roc + ' 名序每日吉祥剪紙 GIF')}" width="1080" height="1080" />
        <div class="daily-actions" style="margin-top:0.9rem">
          <a class="btn btn-primary" href="{_esc(gif_href)}" download="名序_每日吉祥_{_esc(key)}.gif">下載 GIF</a>
          <a class="btn btn-outline" href="{_esc('../' + share_image)}" download="名序_每日吉祥_{_esc(key)}.jpg">下載圖檔</a>
          <a class="btn btn-outline" href="../index.html#daily">回首頁日曆</a>
        </div>
      </article>
      <nav class="daily-nav daily-meta" aria-label="鄰近日">{nav_html}</nav>
      <p class="daily-meta">{_esc(note)}</p>
      <p class="daily-meta"><a href="../newborn.html">新生兒命名</a>　<a href="../rename.html">專業改名</a>　<a href="../liunian.html">流年運勢</a></p>
    </div></section>
  </main>
  <footer id="site-footer"></footer>
  <script src="../js/layout.js?v=1.0.42"></script>
  <script src="../js/site.js?v=1.0.42"></script>
</body>
</html>
"""


def render_daily_hub_html(days: List[Dict[str, Any]], today_key: str) -> str:
    """Hub page daily.html with crawlable list of day links."""
    visible = [d for d in days if str(d.get("dateKey") or "") <= today_key] or days
    items = []
    for d in visible[:60]:
        key = str(d.get("dateKey") or "")
        roc = d.get("rocDate") or _roc_from_key(key)
        theme = d.get("mainTheme") or ""
        level = d.get("fortuneLevel") or ""
        summary = d.get("summaryText") or ""
        items.append(
            "<li><a href=\"daily/{key}.html\"><strong>{roc}</strong>"
            " — 今日{level}{theme_part}</a>"
            "{summary_part}</li>".format(
                key=_esc(key),
                roc=_esc(roc),
                level=_esc(level),
                theme_part=_esc(f"｜{theme}" if theme else ""),
                summary_part=_esc(f"：{summary}" if summary else ""),
            )
        )
    list_html = "\n".join(items) if items else "<li>尚無已發布內容。</li>"
    today_link = f"daily/{today_key}.html" if any(str(d.get("dateKey")) == today_key for d in days) else "daily.html"
    ld_list = {
        "@context": "https://schema.org",
        "@type": "CollectionPage",
        "name": "名序｜每日吉祥剪紙彙整",
        "description": "名序每日吉祥剪紙彙整：依日期查看宜忌、主題、今日總結，並下載 GIF 或圖檔。",
        "url": f"{SITE_BASE}/daily.html",
        "inLanguage": "zh-Hant",
        "isPartOf": {"@type": "WebSite", "name": "名序", "url": f"{SITE_BASE}/"},
        "mainEntity": {
            "@type": "ItemList",
            "itemListElement": [
                {
                    "@type": "ListItem",
                    "position": i + 1,
                    "url": f"{SITE_BASE}/daily/{d.get('dateKey')}.html",
                    "name": str(d.get("rocDate") or d.get("dateKey") or ""),
                }
                for i, d in enumerate(visible[:60])
            ],
        },
    }
    return f"""<!DOCTYPE html>
<html lang="zh-Hant">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>名序｜每日吉祥剪紙彙整</title>
  <meta name="description" content="名序每日吉祥剪紙彙整：依日期查看宜忌、主題、今日總結，並下載 GIF 或圖檔。" />
  <meta name="robots" content="index,follow,max-image-preview:large" />
  <meta name="keywords" content="名序,每日吉祥,剪紙,宜忌,開運,每日運勢" />
  <link rel="canonical" href="{SITE_BASE}/daily.html" />
  <meta property="og:locale" content="zh_TW" />
  <meta property="og:type" content="website" />
  <meta property="og:site_name" content="名序" />
  <meta property="og:title" content="名序｜每日吉祥剪紙彙整" />
  <meta property="og:description" content="名序每日吉祥剪紙彙整：依日期查看宜忌、主題，並下載 GIF 或圖檔。" />
  <meta property="og:url" content="{SITE_BASE}/daily.html" />
  <meta property="og:image" content="{SITE_BASE}/assets/logo.png" />
  <link rel="icon" href="assets/logo.png" type="image/png" />
  <link rel="stylesheet" href="css/site.css?v=1.0.42" />
  <script src="js/tracking-config.js?v=1.0.42"></script>
  <script src="js/tracking.js?v=1.0.42"></script>
  <style>
    .daily-wrap {{ max-width: 720px; margin: 0 auto; }}
    .daily-card {{ background:#fffefb; border:1px solid #e4ddd2; padding:1.1rem 1.2rem; margin:0 0 1rem; }}
    .daily-archive-index {{ line-height:1.7; padding-left:1.2em; }}
    .daily-archive-index li {{ margin:0.35rem 0; }}
  </style>
  <script type="application/ld+json">{json.dumps(ld_list, ensure_ascii=False)}</script>
</head>
<body data-page="daily">
  <a class="skip-link" href="#main">跳至主要內容</a>
  <header id="site-header"></header>
  <main id="main">
    <section class="page-hero"><div class="container daily-wrap">
      <h1>每日吉祥</h1>
      <p class="lead">名序每日剪紙宜忌與開運短語。可依日期閱讀，並下載 GIF 或圖檔。</p>
      <p class="lead"><a class="btn btn-primary" href="{_esc(today_link)}">查看今日</a>
      <a class="btn btn-outline" href="index.html#daily" style="margin-left:0.5rem">回首頁日曆</a></p>
    </div></section>
    <section class="section"><div class="container daily-wrap">
      <div class="daily-card">
        <h2>歷日彙整</h2>
        <ul class="daily-archive-index">
{list_html}
        </ul>
      </div>
    </div></section>
  </main>
  <footer id="site-footer"></footer>
  <script src="js/layout.js?v=1.0.42"></script>
  <script src="js/site.js?v=1.0.42"></script>
</body>
</html>
"""


def write_sitemap(root: Path, day_keys: List[str]) -> None:
    static = [
        f"{SITE_BASE}/",
        f"{SITE_BASE}/about.html",
        f"{SITE_BASE}/newborn.html",
        f"{SITE_BASE}/rename.html",
        f"{SITE_BASE}/liunian.html",
        f"{SITE_BASE}/works.html",
        f"{SITE_BASE}/faq.html",
        f"{SITE_BASE}/contact.html",
        f"{SITE_BASE}/daily.html",
        f"{SITE_BASE}/index.html#daily",
    ]
    # Avoid fragment-only SEO; keep index root already listed
    static = [u for u in static if "#daily" not in u]
    urls = static + [f"{SITE_BASE}/daily/{k}.html" for k in sorted(set(day_keys))]
    body = ['<?xml version="1.0" encoding="UTF-8"?>', '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">', ""]
    for loc in urls:
        body.append("  <url>")
        body.append(f"    <loc>{xml_escape(loc)}</loc>")
        body.append("  </url>")
        body.append("")
    body.append("</urlset>")
    body.append("")
    (root / "sitemap.xml").write_text("\n".join(body), encoding="utf-8")
