#!/usr/bin/env python3
"""Apply SEO meta + JSON-LD to site HTML pages (site/ and docs/)."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = "https://oreoyanz.github.io/namingShooting"
OG_IMAGE = f"{BASE}/assets/logo.png"
CSS_VER = "1.0.15"

PAGES = {
    "index.html": {
        "title": "名序｜台灣新生兒命名・成人改名・流年分析",
        "description": "名序提供台灣新生兒命名、成人改名與流年分析。結合命理、五行、音韻與字義，為每個名字寫下值得珍藏的故事。以命為本，以字成名。",
        "keywords": "名序,取名,新生兒命名,寶寶取名,成人改名,姓名學,八字取名,流年分析,台灣取名,命名服務",
        "path": "/",
        "og_type": "website",
    },
    "about.html": {
        "title": "關於名序｜品牌故事與價值觀",
        "description": "認識名序：一名一序，一生一願。以命為本、以字成名，替每一個人生寫下值得珍藏的第一行文字。",
        "keywords": "名序,關於名序,命名品牌,取名服務,品牌故事",
        "path": "/about.html",
        "og_type": "website",
    },
    "newborn.html": {
        "title": "新生兒命名｜寶寶取名・八字姓名學｜名序",
        "description": "名序新生兒命名：依出生時刻、命理、五行、音韻與字義，為孩子尋找可長久使用、值得珍藏的名字。",
        "keywords": "新生兒命名,寶寶取名,嬰兒取名,八字取名,姓名學,名序",
        "path": "/newborn.html",
        "og_type": "article",
    },
    "rename.html": {
        "title": "成人改名｜姓名分析與改名建議｜名序",
        "description": "名序成人改名：分析原姓名，依命理、音韻、字義與個人期待探索新名，協助選擇值得珍藏的名字。",
        "keywords": "成人改名,改名,姓名分析,改名建議,姓名學,名序",
        "path": "/rename.html",
        "og_type": "article",
    },
    "liunian.html": {
        "title": "流年分析｜八字流年・年度節奏參考｜名序",
        "description": "名序流年分析：以八字、姓名學與生肖框架，提供年度節奏與提醒，不是預言，而是多一個理解自己的角度。",
        "keywords": "流年分析,八字流年,流年運勢,姓名學,名序",
        "path": "/liunian.html",
        "og_type": "article",
    },
    "works.html": {
        "title": "真實案例｜名序",
        "description": "名序真實案例字卡：去識別化公開生日、地區、姓氏與意象／宏觀階段。",
        "keywords": "真實案例,命名案例,改名案例,流年案例,名序",
        "path": "/works.html",
        "og_type": "website",
    },
    "faq.html": {
        "title": "常見問題｜命名流程・資料需求・服務說明｜名序",
        "description": "名序常見問題：命名與購買流程、所需資料、候選數量，以及命理服務定位說明。",
        "keywords": "名序FAQ,取名問題,命名流程,名序",
        "path": "/faq.html",
        "og_type": "website",
    },
    "contact.html": {
        "title": "聯絡名序｜Line 諮詢・電子信箱",
        "description": "聯絡名序：透過 Line 官方帳號 @187xckjb 或電子信箱 nameshootingmingxu@gmail.com 一對一諮詢命名與改名服務。",
        "keywords": "聯絡名序,命名諮詢,Line取名,名序客服",
        "path": "/contact.html",
        "og_type": "website",
    },
}

ORG_LD = {
    "@context": "https://schema.org",
    "@type": "ProfessionalService",
    "name": "名序",
    "alternateName": ["Mingxu", "namingShooting"],
    "url": f"{BASE}/",
    "logo": OG_IMAGE,
    "image": OG_IMAGE,
    "description": "台灣新生兒命名、成人改名與流年分析服務。以命為本，以字成名。",
    "email": "nameshootingmingxu@gmail.com",
    "areaServed": {"@type": "Country", "name": "Taiwan"},
    "availableLanguage": ["zh-Hant", "zh-TW"],
    "sameAs": ["https://line.me/R/ti/p/@187xckjb"],
    "contactPoint": {
        "@type": "ContactPoint",
        "contactType": "customer service",
        "email": "nameshootingmingxu@gmail.com",
        "availableLanguage": ["zh-TW"],
        "hoursAvailable": {
            "@type": "OpeningHoursSpecification",
            "dayOfWeek": [
                "Monday",
                "Tuesday",
                "Wednesday",
                "Thursday",
                "Friday",
            ],
            "opens": "09:00",
            "closes": "17:30",
        },
    },
    "hasOfferCatalog": {
        "@type": "OfferCatalog",
        "name": "名序服務",
        "itemListElement": [
            {
                "@type": "Offer",
                "itemOffered": {
                    "@type": "Service",
                    "name": "新生兒命名",
                    "url": f"{BASE}/newborn.html",
                },
            },
            {
                "@type": "Offer",
                "itemOffered": {
                    "@type": "Service",
                    "name": "成人改名",
                    "url": f"{BASE}/rename.html",
                },
            },
            {
                "@type": "Offer",
                "itemOffered": {
                    "@type": "Service",
                    "name": "流年分析",
                    "url": f"{BASE}/liunian.html",
                },
            },
        ],
    },
}

WEBSITE_LD = {
    "@context": "https://schema.org",
    "@type": "WebSite",
    "name": "名序",
    "url": f"{BASE}/",
    "inLanguage": "zh-Hant",
    "publisher": {"@type": "Organization", "name": "名序", "url": f"{BASE}/"},
}

FAQ_ITEMS = [
    ("名序的命名流程是什麼？", "命理分析 → 多維尋名 → 候選篩選 → 專業甄選 → 名字成章。由名序團隊依此五步完成，全程專人把關。"),
    ("購買流程是什麼？", "選擇服務 → 提供資料（官方 Line）→ 確認匯款 → 命理與姓名分析 → 提供候選姓名與命名解析、故事 → 完成交付。"),
    ("名序提供哪些服務？", "新生兒命名、成人改名、流年分析，以及命名故事等完整內容產出。詳情請透過 Line 官方了解更多。"),
    ("需要提供哪些資料？", "性別、出生日期時間、出生地、姓氏；改名需原姓名；可選填父母姓名、字輩、喜歡或避開的字。"),
    ("會提供多少組候選名字？", "提供 5 組命名解析完整內容。"),
    ("如果不喜歡候選名字怎麼辦？", "可透過 Line 官方討論報價。"),
    ("流年分析可以看幾年？", "可透過 Line 官方討論報價。"),
    ("服務費用如何計算？", "請透過 Line 官方了解更多。"),
    ("名序是算命嗎？會保證改運嗎？", "名序提供文化命理參考與命名設計，不宣稱保證改運、發財或健康。我們強調理解、選擇、祝福與紀念。"),
]

SERVICE_PAGES = {
    "newborn.html": ("新生兒命名", "依出生時刻與命理為孩子尋找承載祝福與意義的名字。"),
    "rename.html": ("成人改名", "理解原姓名，探索新可能，在尊重與期許之間找到屬於自己的名字。"),
    "liunian.html": ("流年分析", "以八字與姓名學框架提供年度節奏參考，協助理解當下。"),
}


def dumps_ld(obj: object) -> str:
    return json.dumps(obj, ensure_ascii=False, indent=2)


def page_json_ld(filename: str, meta: dict) -> list[dict]:
    url = BASE + meta["path"]
    blocks: list[dict] = []
    if filename == "index.html":
        blocks.append(ORG_LD)
        blocks.append(WEBSITE_LD)
    elif filename == "faq.html":
        blocks.append(
            {
                "@context": "https://schema.org",
                "@type": "FAQPage",
                "mainEntity": [
                    {
                        "@type": "Question",
                        "name": q,
                        "acceptedAnswer": {"@type": "Answer", "text": a},
                    }
                    for q, a in FAQ_ITEMS
                ],
            }
        )
    elif filename in SERVICE_PAGES:
        name, desc = SERVICE_PAGES[filename]
        blocks.append(
            {
                "@context": "https://schema.org",
                "@type": "Service",
                "name": name,
                "description": desc,
                "url": url,
                "provider": {"@type": "Organization", "name": "名序", "url": f"{BASE}/"},
                "areaServed": "TW",
            }
        )
    elif filename == "contact.html":
        blocks.append(
            {
                "@context": "https://schema.org",
                "@type": "ContactPage",
                "name": "聯絡名序",
                "url": url,
                "mainEntity": ORG_LD,
            }
        )

    if filename != "index.html":
        name_map = {
            "about.html": "關於名序",
            "newborn.html": "新生兒命名",
            "rename.html": "成人改名",
            "liunian.html": "流年分析",
            "works.html": "真實案例",
            "faq.html": "常見問題",
            "contact.html": "聯絡方式",
        }
        blocks.append(
            {
                "@context": "https://schema.org",
                "@type": "BreadcrumbList",
                "itemListElement": [
                    {
                        "@type": "ListItem",
                        "position": 1,
                        "name": "名序",
                        "item": f"{BASE}/",
                    },
                    {
                        "@type": "ListItem",
                        "position": 2,
                        "name": name_map.get(filename, meta["title"]),
                        "item": url,
                    },
                ],
            }
        )
    return blocks


def build_head(filename: str, meta: dict) -> str:
    url = BASE + meta["path"]
    ld_scripts = "\n".join(
        f'  <script type="application/ld+json">\n{dumps_ld(block)}\n  </script>'
        for block in page_json_ld(filename, meta)
    )
    return f"""<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>{meta['title']}</title>
  <meta name="description" content="{meta['description']}" />
  <meta name="keywords" content="{meta['keywords']}" />
  <meta name="author" content="名序" />
  <meta name="robots" content="index,follow,max-image-preview:large" />
  <meta name="googlebot" content="index,follow" />
  <link rel="canonical" href="{url}" />
  <meta name="theme-color" content="#f7f4ef" />
  <meta http-equiv="X-UA-Compatible" content="IE=edge" />
  <meta property="og:locale" content="zh_TW" />
  <meta property="og:type" content="{meta['og_type']}" />
  <meta property="og:site_name" content="名序" />
  <meta property="og:title" content="{meta['title']}" />
  <meta property="og:description" content="{meta['description']}" />
  <meta property="og:url" content="{url}" />
  <meta property="og:image" content="{OG_IMAGE}" />
  <meta property="og:image:alt" content="名序 Logo" />
  <meta name="twitter:card" content="summary" />
  <meta name="twitter:title" content="{meta['title']}" />
  <meta name="twitter:description" content="{meta['description']}" />
  <meta name="twitter:image" content="{OG_IMAGE}" />
  <link rel="icon" href="assets/logo.png" type="image/png" />
  <link rel="apple-touch-icon" href="assets/logo.png" />
  <link rel="stylesheet" href="css/site.css?v={CSS_VER}" />
  <script src="js/tracking-config.js?v={CSS_VER}"></script>
  <script src="js/tracking.js?v={CSS_VER}"></script>
{ld_scripts}
</head>"""


def patch_html(path: Path, filename: str, meta: dict) -> None:
    text = path.read_text(encoding="utf-8")
    new_head = build_head(filename, meta)
    updated, n = re.subn(r"<head>.*?</head>", new_head, text, count=1, flags=re.S)
    if n != 1:
        raise SystemExit(f"Could not replace <head> in {path}")
    # bump layout/js cache bust if present
    updated = re.sub(
        r"(css/site\.css|js/(?:layout|site|wordcloud|tracking-config|tracking)\.js)\?v=[\d.]+",
        rf"\1?v={CSS_VER}",
        updated,
    )
    path.write_text(updated, encoding="utf-8", newline="\n")
    print(f"updated {path.relative_to(ROOT)}")


def write_sitemap(folder: Path) -> None:
    entries = [
        "/",
        "/about.html",
        "/newborn.html",
        "/rename.html",
        "/liunian.html",
        "/works.html",
        "/faq.html",
        "/contact.html",
    ]
    lines = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">',
        "",
    ]
    for path in entries:
        lines.append("  <url>")
        lines.append(f"    <loc>{BASE}{path}</loc>")
        lines.append("  </url>")
        lines.append("")
    lines.append("</urlset>")
    lines.append("")
    (folder / "sitemap.xml").write_text("\n".join(lines), encoding="utf-8", newline="\n")
    print(f"updated {folder.relative_to(ROOT)}/sitemap.xml")


def write_robots(folder: Path) -> None:
    text = f"""User-agent: *
Allow: /

Sitemap: {BASE}/sitemap.xml
"""
    (folder / "robots.txt").write_text(text, encoding="utf-8", newline="\n")
    print(f"updated {folder.relative_to(ROOT)}/robots.txt")


def main() -> None:
    for folder_name in ("site", "docs"):
        folder = ROOT / folder_name
        for filename, meta in PAGES.items():
            patch_html(folder / filename, filename, meta)
        write_sitemap(folder)
        write_robots(folder)


if __name__ == "__main__":
    main()
