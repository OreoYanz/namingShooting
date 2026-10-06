#!/usr/bin/env python3
"""Generate SEO guide pages under docs/guides and site/guides; refresh sitemap guide URLs."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = "https://mingxu.mingxu.workers.dev"
CSS = "1.0.50"
JS = "1.0.50"

GUIDES = [
    {
        "slug": "index",
        "title": "命名知識｜取名・改名・流年常見問題彙整｜名序",
        "description": "名序命名知識庫：怎麼選命名服務、八字與五格差異、字輩與喜用字、改名理由與時機、每日宜忌怎麼看。",
        "h1": "命名知識",
        "lead": "用清楚的說明，幫你在取名、改名與流年之間做出適合自己的選擇。",
        "keywords": "命名知識,寶寶取名,專業改名,流年分析,姓名學,名序",
        "body": """
      <p>以下文章整理台灣家庭常見的命名疑問。內容屬文化與服務說明，不構成運勢保證；用字與戶政手續請依現行法規確認。</p>
      <ul class="guide-list">
        <li><a href="how-to-choose.html"><strong>怎麼選命名服務／命名老師</strong></a> — 檢查清單與交付物</li>
        <li><a href="naming-methods.html"><strong>姓名學、五格、八字差在哪</strong></a> — 方法比較與名序怎麼整合</li>
        <li><a href="zibei.html"><strong>字輩取名怎麼搭配</strong></a> — 字輩與喜用字衝突怎麼解</li>
        <li><a href="preferred-chars.html"><strong>喜用字命名</strong></a> — 兼顧音義與命理</li>
        <li><a href="phonology.html"><strong>名字音韻好聽嗎</strong></a> — 聲調與日常使用感</li>
        <li><a href="rename-reason.html"><strong>改名理由怎麼寫</strong></a> — 常見類型與注意事項</li>
        <li><a href="rename-timing.html"><strong>大人改名時機怎麼看</strong></a> — 評估面向而非保證</li>
        <li><a href="auspicious-days.html"><strong>今日宜忌與吉祥日</strong></a> — 冠笄、開市、嫁娶怎麼看</li>
      </ul>
      <h2>相關服務</h2>
      <p><a href="../newborn.html">新生兒命名</a> · <a href="../rename.html">專業改名</a> · <a href="../liunian.html">流年分析</a> · <a href="../works.html">真實案例</a></p>
""",
    },
    {
        "slug": "how-to-choose",
        "title": "怎麼選命名服務｜命名老師檢查清單｜名序",
        "description": "找命名服務前先看：交付物清不清楚、是否客製、有無真實案例、會不會誇大保證。名序整理實用檢查清單。",
        "h1": "怎麼選命名服務",
        "lead": "好的命名服務，應該讓你看得懂方法、收得到完整說明，而不是只給幾個字。",
        "keywords": "怎麼選命名老師,命名服務推薦,寶寶取名推薦,台灣命名,名序",
        "cta": ("../newborn.html", "了解新生兒命名"),
        "body": """
      <p>台灣命名選擇很多。比起追品牌關鍵字，更重要的是確認「你會拿到什麼」與「對方如何負責說明」。</p>
      <h2>建議檢查清單</h2>
      <ul>
        <li><strong>交付物</strong>：是否有完整報告（命理摘要、字義、音韻、故事、流年）？</li>
        <li><strong>客製程度</strong>：能否處理字輩、喜用字、避開字與單雙名偏好？</li>
        <li><strong>方法說明</strong>：是否清楚說明八字、五行、五格、音韻如何一起看？</li>
        <li><strong>真實案例</strong>：有沒有去識別化的公開案例可參考？</li>
        <li><strong>承諾界線</strong>：是否避免「保證改運／發財／健康」等誇大說法？</li>
        <li><strong>溝通管道</strong>：交件後能否討論偏好與調整方向？</li>
      </ul>
      <h2>名序怎麼做</h2>
      <p>名序提供新生兒命名、專業改名與流年分析，採多維評估（命理、五行、結構、音韻、字義、氣質、使用感），並交付 PDF 報告與名字故事。詳見 <a href="../works.html">真實案例</a> 與 <a href="../faq.html">常見問題</a>。</p>
      <h2>常見 FAQ</h2>
      <details class="faq-item"><summary>一定要請大師才準嗎？</summary>
        <p>命名更像「理解＋設計＋說明」。重點是方法是否透明、報告是否可讀、是否尊重你的期待，而不是神秘感。</p></details>
      <details class="faq-item"><summary>價格高低代表什麼？</summary>
        <p>通常反映交付深度與服務範圍。先比內容與流程，再比價格，较不容易後悔。</p></details>
