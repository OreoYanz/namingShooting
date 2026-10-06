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
        <li><a href="naming-methods.html"><strong>姓名學五格與八字差在哪</strong></a> — 五格、五行、八字取名一次看懂</li>
        <li><a href="preferred-chars.html"><strong>喜用字怎麼取</strong></a> — 五行喜用字命名注意事項</li>
        <li><a href="phonology.html"><strong>寶寶名字怎麼取才好聽</strong></a> — 音韻、聲調與日常使用</li>
        <li><a href="zibei.html"><strong>字輩取名怎麼搭配</strong></a> — 家族字輩與喜用字衝突怎麼辦</li>
      </ul>
      <h2>專業改名</h2>
      <ul class="guide-list">
        <li><a href="rename-reason.html"><strong>為什麼想改名字</strong></a> — 成人改名常見原因與改名前該想清楚的事</li>
        <li><a href="rename-timing.html"><strong>成人改名什麼時候適合</strong></a> — 改名前可先評估的面向</li>
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
      <p>八字／五行偏命理契合；姓名學偏姓名結構；音義偏日常使用。三者可能互相牽制，因此需要取捨與說明，而不是只留單一規則的答案。延伸閱讀：<a href="naming-methods.html">姓名學五格與八字差在哪</a>。</p>

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
        "title": "姓名學五格與八字差在哪？五格、五行、八字取名一次看懂｜名序",
        "description": "姓名學五格與八字差在哪？一次看懂三才五格、五格姓名學、八字取名與五行取名的差異，以及實際命名時如何綜合判斷，避免只看單一數字。",
        "h1": "姓名學五格與八字差在哪？五格、五行、八字取名一次看懂",
        "lead": "",
        "keywords": "姓名學,姓名學五格,三才五格,五格,八字取名,五行取名,八字命名,姓名學取名,三才五格是什麼,五格姓名學,五行與姓名,名序",
        "cta": ("../newborn.html", "查看新生兒命名服務"),
        "cta_heading": "想了解名序如何整合不同命名方法？",
        "cta_lead": "",
        "body": """
      <h2>姓名學五格是什麼？</h2>
      <p>姓名學五格（常稱五格剖象／五格姓名學）是依姓名用字與筆畫結構，把名字拆成幾個「格」來觀察配置關係的方法。重點在<strong>姓名本身的結構</strong>，不是出生時刻的命盤。</p>
      <h3>天格、人格、地格、外格、總格</h3>
      <ul>
        <li><strong>天格</strong>：多與姓氏相關的結構參考</li>
        <li><strong>人格</strong>：常被視為姓名核心配置之一</li>
        <li><strong>地格</strong>：多與名字用字相關</li>
        <li><strong>外格</strong>：整體外圍配置參考</li>
        <li><strong>總格</strong>：姓名整體結構的綜合觀察</li>
      </ul>
      <p>各家對筆畫口徑（如康熙字典筆畫）可能不同，解讀前要先確認計算方式一致。</p>

      <h2>三才五格是在看什麼？</h2>
      <p>「三才五格是什麼」常被一起問：五格是五個結構位置；三才多指天、人、地三者之間的配置關係。</p>
      <h3>三才配置怎麼理解？</h3>
      <p>三才配置用來觀察天、人、地是否較為通暢或互相牽制。它是姓名學內部的結構語言，適合當參考，不宜單獨變成「吉／凶唯一答案」。</p>

      <h2>八字取名是什麼？</h2>
      <p>八字取名（八字命名）依出生年、月、日、時排出四柱八字，再依命盤條件思考用字方向。重點在<strong>出生資料與命理節奏</strong>，再對應到姓名。</p>
      <h3>日主與五行</h3>
      <p>日主代表命盤中的核心參考；五行（金木水火土）用來描述能量分布與平衡關係，也是五行取名常討論的基礎。</p>
      <h3>喜用與忌用方向</h3>
      <p>命理上常會歸納相對較需要補足或較宜謹慎的方向（喜用／忌用）。轉成取名時，是「用字方向」的參考，不是保證結果的開關。延伸：<a href="preferred-chars.html">喜用字怎麼取</a>。</p>

      <h2>五行與八字有什麼關係？</h2>
      <p>八字由干支組成，干支各自對應五行，因此八字分析幾乎離不開五行。五行取名常被用來描述「名字如何呼應命盤的五行關係」。</p>
      <h3>姓名五行與命盤之間的關係</h3>
      <p>姓名用字可從字義、字形、傳統五行歸類等角度討論，再與命盤方向對照。五行與姓名的關係應視為<strong>互相參照</strong>，而不是用名字完全改寫命盤。</p>

      <h2>姓名學五格與八字取名有什麼不同？</h2>
      <ul>
        <li><strong>輸入資料不同</strong>：五格看姓名結構；八字看出生時刻</li>
        <li><strong>問題不同</strong>：五格偏「這個名字結構如何」；八字偏「這個名字與出生條件如何對應」</li>
        <li><strong>結論型態不同</strong>：五格常落在數理／配置語言；八字常落在喜用方向與平衡</li>
        <li><strong>限制不同</strong>：五格受筆畫口徑影響；八字受出生時間準確度影響</li>
      </ul>

      <h2>只看五格或只看八字可以嗎？</h2>
      <p>可以當起點，但不建議當唯一標準。</p>
      <h3>為什麼命名不能只看單一數字？</h3>
      <p>單一數字或單一規則，無法同時回答：好不好叫、好不好寫、寓意清不清楚、能否配合字輩、是否與家庭期待一致。只看五格可能忽略命理方向與音義；只看八字可能忽略姓名結構與日常使用感。</p>

      <h2>實際命名時如何綜合判斷？</h2>
      <ol>
        <li>先確認出生資料與家庭條件（字輩、喜用／避開字、單雙名）</li>
        <li>用八字／五行理解大方向</li>
        <li>在候選字中檢查音韻、字義、書寫與辨識度</li>
        <li>再以姓名學五格／三才作為結構參考與交叉檢查</li>
        <li>保留多組候選，讓父母比較氣質與取捨</li>
      </ol>
      <p>也可搭配 <a href="phonology.html">寶寶名字音韻</a>、<a href="zibei.html">字輩取名</a>。</p>

      <h2>名序如何整合不同命名方法？</h2>
      <p>名序不以單一規則決定名字，而是把八字取名、五行方向、姓名學結構、音韻與字義一起評估，再產出可讀的解析與 PDF 報告。詳見 <a href="../newborn.html">新生兒命名</a> 或 <a href="../rename.html">專業改名</a>。</p>

      <h2>常見問題</h2>
      <details class="faq-item"><summary>三才五格一定要全吉才可用嗎？</summary>
        <p>不必把「全吉」當唯一門檻。更重要的是整體是否合理、好不好用，以及能否清楚說明取捨。</p></details>
      <details class="faq-item"><summary>八字取名和姓名學取名哪個比較準？</summary>
        <p>兩者回答的問題不同，沒有絕對高下。實務上較穩的是互相參照，而不是二選一。</p></details>
      <details class="faq-item"><summary>五行取名會不會和五格衝突？</summary>
        <p>有可能。此時應回到使用情境與家庭條件做取捨，並在報告中說明為何這樣選。</p></details>
""",
    },
    {
        "slug": "preferred-chars",
        "title": "喜用字怎麼取？五行喜用字命名有哪些注意事項？｜名序",
        "description": "喜用字怎麼取？說明喜用字與八字、五行、喜用神的關係，以及五行喜用字命名時如何兼顧字義、音韻與姓名結構，避免只缺就硬補。",
        "h1": "喜用字怎麼取？五行喜用字命名有哪些注意事項？",
        "lead": "",
        "keywords": "喜用字,喜用字取名,喜用字命名,五行取名,八字取名,八字喜用,喜用神,五行喜用字,新生兒取名,寶寶取名,命名用字,名序",
        "cta": ("../newborn.html", "查看新生兒命名服務"),
        "cta_heading": "想用喜用字為孩子規劃名字？",
        "cta_lead": "",
        "body": """
      <h2>什麼是喜用字？</h2>
      <p>喜用字，是指在八字取名／五行取名脈絡裡，較符合命盤「喜用」方向的用字。它不是某一本字典的固定清單，而是依個人出生條件推導出的<strong>命名用字方向</strong>。</p>
      <p>實務上說「喜用字命名」，通常是：先看八字喜用，再從對應五行、字義與意象的字中挑選。</p>

      <h2>喜用字與八字、五行有什麼關係？</h2>
      <h3>出生資料與命盤分析</h3>
      <p>八字取名以出生年、月、日、時為基礎。出生資料愈清楚，喜用方向的討論才愈有依據。</p>
      <h3>日主與五行方向</h3>
      <p>日主是命盤核心參考之一；五行（金木水火土）用來描述能量分布。八字喜用，多半從「這個命盤相對需要什麼、宜謹慎什麼」談起。</p>
      <h3>喜用與忌用方向</h3>
      <p>喜用神／喜用方向，是文化命理上的補益或調和參考；忌用則是相對不宜過度強化的方向。轉成喜用字取名時，重點是「方向感」，不是保證結果。</p>

      <h2>是不是缺什麼五行就一定要補什麼？</h2>
      <p>不一定。常見誤解是「缺水就一定要一堆水部字」。實際命名還要看：</p>
      <ul>
        <li>是否與字輩、姓氏、音韻衝突</li>
        <li>字義是否合適、會不會過於堆疊</li>
        <li>是否只追求五行標籤，忽略日常好不好叫</li>
      </ul>
      <p>五行喜用字是加分方向，不是唯一及格線。也可對照 <a href="naming-methods.html">姓名學五格與八字差在哪</a>。</p>

      <h2>喜用字怎麼挑？</h2>
      <ol>
        <li>先確認出生資料與喜用／忌用方向</li>
        <li>建立符合方向的候選字池</li>
        <li>去掉生僻、難寫、不良諧音的字</li>
        <li>再檢查字義、音韻、姓名整體感</li>
        <li>若有字輩，先滿足字輩再找喜用搭配（見 <a href="zibei.html">字輩取名怎麼搭配</a>）</li>
      </ol>

      <h2>取名時除了五行還要看什麼？</h2>
      <h3>字義與寓意</h3>
      <p>名字要能被說出故事。只有五行標籤、沒有字義，日後很難對家人與自己交代。</p>
      <h3>音韻與讀音</h3>
      <p>再好的五行方向，若難念、易叫錯，長期使用成本很高。</p>
      <h3>姓名結構與整體感</h3>
      <p>還要看全名氣質是否協調、結構是否合理，而不是單字分數加總。</p>

      <h2>喜用字與字義可以同時兼顧嗎？</h2>
      <p>可以，而且應該兼顧。較穩的做法是：在喜用方向的字池中，優先選字義清楚、意象正向、不生僻的字；若兩者衝突，寧可說明取捨，也不要硬塞難用的字。</p>

      <h2>喜用字與音韻如何搭配？</h2>
      <p>先確認姓氏（與字輩）的讀法，再為喜用字位置選聲調、韻母合適的字，並整名朗讀。延伸：<a href="phonology.html">寶寶名字音韻</a>。</p>

      <h2>名序如何進行喜用字命名？</h2>
      <p>名序以出生資料做八字／五行方向分析，再綜合喜用字、字義、音韻與姓名結構，提供多組候選與完整解析。新生兒取名、寶寶取名皆可指定喜用方向或避開字，詳見 <a href="../newborn.html">新生兒命名</a>。</p>

      <h2>常見問題</h2>
      <details class="faq-item"><summary>喜用神和喜用字是同一件事嗎？</summary>
        <p>相關但不相同。喜用神偏命盤方向；喜用字是把方向落成實際用字的結果。</p></details>
      <details class="faq-item"><summary>一定要出現喜用五行偏旁才算有補到嗎？</summary>
        <p>不一定。字義、意象與整體配置都可能參與討論，不必只認偏旁。</p></details>
      <details class="faq-item"><summary>可以自己指定喜用字嗎？</summary>
        <p>可以提供指定字或方向；名序會評估它與音韻、字義、字輩是否合適，並說明取捨。</p></details>
""",
    },
    {
        "slug": "phonology",
        "title": "寶寶名字怎麼取才好聽？音韻、聲調與日常使用一次看｜名序",
        "description": "寶寶名字怎麼取才好聽？一次看懂名字音韻、名字聲調、姓氏搭配、諧音注意事項，以及筆畫、五行與音韻如何一起考量。",
        "h1": "寶寶名字怎麼取才好聽？音韻、聲調與日常使用一次看",
        "lead": "",
        "keywords": "寶寶名字,寶寶取名,名字好聽,名字音韻,名字聲調,新生兒取名,名字怎麼取,小孩名字,新生兒名字,名字讀音,名字諧音,名序",
        "cta": ("../newborn.html", "查看新生兒命名服務"),
        "cta_heading": "想為孩子取一個好聽又好用的名字？",
        "cta_lead": "",
        "body": """
      <h2>寶寶名字好不好聽，為什麼音韻很重要？</h2>
      <p>寶寶名字、小孩名字會被反覆叫喊：家裡呼喚、學校點名、未來自我介紹。名字好聽不只是感覺問題，更是名字音韻與日常使用是否順暢。新生兒取名若只重筆畫或五行，忽略讀音，之後最常被抱怨的往往是「不好叫」。</p>

      <h2>名字音韻主要看哪些地方？</h2>
      <ul>
        <li>全名朗讀是否順口</li>
        <li>聲調與節奏是否自然</li>
        <li>有無不良諧音</li>
        <li>日常暱稱與正式場合是否都好用</li>
      </ul>

      <h2>聲調搭配會影響名字的感覺嗎？</h2>
      <p>會。名字聲調會改變節奏與氣質：有的輕快，有的厚重，有的聽起來平淡或過衝。</p>
      <h3>姓氏與名字的聲調</h3>
      <p>先把姓氏聲調固定，再看名字用字如何銜接，整名才容易自然。</p>
      <h3>連續相同聲調</h3>
      <p>連續同調不一定不行，但要特別檢查是否像卡住、過於平板或不易分辨。</p>
      <h3>名字的節奏感</h3>
      <p>雙名時兩個字的長短輕重，也會影響「好不好聽、好不好記」。</p>

      <h2>名字需要注意哪些諧音？</h2>
      <h3>常見諧音問題</h3>
      <ul>
        <li>與不雅、玩笑、負面詞語接近的讀音</li>
        <li>方言或口語連讀後變味</li>
        <li>容易被同學取綽號的組合</li>
      </ul>
      <p>名字諧音檢查，建議用國語朗讀，也可用家人常用的口音再念一次。</p>

      <h2>姓氏與名字怎麼搭配比較自然？</h2>
      <p>好的搭配通常是：姓＋名連起來不繞口、不含糊、氣勢不過衝也不過軟。單名要特別注意辨識度；雙名則看兩個字彼此是否咬字清楚。名字怎麼取，從「姓氏＋候選」整名試念開始，往往比先挑單字更準。</p>

      <h2>筆畫、五行與音韻可以一起考量嗎？</h2>
      <p>可以，而且應該一起看。筆畫與五行是參考方向；音韻決定長期使用感。當規則與讀音衝突時，要能說明取捨，而不是只留單一分數最高的字。延伸：<a href="naming-methods.html">姓名學五格與八字差在哪</a>、<a href="preferred-chars.html">喜用字怎麼取</a>。</p>

      <h2>名字好聽之外，還要考慮哪些事情？</h2>
      <h3>日常稱呼是否自然</h3>
      <p>家人怎麼叫、同學怎麼叫，會不會彆扭。</p>
      <h3>正式場合與日常使用感</h3>
      <p>點名、證件、自我介紹時是否清楚、好寫、好記。</p>
      <ul>
        <li>字義與寓意是否說得出口</li>
        <li>是否生僻難寫</li>
        <li>若有字輩，是否仍朗讀順暢（見 <a href="zibei.html">字輩取名怎麼搭配</a>）</li>
      </ul>

      <h2>名序如何評估名字的音韻？</h2>
      <p>名序在新生兒取名流程中，會把名字音韻、聲調節奏、諧音風險與日常／正式使用感納入綜合評估，並提供多組不同氣質的候選，讓父母比較「好聽」與「好用」。詳見 <a href="../newborn.html">新生兒命名</a>。</p>

      <h2>常見問題</h2>
      <details class="faq-item"><summary>名字好聽就夠了嗎？</summary>
        <p>不夠。還要字義清楚、書寫可行，並與家庭條件（如字輩、喜用方向）合理搭配。</p></details>
      <details class="faq-item"><summary>新生兒名字一定要三個字才好聽嗎？</summary>
        <p>不一定。單名、雙名都能好聽，關鍵是整名節奏與辨識度，不是字數本身。</p></details>
      <details class="faq-item"><summary>可以指定氣質方向嗎？</summary>
        <p>可以，例如清朗、溫潤、文雅等。名序會在音韻與用字上往該方向收斂，並說明取捨。</p></details>
""",
    },
    {
        "slug": "zibei",
        "title": "字輩取名怎麼搭配？家族字輩與喜用字衝突時怎麼辦？｜名序",
        "description": "字輩取名怎麼搭配？說明家族字輩怎麼用、字輩與五行／喜用字衝突時怎麼辦，以及音韻、字義、名字氣質如何一起考慮。",
        "h1": "字輩取名怎麼搭配？家族字輩與喜用字衝突時怎麼辦？",
        "lead": "",
        "keywords": "字輩取名,字輩,字輩命名,家族字輩,寶寶取名,字輩怎麼取名字,字輩與五行,字輩與喜用字,新生兒命名,傳統命名,名序",
        "cta": [
            ("../newborn.html", "查看新生兒命名服務"),
            ("../rename.html", "查看專業改名服務"),
        ],
        "cta_heading": "需要字輩命名協助？",
        "cta_lead": "",
        "body": """
      <h2>什麼是字輩？</h2>
      <p>字輩是家族用以標示輩分的用字傳統，常見於家譜或宗族約定。透過固定某一字（或依輩分輪替的字），讓同輩名字在結構上有秩序，也是傳統命名很重要的一環。</p>

      <h2>字輩取名通常怎麼使用？</h2>
      <h3>固定字輩的位置</h3>
      <p>多數家族會指定名字中的某一字為字輩，雙名時常見放在中間字；也有家族規定首字或另有排列。命名前先確認位置，比先挑喜用字更重要。</p>
      <h3>字輩的字義</h3>
      <p>字輩字本身也有字義與意象。即使字輩固定，仍要理解它帶給全名的語氣，再決定另一字如何搭配。</p>
      <h3>字輩與姓氏搭配</h3>
      <p>姓＋字輩連讀的節奏，會影響整名好不好叫。字輩怎麼取名字，第一步往往是把「姓＋字輩」念順，再找其餘用字。</p>

      <h2>有固定字輩，還可以考慮五行嗎？</h2>
      <p>可以。字輩通常優先保留；五行／喜用方向則盡量在<strong>其餘用字</strong>上補強。也就是：家族規範先定位，命理參考再微調，而不是二者硬撞。</p>
      <h3>字輩與喜用方向</h3>
      <p>若字輩字與喜用方向不完全一致，不必立刻放棄字輩。實務上多在另一字尋找補足，或在多組候選中比較取捨。延伸：<a href="preferred-chars.html">喜用字怎麼取</a>、<a href="naming-methods.html">姓名學五格與八字差在哪</a>。</p>

      <h2>字輩與喜用字衝突時怎麼辦？</h2>
      <ul>
        <li><strong>保留字輩，調整另一字</strong>：最常見、也最能兼顧家族與命理</li>
        <li><strong>比較多組候選</strong>：讓父母看到不同氣質與取捨</li>
        <li><strong>變通需家人共識</strong>：若字輩字難念難寫，可討論同音近義或家族可接受方案，不宜單方面決定</li>
      </ul>
      <p>字輩與喜用字不是只能二選一，而是要找到「守輩分又能用」的平衡。</p>

      <h2>字輩取名還需要考慮哪些因素？</h2>
      <h3>字輩與音韻</h3>
      <p>固定字輩後，另一字的聲調、韻母會大幅影響朗讀。要避免全名卡頓、繞口或不良諧音。</p>
      <h3>字輩與整體名字氣質</h3>
      <p>除了規則正確，還要看名字整體是溫潤、清朗還是厚重，是否符合家庭期待與孩子未來長期使用。</p>
      <ul>
        <li>書寫難度與生僻字</li>
        <li>同輩是否撞字</li>
        <li>單名／雙名是否被家族允許</li>
      </ul>

      <h2>字輩與名字音韻怎麼搭配？</h2>
      <p>建議先固定「姓＋字輩」的讀法，再為剩餘位置選字，並整名朗讀數次。可同步參考 <a href="phonology.html">寶寶名字音韻</a>。</p>

      <h2>字輩命名有哪些常見問題？</h2>
      <details class="faq-item"><summary>字輩一定要放中間嗎？</summary>
        <p>不一定，依各家族規定。先確認家譜或長輩約定，再開始選字。</p></details>
      <details class="faq-item"><summary>字輩和五行衝突一定改字輩嗎？</summary>
        <p>通常先保留字輩，在其他用字上調整；若要變通字輩，需取得家人共識。</p></details>
      <details class="faq-item"><summary>新生兒命名一定要有字輩嗎？</summary>
        <p>不是每家都有。有家族字輩就納入條件；沒有字輩，仍可依命理、音義與喜好命名。</p></details>

      <h2>名序如何協助字輩命名？</h2>
      <p>委託時可提供字輩、喜用字、避開字與單雙名偏好。名序會在候選中標明字輩安排，並說明與喜用、音韻、字義的取捨，適用 <a href="../newborn.html">新生兒命名</a> 與需要調整用字的 <a href="../rename.html">專業改名</a>。</p>
""",
    },
    {
        "slug": "rename-reason",
        "title": "為什麼想改名字？成人改名常見原因與改名前應該想清楚的事｜名序",
        "description": "為什麼想改名字？整理成人改名常見原因，以及改名前該想清楚的事：原名分析、新名字選擇，還有姓名學、五行如何評估。適合考慮專業改名的人。",
        "h1": "為什麼想改名字？成人改名常見原因與改名前應該想清楚的事",
        "lead": "",
        "keywords": "專業改名,改名字,為什麼要改名字,改名原因,改名,成人改名原因,改名字好嗎,想改名字,姓名學改名,八字改名,名序",
        "cta": ("../rename.html", "查看專業改名服務"),
        "cta_heading": "想重新選擇一個適合自己的名字？",
        "cta_lead": "",
        "body": """
      <h2>為什麼有人會想改名字？</h2>
      <p>想改名字，通常不是忽然興起，而是生活裡累積已久的感受：對原名不習慣、不好叫、不好解釋，或覺得它已無法代表現在的自己。專業改名要處理的，往往是「為什麼要改名字」背後的真實需求，而不只是換兩個字。</p>

      <h2>成人改名常見原因有哪些？</h2>
      <h3>對原姓名沒有認同感</h3>
      <p>覺得名字與性格、價值觀或自我形象落差很大，長期叫起來彆扭。</p>
      <h3>希望改善名字的讀音或字義</h3>
      <p>難念、常被叫錯、諧音尷尬，或字義讓自己不舒服。</p>
      <h3>人生階段改變</h3>
      <p>就業、婚育、遷居、重新出發時，希望名字更能對應下一個階段。</p>
      <h3>工作與社交使用需求</h3>
      <p>職場點名、對外介紹、名片與社群帳號，希望讀音清楚、好記好寫。</p>
      <h3>希望重新選擇一個名字</h3>
      <p>不是否定過去，而是想主動挑選一個更能長期使用的名字。</p>

      <h2>改名字前應該先想清楚什麼？</h2>
      <ul>
        <li>你最想改善的是什麼：讀音、字義、認同感，還是使用場合？</li>
        <li>你希望保留什麼特質或家族條件？</li>
        <li>是否準備好處理戶政與周遭重新認識新名的成本？</li>
        <li>你期待的是「更好用的名字」，還是把改名當成絕對改運？（後者請重新對齊期待）</li>
      </ul>
      <p>也可一併閱讀 <a href="rename-timing.html">改名時機</a>。</p>

      <h2>只是覺得名字不好聽，需要改嗎？</h2>
      <p>不一定立刻要改。若只是短暫不喜歡，可先觀察日常困擾是否持續、是否影響社交與自我介紹。若長期不好叫、常被誤會，或明顯影響認同感，再進入專業改名評估會比較穩。</p>
      <p>「改名字好嗎」沒有標準答案；關鍵是原因是否清楚、新名是否真的更好用。</p>

      <h2>原本的名字可以先分析嗎？</h2>
      <h3>改名前先了解原名</h3>
      <p>可以，而且建議先做。了解原名的音韻、字義與結構後，才知道新名字要往哪裡調整，也比較能比較前後差異。名序專業改名會先做原姓名分析，再探索新名。</p>

      <h2>改名字時應該如何選擇新名字？</h2>
      <ol>
        <li>對齊改名原因與使用場合</li>
        <li>確認可接受的風格、單雙名、避開字</li>
        <li>比較多組候選，而不只看一組</li>
        <li>整名朗讀、檢查諧音與書寫</li>
        <li>確認你願意長期使用，再進入戶政程序</li>
      </ol>

      <h2>姓名學、五行與新名字可以怎麼評估？</h2>
      <p>姓名學改名、八字改名常被一起討論：前者偏姓名結構參考，後者偏出生條件與五行方向。實務上可互相參照，再加上音韻與字義，避免只看單一規則。延伸：<a href="naming-methods.html">姓名學五格與八字差在哪</a>、<a href="preferred-chars.html">喜用字怎麼取</a>。</p>

      <h2>名序如何協助成人改名？</h2>
      <p>名序專業改名包含：原姓名分析、新名探索、前後比較，以及改名理由與命名理念整理，並交付 PDF 報告。詳見 <a href="../rename.html">專業改名</a> 與 <a href="../works.html">真實案例</a>。</p>

      <h2>常見問題</h2>
      <details class="faq-item"><summary>改名一定要有很嚴重的理由嗎？</summary>
        <p>不必用「嚴不嚴重」衡量。重點是困擾是否真實、持續，以及你是否清楚想改善什麼。</p></details>
      <details class="faq-item"><summary>可以只改名字、不改姓嗎？</summary>
        <p>多數成人改名討論的是名；姓氏變更涉及不同法規與情境，需另向戶政確認。</p></details>
      <details class="faq-item"><summary>名序會保證改運嗎？</summary>
        <p>不會。我們提供文化命理參考與命名設計，不宣稱保證事業、財運、感情或健康結果。</p></details>
""",
    },
    {
        "slug": "rename-timing",
        "title": "成人改名什麼時候適合？改名字前可以先評估的 7 個面向｜名序",
        "description": "成人改名什麼時候適合？說明改名時機不必只看特定日子，並整理改名字前可先評估的面向：人生階段、職涯、使用感受、讀音字義、原名與新名比較、戶籍與日常使用。",
        "h1": "成人改名什麼時候適合？改名字前可以先評估的 7 個面向",
        "lead": "",
        "keywords": "成人改名,改名字,改名時機,什麼時候改名字,成人改名時機,改名時間,改名字要注意什麼,姓名學改名,八字改名,改名需要什麼,名序",
        "cta": ("../rename.html", "查看專業改名服務"),
        "cta_heading": "想評估現在是否適合改名？",
        "cta_lead": "",
        "body": """
      <h2>成人改名一定要挑特定時間嗎？</h2>
      <p>不一定。很多人問「什麼時候改名字」「改名時間」時，會先想到黃曆吉日；那可以當文化參考，但成人改名時機更關鍵的是：你是否已清楚原因、是否準備好長期使用新名，以及戶政與生活轉換成本是否可負擔。</p>

      <h2>什麼情況下可以考慮改名字？</h2>
      <h3>人生階段變化</h3>
      <p>就業、婚育、遷居、重新出發，希望名字更能對應下一階段。</p>
      <h3>工作與職涯轉換</h3>
      <p>對外介紹、名片、面試與職場點名，需要更清楚、好記的讀音與形象。</p>
      <h3>長期使用感受</h3>
      <p>不是短暫不喜歡，而是長期叫起來彆扭、常被叫錯，或缺乏認同感。</p>
      <p>更多原因整理見 <a href="rename-reason.html">為什麼想改名字</a>。</p>

      <h2>決定改名前可以先評估哪些事情？</h2>
      <ul>
        <li>最想改善的是讀音、字義、認同感，還是使用場合？</li>
        <li>困擾是否持續、是否影響工作與社交？</li>
        <li>是否已準備好告訴家人／同事新名字？</li>
        <li>戶政與證件更新的時間與流程是否了解？（請向戶政確認）</li>
        <li>期待是否合理：要的是更好用的名字，而非絕對改運保證</li>
      </ul>

      <h2>原姓名分析為什麼重要？</h2>
      <p>改名字要注意什麼，第一步常是先看懂原名。了解原名的音韻、字義與結構後，才知道新名要往哪裡調整，也比較能說明「為什麼改、改了什麼」。</p>

      <h2>新名字應該從哪些面向評估？</h2>
      <h3>名字讀音與字義</h3>
      <p>好不好叫、好不好寫、寓意是否清楚，會直接影響長期使用。</p>
      <h3>原名與新名比較</h3>
      <p>前後對照，才能確認新名真的改善了你在意的點，而不是只是「不一樣」。</p>
      <ul>
        <li>辨識度與諧音風險</li>
        <li>正式場合與日常暱稱是否自然</li>
        <li>是否符合你可接受的風格與條件</li>
      </ul>

      <h2>改名後的實際生活需要考慮什麼？</h2>
      <h3>實際戶籍與日常使用</h3>
      <p>改名需要什麼，除了選名，還包括戶籍登記後的證件、帳戶、通訊與人際重新認識。法律與行政細節請以戶政事務所說明為準。</p>

      <h2>姓名學與八字在改名時扮演什麼角色？</h2>
      <p>姓名學改名偏姓名結構參考；八字改名偏出生條件與五行方向。兩者可互相參照，再結合音韻與字義，避免只看單一規則。延伸：<a href="naming-methods.html">姓名學五格與八字差在哪</a>。</p>

      <h2>名序如何進行成人改名分析？</h2>
      <p>名序會先做原姓名分析，再依你的改名原因與希望方向提出候選，並做原名／新名比較與說明，最後交付 PDF 報告。詳見 <a href="../rename.html">專業改名</a>。</p>

      <h2>常見問題</h2>
      <details class="faq-item"><summary>一定要等某個流年才改名嗎？</summary>
        <p>不必把單一時間點當成唯一條件。更重要的是原因清楚、新名好用、你已準備好使用。</p></details>
      <details class="faq-item"><summary>現在不喜歡原名，是不是就該立刻改？</summary>
        <p>建議先確認困擾是否持續、是否影響生活；必要時可先做原名分析再決定。</p></details>
      <details class="faq-item"><summary>名序會指定唯一吉日嗎？</summary>
        <p>我們著重命名分析與使用評估；黃曆可作參考，但不以「絕對吉日」取代專業判斷。</p></details>
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
        cta_items = cta if isinstance(cta, list) else [cta]
        btn_bits = []
        for i, (href, label) in enumerate(cta_items):
            cls = "btn btn-gold" if i == 0 else "btn btn-outline"
            btn_bits.append(f'<a class="{cls}" href="{href}">{label}</a>')
        btn_bits.append(
            '<a class="btn btn-outline" href="https://line.me/R/ti/p/@187xckjb" '
            'target="_blank" rel="noopener noreferrer" data-mx-track="line_add">Line 諮詢</a>'
        )
        cta_heading = g.get("cta_heading") or "下一步"
        cta_lead = g.get("cta_lead")
        if cta_lead is None:
            cta_lead = "以命為本，以字成名。"
        lead_html = f"<p>{cta_lead}</p>" if cta_lead else ""
        cta_html = f"""
    <section class="section"><div class="container reveal"><div class="cta-band">
      <h2>{cta_heading}</h2>
      {lead_html}
      {" ".join(btn_bits)}
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
