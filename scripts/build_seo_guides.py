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
        "title": "命名知識｜新生兒命名・專業改名・吉日宜忌｜名序",
        "description": "名序命名知識：新生兒命名怎麼選、八字姓名學五格差異、喜用字與字輩、寶寶名字音韻、為什麼想改名、改名時機、吉日與宜忌。",
        "h1": "命名知識",
        "lead": "",
        "keywords": "新生兒命名,寶寶取名,專業改名,改名時機,喜用字,字輩取名,八字取名,五格剖象,今日宜忌,名序",
        "body": """
      <p>內容屬文化與服務說明，不構成運勢保證；用字與戶政手續請依現行法規確認。</p>
      <h2>新生兒命名</h2>
      <ul class="guide-list">
        <li><a href="how-to-choose.html"><strong>新生兒命名怎麼選</strong></a> — 挑選命名服務前必看的重點</li>
        <li><a href="naming-methods.html"><strong>八字／姓名學／五格差異</strong></a> — 三種方法怎麼比、如何一起看</li>
        <li><a href="preferred-chars.html"><strong>喜用字怎麼取</strong></a> — 喜用神轉成用字的實務做法</li>
        <li><a href="phonology.html"><strong>寶寶名字音韻</strong></a> — 好聽、好叫、好記怎麼評估</li>
        <li><a href="zibei.html"><strong>字輩取名</strong></a> — 字輩位置、衝突與搭配</li>
      </ul>
      <h2>專業改名</h2>
      <ul class="guide-list">
        <li><a href="rename-reason.html"><strong>為什麼想改名字</strong></a> — 常見原因與如何說清楚需求</li>
        <li><a href="rename-timing.html"><strong>改名時機</strong></a> — 什麼時候適合評估、要注意什麼</li>
      </ul>
      <h2>流年／傳統習俗</h2>
      <ul class="guide-list">
        <li><a href="auspicious-days.html"><strong>吉日與宜忌</strong></a> — 今日宜忌、冠笄開市嫁娶怎麼看</li>
      </ul>
      <h2>相關服務</h2>
      <p><a href="../newborn.html">新生兒命名</a> · <a href="../rename.html">專業改名</a> · <a href="../liunian.html">流年分析</a> · <a href="../works.html">真實案例</a></p>
""",
    },
    {
        "slug": "how-to-choose",
        "title": "新生兒命名怎麼選？挑選命名服務前必看的 7 個重點｜名序",
        "description": "新生兒命名怎麼選？挑選命名服務／取名老師前必看的重點：出生資料分析、多組候選、完整姓名解析、字義寓意、報告交付，以及八字五行姓名學如何綜合考量。",
        "h1": "新生兒命名怎麼選？挑選命名服務前必看的 7 個重點",
        "lead": "",
        "keywords": "新生兒命名,寶寶取名,新生兒取名,命名服務,取名老師,命名老師,八字取名,五行取名,姓名學取名,寶寶名字,新生兒名字怎麼取,名序",
        "cta": ("../newborn.html", "查看新生兒命名服務"),
        "cta_heading": "想了解名序如何為孩子規劃名字？",
        "cta_lead": "",
        "body": """
      <h2>新生兒命名為什麼需要仔細挑選？</h2>
      <p>寶寶名字會伴隨很長一段人生：戶籍登記、學校點名、親友稱呼，到未來的自我介紹。新生兒命名／寶寶取名若只看「好不好聽」或單一規則，之後常會發現難念、難寫、說不出寓意，或與家族字輩、期待不合。</p>
      <p>因此挑選命名服務、取名老師時，重點不是神秘感，而是：方法清不清楚、交付完不完整、能不能依你的條件客製。</p>

      <h2>挑選命名服務前要注意什麼？</h2>
      <h3>是否有提供出生資料分析？</h3>
      <p>可靠的新生兒取名，通常會依出生日期時間（與出生地校正）做基礎分析，而不是只憑姓氏隨便組字。</p>
      <h3>是否提供多組候選姓名？</h3>
      <p>多組候選能讓父母比較氣質與方向，也比較不容易被單一偏好綁死。</p>
      <h3>是否有完整姓名解析？</h3>
      <p>除了名字本身，還應說明為何推薦、各字如何搭配，方便家人討論與日後解釋。</p>
      <h3>是否說明名字的字義與寓意？</h3>
      <p>字義與寓意是名字「可被說出的故事」。沒有寓意說明的候選，之後往往很難說服長輩或自己。</p>
      <h3>是否提供完整報告？</h3>
      <p>完整電子報告（如 PDF）能把命理摘要、用字、音韻與故事留下，而不只是聊天室裡的幾個字。</p>
      <h3>父母可以指定命名方向嗎？</h3>
      <p>例如字輩、喜用字、避開字、單雙名、氣質偏好。好的命名服務應能在合理範圍內承接這些條件。</p>

      <h2>命名服務通常會看哪些內容？</h2>
      <ul>
        <li><strong>出生資料</strong>：性別、出生日期時間、出生地</li>
        <li><strong>姓氏與結構</strong>：單名／雙名、字輩位置</li>
        <li><strong>命理參考</strong>：八字取名、五行取名相關方向</li>
        <li><strong>姓名學參考</strong>：姓名結構與配置</li>
        <li><strong>音韻與字義</strong>：好不好叫、好不好寫、寓意是否清楚</li>
        <li><strong>家庭條件</strong>：喜用字、避開字、風格偏好</li>
      </ul>

      <h2>只看姓名筆畫就可以嗎？</h2>
      <p>不建議。筆畫或數理只是其中一種參考；若忽略八字五行方向、字義與日常音韻，容易出現「數字好看、實際不好用」的名字。新生兒名字怎麼取，較穩的做法是多面向一起看。</p>

      <h2>八字、五行、姓名學應該怎麼綜合考量？</h2>
      <p>八字／五行偏命理契合；姓名學偏姓名結構；音義偏日常使用。三者可能互相牽制，因此需要取捨與說明，而不是只留單一規則的答案。延伸閱讀：<a href="naming-methods.html">八字／姓名學／五格差異</a>。</p>

      <h2>如何判斷命名服務是否適合自己？</h2>
      <ul>
        <li>能否清楚說明方法，而不是只給結論</li>
        <li>交付物是否完整、是否可保存</li>
        <li>能否處理字輩、喜用字等真實家庭條件</li>
        <li>是否避免「保證改運／發財／健康」等誇大承諾</li>
        <li>交件後是否仍可討論偏好與方向</li>
      </ul>
      <p>也可參考 <a href="../works.html">真實案例</a> 與 <a href="../faq.html">常見問題</a>。</p>

      <h2>名序的新生兒命名方式</h2>
      <p>名序以出生資料為本，綜合命理、五行、姓名結構、音韻、字義與使用感，提供多組專屬候選，並交付含解析、寓意故事與流年參考的 PDF 報告。父母可指定字輩、喜用字與風格方向。</p>
      <p>服務內容與方案見 <a href="../newborn.html">新生兒命名</a>。</p>

      <h2>常見問題</h2>
      <details class="faq-item"><summary>新生兒命名一定要請命名老師嗎？</summary>
        <p>不一定。若你已有完整方法與判斷標準，也可自行取名。若希望有系統分析與可保存報告，再考慮命名服務會比較有效率。</p></details>
      <details class="faq-item"><summary>寶寶取名最重要的是什麼？</summary>
        <p>長期好用：好念、好寫、寓意清楚，並能對應家庭條件（如字輩）與合理的命理／姓名學參考。</p></details>
      <details class="faq-item"><summary>名序會保證改運嗎？</summary>
        <p>不會。名序提供文化命理參考與命名設計，強調理解、選擇與珍藏，不宣稱保證運勢結果。</p></details>
""",
    },
    {
        "slug": "naming-methods",
        "title": "八字姓名學五格差異｜新生兒取名方法比較｜名序",
        "description": "八字、姓名學、五格剖象差在哪？新生兒取名時如何一起看喜用、筆畫結構與日常音義，避免只迷信單一規則。",
        "h1": "八字／姓名學／五格差異",
        "lead": "三種常見取名方法各看不同面向；完整做法是整合，而不是互搶唯一答案。",
        "keywords": "八字取名,姓名學,五格剖象,三才五格,五行命名,新生兒取名方法,名序",
        "cta": ("../newborn.html", "了解名序命名"),
        "body": """
      <h2>八字取名看什麼</h2>
      <p>依出生年、月、日、時排出八字，觀察五行強弱與喜用方向，再反映到用字選擇。偏「命理契合」。</p>
      <h3>適合解答的問題</h3>
      <p>這個名字與孩子出生條件的關係是什麼？喜用方向大致落在哪裡？</p>
      <h2>姓名學／五格看什麼</h2>
      <p>五格剖象多從姓名結構與筆畫配置看天、人、地等關係，偏「姓名結構」參考。</p>
      <h3>要注意什麼</h3>
      <p>筆畫版本與計算口徑不同，結論可能不同；不宜只靠一個數字決定名字。</p>
      <h2>音義與日常使用</h2>
      <p>字義、意象、聲調、朗讀與辨識度，決定名字「好不好用」。這部分常被只看命理或只看筆畫的流程忽略。</p>
      <h2>為什麼不能只看一項</h2>
      <ul>
        <li>只看八字：可能忽略難念、難寫、諧音</li>
        <li>只看五格：可能忽略喜用與生活語感</li>
        <li>只看好聽：可能與家族字輩或命理方向落差過大</li>
      </ul>
      <h2>名序怎麼整合</h2>
      <p>以命理分析出發，再做多維尋名與專業甄選，最後寫成可保存報告。延伸：<a href="preferred-chars.html">喜用字怎麼取</a>、<a href="phonology.html">寶寶名字音韻</a>。</p>
""",
    },
    {
        "slug": "preferred-chars",
        "title": "喜用字怎麼取｜八字喜用神取名實務｜名序",
        "description": "喜用字怎麼取？說明八字喜用神如何轉成用字，以及如何兼顧音韻、字義、字輩，避免把喜用當成唯一標準。",
        "h1": "喜用字怎麼取",
        "lead": "喜用是取名方向，不是唯一答案；還要能念、能寫、說得出寓意。",
        "keywords": "喜用字怎麼取,喜用神取名,八字喜用字,五行喜用,寶寶取名喜用,名序",
        "cta": ("../newborn.html", "開始命名諮詢"),
        "body": """
      <h2>喜用字是什麼</h2>
      <p>在八字脈絡裡，「喜用」常指較能補足或平衡命盤的五行方向。轉成用字時，會優先考慮對應五行、字義與意象的字。</p>
      <h2>喜用字怎麼取：建議順序</h2>
      <ol>
        <li>先確認出生資料與喜用方向（文化命理參考）</li>
        <li>在符合方向的字池中，篩掉難寫、生僻、不良諧音</li>
        <li>再檢查音韻流暢與名字整體氣質</li>
        <li>若有字輩，先滿足字輩再找喜用搭配</li>
      </ol>
      <h3>常見誤解</h3>
      <ul>
        <li>同一個五行堆很多字就一定更好</li>
        <li>忽略讀音與學校／職場使用感</li>
        <li>把喜用當唯一分數，放棄其他面向</li>
      </ul>
      <h2>名序怎麼做</h2>
      <p>委託時可提供喜用方向或指定字；報告會說明推薦理由。也可讀 <a href="naming-methods.html">八字／姓名學／五格差異</a>、<a href="zibei.html">字輩取名</a>。</p>
""",
    },
    {
        "slug": "phonology",
        "title": "寶寶名字音韻｜好聽好叫好記的取名重點｜名序",
        "description": "寶寶名字音韻怎麼看？從聲調、韻母、朗讀節奏、諧音與點名辨識度，說明好聽好叫好記的評估重點。",
        "h1": "寶寶名字音韻",
        "lead": "名字要叫很多年。聽起來順、叫得出口、好記好寫，跟命理一樣重要。",
        "keywords": "寶寶名字音韻,取名好聽,名字聲調,嬰兒取名朗讀,好念好記,名序",
        "cta": ("../newborn.html", "了解命名服務"),
        "body": """
      <h2>音韻常看哪些面向</h2>
      <h3>全名朗讀</h3>
      <p>姓＋名連起來是否順口，有沒有卡頓、繞口或氣勢過衝／過平。</p>
      <h3>聲調與韻母</h3>
      <p>聲調組合會影響節奏；韻母相近時要特別注意是否像口吃或含糊。</p>
      <h3>諧音與辨識度</h3>
      <p>避免不良諧音；也要考慮學校點名、職場自我介紹時清不清楚。</p>
      <h2>常見迷思</h2>
      <p>「筆畫吉」不等于「好聽好用」。只重數理、忽略音韻，日後最常被抱怨的往往是「不好叫」。</p>
      <h2>名序怎麼評估</h2>
      <p>音韻流暢與日常使用感會納入綜合評估，並提供多組不同氣質候選。延伸：<a href="preferred-chars.html">喜用字怎麼取</a>。</p>
""",
    },
    {
        "slug": "zibei",
        "title": "字輩取名｜有字輩怎麼幫寶寶取名｜名序",
        "description": "字輩取名怎麼做？說明字輩位置、單雙名、與喜用字衝突時怎麼搭配，以及新生兒命名如何保留家族輩分。",
        "h1": "字輩取名",
        "lead": "字輩是家族秩序的一部分；好的命名會在輩分、喜用與音義之間找到平衡。",
        "keywords": "字輩取名,字輩命名,有字輩怎麼取名,輩分取名,新生兒字輩,名序",
        "cta": ("../newborn.html", "諮詢字輩命名"),
        "body": """
      <h2>字輩取名前先確認什麼</h2>
      <ul>
        <li>字輩用在第幾字（常見為中間字）</li>
        <li>單名或雙名偏好</li>
        <li>可否調整順序或變通</li>
        <li>同輩已用字是否要避開</li>
      </ul>
      <h2>字輩與喜用字衝突怎麼辦</h2>
      <h3>家族規範優先</h3>
      <p>多數情況先保留字輩，再於另一字補強喜用、音韻與字義。</p>
      <h3>字輩字難念難寫時</h3>
      <p>可討論同音近義或家族可接受的變通，但需家人共識，不宜單方面決定。</p>
      <h2>名序怎麼協助</h2>
      <p>委託時可填字輩與喜用／避開字；候選會標明字輩安排與取捨。延伸：<a href="preferred-chars.html">喜用字怎麼取</a>、<a href="how-to-choose.html">新生兒命名怎麼選</a>。</p>
""",
    },
    {
        "slug": "rename-reason",
        "title": "為什麼想改名字｜成人改名常見原因｜名序",
        "description": "為什麼想改名字？整理成人改名常見原因：人生轉換、音韻困擾、自我認同與期待落差，並說明如何把需求說清楚。",
        "h1": "為什麼想改名字",
        "lead": "改名通常不是忽然興起，而是生活裡累積已久的感受，終於需要一個清楚出口。",
        "keywords": "為什麼想改名字,成人改名原因,改名理由,專業改名,改名需求,名序",
        "cta": ("../rename.html", "了解專業改名"),
        "body": """
      <h2>成人改名常見原因</h2>
      <h3>人生階段轉換</h3>
      <p>就業、婚育、遷居、重新出發時，希望名字更能代表下一個階段。</p>
      <h3>音韻、諧音或書寫不便</h3>
      <p>原名難念、常被叫錯、諧音尷尬，或書寫／系統輸入長期困擾。</p>
      <h3>自我認同與期待</h3>
      <p>覺得原名與性格、價值觀或家人期待落差太大，想重新選擇可被理解的名字。</p>
      <h2>把「為什麼」說清楚的好處</h2>
      <p>需求愈具體，候選方向愈準：你想保留什麼特質、想改善哪種日常感受，都會影響選字。</p>
      <h2>注意界線</h2>
      <p>建議描述真實困擾與期待，避免把改名寫成絕對改運保證。戶政手續請向戶政事務所確認。</p>
      <h2>名序怎麼協助</h2>
      <p>專業改名含原名分析、新名探索與前後比較，並可整理改名理由於報告。延伸：<a href="rename-timing.html">改名時機</a>。</p>
""",
    },
    {
        "slug": "rename-timing",
        "title": "改名時機｜大人什麼時候適合改名｜名序",
        "description": "改名時機怎麼看？從生活困擾、人生轉換與戶政成本評估大人什麼時候適合改名，而不是只找一個絕對吉日。",
        "h1": "改名時機",
        "lead": "時機不只是黃曆上的一天；更重要的是你為什麼想改、是否準備好使用新名。",
        "keywords": "改名時機,大人改名,成人什麼時候改名,改名吉日,專業改名台灣,名序",
        "cta": ("../rename.html", "了解專業改名"),
        "body": """
      <h2>什麼時候適合認真評估改名</h2>
      <ul>
        <li>原名已造成明顯生活困擾（音、字、辨識、自我認同）</li>
        <li>正處在清楚的人生轉換，需要可被理解的新起點</li>
        <li>已準備好處理戶政與周遭重新認識新名的成本</li>
      </ul>
      <h2>改名時機常被問的兩件事</h2>
      <h3>一定要選吉日嗎？</h3>
      <p>黃曆可作文化參考，但更關鍵的是名字是否好用、理由是否清楚、你是否真的會長期使用。</p>
      <h3>會不會影響運勢？</h3>
      <p>名字屬文化與認同的一部分，可作為整理人生節奏的參考；名序<strong>不保證</strong>事業、財運、感情或健康結果。</p>
      <h2>名序怎麼評估</h2>
      <p>分析原名後，依原因與希望方向提出候選並做前後比較。延伸：<a href="rename-reason.html">為什麼想改名字</a>、<a href="../works.html">真實案例</a>。</p>
""",
    },
    {
        "slug": "auspicious-days",
        "title": "吉日與宜忌｜今日宜忌、冠笄開市嫁娶怎麼看｜名序",
        "description": "吉日與宜忌怎麼看？說明今日宜忌讀法，以及冠笄、開市、嫁娶等用語含義；傳統曆法參考，不是絕對指令。",
        "h1": "吉日與宜忌",
        "lead": "宜忌是傳統曆法的文化參考，可作為安排提醒，不是絕對指令。",
        "keywords": "吉日與宜忌,今日宜忌,吉祥日,冠笄,開市,嫁娶日子,黃曆宜忌,名序",
        "cta": ("../daily.html", "查看每日吉祥"),
        "body": """
      <h2>今日宜忌怎麼讀</h2>
      <ul>
        <li><strong>宜</strong>：傳統上較常被建議安排的事項方向</li>
        <li><strong>忌</strong>：較常被提醒避開或慎重的事項</li>
        <li>同一天可能同時適合某些事、不適合另一些事，需對照你的實際安排</li>
      </ul>
      <h2>常見吉日主題怎麼理解</h2>
      <h3>冠笄</h3>
      <p>與成長禮、階段標記相關的傳統用語。</p>
      <h3>開市</h3>
      <p>開業、展開營運相關安排的參考。</p>
      <h3>嫁娶</h3>
      <p>婚嫁儀式日程的傳統參考。</p>
      <h3>祭祀／出行／修造</h3>
      <p>各有對應的生活安排語境，宜搭配實際行程解讀。</p>
      <h2>到哪裡看每日內容</h2>
      <p>請至 <a href="../daily.html">每日吉祥彙整</a> 或 <a href="../index.html#daily">首頁日曆</a>。若關心個人年度節奏，可了解 <a href="../liunian.html">流年分析</a>。</p>
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
        cta_heading = g.get("cta_heading") or "下一步"
        cta_lead = g.get("cta_lead")
        if cta_lead is None:
            cta_lead = "以命為本，以字成名。"
        lead_html = f"<p>{cta_lead}</p>" if cta_lead else ""
        cta_html = f"""
    <section class="section"><div class="container reveal"><div class="cta-band">
      <h2>{cta_heading}</h2>
      {lead_html}
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
    if slug == "index":
        crumb_ld = f"""  <script type="application/ld+json">
{{
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  "itemListElement": [
    {{"@type": "ListItem", "position": 1, "name": "名序", "item": "{BASE}/"}},
    {{"@type": "ListItem", "position": 2, "name": "命名知識", "item": "{BASE}{path}"}}
  ]
}}
  </script>"""
    else:
        crumb_ld = f"""  <script type="application/ld+json">
{{
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  "itemListElement": [
    {{"@type": "ListItem", "position": 1, "name": "名序", "item": "{BASE}/"}},
    {{"@type": "ListItem", "position": 2, "name": "命名知識", "item": "{BASE}/guides/index.html"}},
    {{"@type": "ListItem", "position": 3, "name": "{crumb_name}", "item": "{BASE}{path}"}}
  ]
}}
  </script>"""
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
{crumb_ld}
</head>
<body data-page="guides" data-asset-base="../">
  <a class="skip-link" href="#main">跳至主要內容</a>
  <header id="site-header"></header>
  <main id="main">
    <section class="page-hero"><div class="container">
      <h1>{g["h1"]}</h1>
      {f'<p class="lead">{g["lead"]}</p>' if g.get("lead") else ""}
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