""",
    },
    {
        "slug": "naming-methods",
        "title": "姓名學五格與八字差在哪｜命名方法比較｜名序",
        "description": "五格筆畫、八字五行、音韻字義差在哪？名序用對照表說明常見命名方法，以及如何綜合評估，避免只看單一規則。",
        "h1": "姓名學、五格、八字差在哪",
        "lead": "單一規則很容易互相衝突；比較完整的做法，是把不同面向一起看清楚。",
        "keywords": "姓名學,五格剖象,八字取名,五行命名,命名方法比較,名序",
        "cta": ("../newborn.html", "了解名序命名"),
        "body": """
      <h2>常見方法對照</h2>
      <ul>
        <li><strong>八字／五行</strong>：依出生時刻看喜用與平衡，偏「命理契合」。</li>
        <li><strong>三才五格</strong>：從姓名結構與筆畫配置看配置關係，偏「姓名結構」。</li>
        <li><strong>音韻</strong>：聲調、韻母、朗讀是否自然，偏「日常使用」。</li>
        <li><strong>字義／意象</strong>：字的意思、文化聯想與氣質，偏「可說出的故事」。</li>
      </ul>
      <h2>為什麼不能只看一項</h2>
      <p>只看筆畫可能忽略讀音；只看五行可能忽略日常好不好叫。名序以綜合評估處理，並提供多組候選，而不是單一路線。</p>
      <h2>名序怎麼整合</h2>
      <p>從命理分析出發，再做多維尋名、候選篩選與專業甄選，最後寫成可保存的名字故事與報告。可先看 <a href="../newborn.html">新生兒命名</a> 或 <a href="../rename.html">專業改名</a>。</p>
""",
    },
    {
        "slug": "zibei",
        "title": "字輩取名怎麼搭配｜喜用字衝突怎麼解｜名序",
        "description": "有字輩怎麼取名？字輩固定字與喜用字衝突時怎麼辦？名序說明字輩搭配原則，以及新生兒／改名服務如何處理。",
        "h1": "字輩取名怎麼搭配",
        "lead": "字輩是家族秩序的一部分；好的命名會在字輩、喜用與音義之間找到平衡。",
        "keywords": "字輩取名,字輩命名,輩分取名,喜用字,新生兒命名,名序",
        "cta": ("../newborn.html", "諮詢字輩命名"),
        "body": """
      <h2>字輩通常怎麼用</h2>
      <p>多數家族會指定中間字或特定位置用字。命名時需先確認：單名／雙名、字輩位置、可否調整順序、有沒有同輩已用字要避開。</p>
      <h2>字輩與喜用字衝突時</h2>
      <ul>
        <li>先保留字輩（家族規範優先），在另一字上補強喜用、音韻與字義。</li>
        <li>若字輩字本身音韻尷尬，可改用同音近義或家族可接受的變通（需家人共識）。</li>
        <li>提供多組候選，讓父母在「守輩分」與「好叫好寫」之間比較。</li>
      </ul>
      <h2>名序怎麼協助</h2>
      <p>委託時可填字輩與喜用／避開字。名序會在候選中標明字輩安排，並說明取捨。延伸閱讀：<a href="preferred-chars.html">喜用字命名</a>。</p>
""",
    },
    {
        "slug": "preferred-chars",
        "title": "喜用字命名怎麼做｜兼顧音義與命理｜名序",
        "description": "什麼是喜用字？命名時如何把喜用字、音韻與字義一起考慮？名序說明實務做法與注意事項。",
        "h1": "喜用字命名",
        "lead": "喜用字是方向，不是唯一答案；還要聽起來自然、寫起來清楚、說得出寓意。",
        "keywords": "喜用字,喜用神取名,五行喜用,寶寶取名,名序",
        "cta": ("../newborn.html", "開始命名諮詢"),
        "body": """
      <h2>喜用字是什麼</h2>
      <p>在命理脈絡裡，「喜用」常指較能補足或平衡命盤的五行方向。轉成用字時，會優先考慮對應五行、字義與意象的字。</p>
      <h2>實務上怎麼用比較穩</h2>
      <ul>
        <li>把喜用當篩選條件之一，而不是唯一分數。</li>
        <li>同時檢查音韻、避開生僻難寫、注意職場與學校使用感。</li>
        <li>若有字輩，先滿足字輩再找喜用搭配。</li>
      </ul>
      <h2>名序怎麼做</h2>
      <p>可於委託時提供喜用方向或指定字。報告會說明為何推薦該組字，以及它與命理、音義的關係。也可看 <a href="naming-methods.html">方法比較</a>。</p>
""",
    },
    {
        "slug": "phonology",
        "title": "寶寶名字音韻好聽嗎｜聲調與日常使用感｜名序",
        "description": "名字好不好聽，和聲調、韻母、朗讀節奏有關。名序說明音韻評估重點，以及如何避免只重筆畫忽略日常使用。",
        "h1": "名字音韻好聽嗎",
        "lead": "名字要叫很多年。聽起來順、叫得出口、好記好寫，跟命理一樣重要。",
        "keywords": "名字音韻,取名好聽,聲調取名,寶寶名字朗讀,名序",
        "cta": ("../newborn.html", "了解命名服務"),
        "body": """
      <h2>音韻常看什麼</h2>
      <ul>
        <li>全名朗讀是否順口、有沒有卡頓或繞口。</li>
        <li>聲調搭配是否太平或過衝。</li>
        <li>是否容易與不好的諧音聯想。</li>
        <li>職場、學校點名時的清晰度與辨識度。</li>
      </ul>
      <h2>常見迷思</h2>
      <p>「筆畫吉」不等于「好聽好用」。名序把音韻流暢與日常使用感納入綜合評估，並提供多組不同氣質的候選。</p>
""",
    },
    {
        "slug": "rename-reason",
        "title": "改名理由怎麼寫｜常見類型與注意事項｜名序",
        "description": "成人改名理由怎麼整理？事業、感情、人生階段轉換等常見類型，以及寫理由時要注意的界線。名序提供實用說明。",
        "h1": "改名理由怎麼寫",
        "lead": "理由寫得清楚，比較能對準你真正想改善或紀念的人生面向。",
        "keywords": "改名理由,成人改名,改名原因,專業改名,名序",
        "cta": ("../rename.html", "了解專業改名"),
        "body": """
      <h2>常見理由類型</h2>
      <ul>
        <li>人生階段轉換（就業、婚育、遷居、重新出發）</li>
        <li>原名音韻／諧音／書寫不便</li>
        <li>希望名字更貼近自我期許或家族期待</li>
        <li>整理原名與現階段目標的落差</li>
      </ul>
      <h2>撰寫注意</h2>
      <p>建議具體描述「想改善的日常感受」或「想保留的特質」，避免把改名寫成絕對改運保證。戶政手續與法規請向戶政事務所確認。</p>
      <h2>名序怎麼協助</h2>
      <p>專業改名含原姓名分析、新名探索與前後比較，並可整理改名理由與命名理念於報告中。詳見 <a href="../rename.html">專業改名</a>。</p>
""",
    },
    {
        "slug": "rename-timing",
        "title": "大人改名時機怎麼看｜評估面向說明｜名序",
        "description": "成人什麼時候適合考慮改名？從人生階段、使用困擾與期待方向評估，而不是保證改運。名序說明可討論的面向。",
        "h1": "大人改名時機怎麼看",
        "lead": "時機不是算一個「絕對吉日」而已；更重要的是你為什麼想改、改完如何使用。",
        "keywords": "大人改名,成人改名時機,改名運勢,專業改名台灣,名序",
        "cta": ("../rename.html", "了解專業改名"),
        "body": """
      <h2>可以先問自己的三個問題</h2>
      <ul>
        <li>原名在生活中是否造成明顯困擾（音、字、辨識、自我認同）？</li>
        <li>是否處在明確的人生轉換，需要一個可被理解的新起點？</li>
        <li>是否準備好處理戶政與周遭重新認識新名的成本？</li>
      </ul>
      <h2>名序的評估方式</h2>
      <p>我們會分析原名，再依你的原因與希望方向提出候選，並做前後比較。內容屬文化命理參考，<strong>不保證</strong>事業、財運、感情或健康結果。</p>
      <p>延伸：<a href="rename-reason.html">改名理由怎麼寫</a> · <a href="../works.html">改名相關案例</a></p>
""",
    },
    {
        "slug": "auspicious-days",
        "title": "今日宜忌與吉祥日｜冠笄開市嫁娶怎麼看｜名序",
        "description": "今日宜忌怎麼讀？冠笄、開市、嫁娶等用語是什麼意思？名序說明每日吉祥的看懂方式，並導向每日彙整與服務頁。",
        "h1": "今日宜忌與吉祥日",
        "lead": "宜忌是傳統曆法的文化參考，可作為安排的提醒，不是絕對指令。",
        "keywords": "今日宜忌,吉祥日,冠笄,開市,嫁娶日子,每日吉祥,名序",
        "cta": ("../daily.html", "查看每日吉祥"),
        "body": """
      <h2>怎麼讀每日宜忌</h2>
      <ul>
        <li><strong>宜</strong>：傳統上較常被建議安排的事項方向。</li>
        <li><strong>忌</strong>：較常被提醒避開或慎重的事項。</li>
        <li>同一天可能同時適合某些事、不適合另一些事，需對照你的實際安排。</li>
      </ul>
      <h2>常見主題詞</h2>
      <ul>
        <li><strong>冠笄</strong>：與成長禮、階段標記相關的傳統用語。</li>
        <li><strong>開市</strong>：開業、展開營運相關安排的參考。</li>
        <li><strong>嫁娶</strong>：婚嫁儀式日程的傳統參考。</li>
        <li><strong>祭祀／出行／修造</strong>：各有對應的生活安排語境。</li>
      </ul>
      <h2>到哪裡看每日內容</h2>
      <p>請至 <a href="../daily.html">每日吉祥彙整</a> 依日期閱讀，或回 <a href="../index.html#daily">首頁日曆</a>。若你關心的是個人年度節奏，可了解 <a href="../liunian.html">流年分析</a>。</p>
""",
    },
]


def page_html(g: dict) -> str:
    slug = g["slug"]
    path = "/guides/" if slug == "index" else f"/guides/{slug}.html"
    # guides/index.html URL as guides/index.html for canonical clarity
    if slug == "index":
        path = "/guides/index.html"
    cta = g.get("cta")
    cta_html = ""
    if cta:
        href, label = cta
        cta_html = f"""
    <section class="section"><div class="container reveal"><div class="cta-band">
      <h2>下一步</h2>
      <p>以命為本，以字成名。</p>
      <a class="btn btn-gold" href="{href}">{label}</a>
      <a class="btn btn-outline" href="https://line.me/R/ti/p/@187xckjb" target="_blank" rel="noopener noreferrer" data-mx-track="line_add">Line 諮詢</a>
      <p style="margin-top:0.85rem"><a href="index.html">返回命名知識</a></p>
    </div></div></section>"""
    elif slug == "index":
        cta_html = f"""
    <section class="section"><div class="container reveal"><div class="cta-band">
      <h2>需要協助命名？</h2>
      <p>以命為本，以字成名。</p>
      <a class="btn btn-gold" href="../index.html#services">查看服務</a>
      <a class="btn btn-outline" href="https://line.me/R/ti/p/@187xckjb" target="_blank" rel="noopener noreferrer" data-mx-track="line_add">Line 諮詢</a>
    </div></div></section>"""

    crumb_name = g["h1"]
    return f"""<!DOCTYPE html>
<html lang="zh-Hant">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>{g["title"]}</title>
  <meta name="description" content="{g["description"]}" />
  <meta name="keywords" content="{g["keywords"]}" />
  <meta name="author" content="名序" />
  <meta name="robots" content="index,follow,max-image-preview:large" />
  <link rel="canonical" href="{BASE}{path}" />
  <meta name="theme-color" content="#f7f4ef" />
  <meta property="og:locale" content="zh_TW" />
  <meta property="og:type" content="article" />
  <meta property="og:site_name" content="名序" />
  <meta property="og:title" content="{g["title"]}" />
  <meta property="og:description" content="{g["description"]}" />
  <meta property="og:url" content="{BASE}{path}" />
  <meta property="og:image" content="{BASE}/assets/logo.png" />
  <meta name="twitter:card" content="summary" />
  <meta name="twitter:title" content="{g["title"]}" />
  <meta name="twitter:description" content="{g["description"]}" />
  <meta name="twitter:image" content="{BASE}/assets/logo.png" />
  <link rel="icon" href="../assets/logo.png" type="image/png" />
  <link rel="stylesheet" href="../css/site.css?v={CSS}" />
  <script src="../js/tracking-config.js?v={JS}"></script>
  <script src="../js/tracking.js?v={JS}"></script>
  <style>
    .guide-list {{ line-height: 1.75; padding-left: 1.2em; }}
    .guide-list li {{ margin: 0.45rem 0; }}
  </style>
  <script type="application/ld+json">
{{
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  "itemListElement": [
    {{"@type": "ListItem", "position": 1, "name": "名序", "item": "{BASE}/"}},
    {{"@type": "ListItem", "position": 2, "name": "命名知識", "item": "{BASE}/guides/index.html"}},
    {{"@type": "ListItem", "position": 3, "name": "{crumb_name}", "item": "{BASE}{path}"}}
  ]
}}
  </script>
</head>
<body data-page="guides" data-asset-base="../">
  <a class="skip-link" href="#main">跳至主要內容</a>
  <header id="site-header"></header>
  <main id="main">
    <section class="page-hero"><div class="container">
      <h1>{g["h1"]}</h1>
      <p class="lead">{g["lead"]}</p>
    </div></section>
    <section class="section"><div class="container content-block reveal">
{g["body"]}
    </div></section>
{cta_html}
  </main>
  <footer id="site-footer"></footer>
  <script src="../js/layout.js?v={JS}"></script>
  <script src="../js/site.js?v={JS}"></script>
</body>
</html>
"""


def write_guides(root: Path) -> list[str]:
    out = root / "guides"
    out.mkdir(parents=True, exist_ok=True)
    urls = []
    for g in GUIDES:
        name = "index.html" if g["slug"] == "index" else f"{g['slug']}.html"
        (out / name).write_text(page_html(g), encoding="utf-8")
        urls.append(f"{BASE}/guides/{name}")
    return urls


def patch_sitemap(path: Path, guide_urls: list[str]) -> None:
    text = path.read_text(encoding="utf-8")
    # Remove existing guides entries then insert before daily/ or before closing
    import re

    text = re.sub(
        r"\s*<url>\s*<loc>https://mingxu\.mingxu\.workers\.dev/guides/[^<]+</loc>\s*</url>\s*",
        "\n",
        text,
    )
    block = "\n".join(
        f"  <url>\n    <loc>{u}</loc>\n  </url>\n" for u in guide_urls
    )
    marker = "  <url>\n    <loc>https://mingxu.mingxu.workers.dev/daily.html</loc>"
    if marker in text:
        text = text.replace(marker, block + "\n" + marker, 1)
    else:
        text = text.replace("</urlset>", block + "\n</urlset>", 1)
    path.write_text(text, encoding="utf-8")


def main() -> None:
    for folder in ("docs", "site"):
        root = ROOT / folder
        urls = write_guides(root)
        sm = root / "sitemap.xml"
        if sm.exists():
            patch_sitemap(sm, urls)
        print(f"{folder}: wrote {len(urls)} guide pages")


if __name__ == "__main__":
    main()
