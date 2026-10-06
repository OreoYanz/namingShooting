/** 名序官網 · 共用 Header / Footer */
(function () {
  const BASE = (document.body && document.body.dataset.assetBase) || "";
  function u(path) {
    return BASE + path;
  }

  const NAV = [
    { href: "index.html", label: "首頁" },
    { href: "daily.html", label: "每日吉祥" },
    { href: "about.html", label: "關於名序" },
    { href: "newborn.html", label: "新生兒命名" },
    { href: "rename.html", label: "專業改名" },
    { href: "liunian.html", label: "流年分析" },
    { href: "index.html#showcase", label: "成果展示" },
    { href: "works.html", label: "真實案例" },
    { href: "faq.html", label: "常見問題" },
    { href: "contact.html", label: "聯絡方式" },
  ];

  const page = (document.body.dataset.page || "").trim();
  const year = new Date().getFullYear();

  function isActive(href) {
    if (page === "daily-day" && href === "daily.html") return true;
    const key = href === "index.html" ? "home" : href.replace(".html", "");
    return page === key;
  }

  function navHtml() {
    return NAV.map(
      (n) =>
        `<li><a href="${u(n.href)}" class="${isActive(n.href) ? "active" : ""}">${n.label}</a></li>`
    ).join("");
  }

  const header = document.getElementById("site-header");
  if (header) {
    header.className = "site-header";
    header.innerHTML = `
      <div class="header-inner">
        <a class="brand-link" href="${u("index.html")}" aria-label="名序首頁">
          <img class="brand-logo" src="${u("assets/logo.png")}" width="40" height="40" alt="" />
          <span class="brand-name">名序</span>
        </a>
        <button type="button" class="nav-toggle" id="navToggle" aria-expanded="false" aria-controls="siteNav" aria-label="開啟選單">
          <span></span><span></span><span></span>
        </button>
      </div>
      <button type="button" class="nav-backdrop" id="navBackdrop" aria-label="關閉選單" tabindex="-1"></button>
      <nav class="site-nav" id="siteNav" aria-label="主要導覽">
        <ul>${navHtml()}</ul>
        <div class="nav-cta">
          <a class="btn btn-primary" href="https://line.me/R/ti/p/@187xckjb" target="_blank" rel="noopener noreferrer" data-mx-track="line_add">Line 諮詢</a>
        </div>
      </nav>`;
    const backdrop = document.getElementById("navBackdrop");
    const nav = document.getElementById("siteNav");
    if (backdrop) document.body.appendChild(backdrop);
    if (nav) document.body.appendChild(nav);
  }

  const footer = document.getElementById("site-footer");
  if (footer) {
    footer.className = "site-footer";
    footer.innerHTML = `
      <div class="container">
        <div class="footer-grid">
          <div>
            <p class="footer-brand">名序</p>
            <p>以命為本，以字成名。<br />一名一序，一生一願。</p>
            <p class="footer-legal">替每一個人生，寫下值得珍藏的第一行文字。</p>
          </div>
          <div>
            <h4>服務</h4>
            <ul>
              <li><a href="${u("newborn.html")}">新生兒命名</a></li>
              <li><a href="${u("rename.html")}">專業改名</a></li>
              <li><a href="${u("liunian.html")}">流年分析</a></li>
              <li><a href="${u("contact.html")}#line">Line 諮詢</a></li>
              <li><a href="${u("index.html")}#purchase-flow">購買流程</a></li>
            </ul>
          </div>
          <div>
            <h4>認識名序</h4>
            <ul>
              <li><a href="${u("daily.html")}">每日吉祥</a></li>
              <li><a href="${u("about.html")}">品牌故事</a></li>
              <li><a href="${u("works.html")}">真實案例</a></li>
              <li><a href="${u("faq.html")}">常見問題</a></li>
              <li><a href="${u("guides/index.html")}">命名知識</a></li>
            </ul>
          </div>
          <div>
            <h4>聯絡</h4>
            <ul>
              <li><a href="${u("contact.html")}">聯絡方式</a></li>
              <li><a href="${u("contact.html")}#line">Line 官方帳號</a></li>
              <li><a href="mailto:nameshootingmingxu@gmail.com">電子信箱</a></li>
              <li><a href="https://tw.shp.ee/WSCcyTJW" target="_blank" rel="noopener noreferrer">蝦皮賣場</a></li>
            </ul>
          </div>
        </div>
        <div class="footer-bottom">
          <p>名序 · 版權所有 © ${year}</p>
          <p class="footer-visits">網站瀏覽：<span id="siteVisitCount">—</span> 人次</p>
          <p class="footer-legal">本網站內容僅供文化參考，命理分析不構成人生保證。用字與改名請向戶政事務所確認。</p>
        </div>
      </div>`;
  }

  // Site-wide visit counter (once per browser session)
  (function mountVisitCounter() {
    const el = document.getElementById("siteVisitCount");
    if (!el) return;
    const host = (location && location.hostname) || "";
    if (!host || host === "localhost" || host === "127.0.0.1") {
      el.textContent = "—";
      return;
    }
    const storageKey = "mx_visit_counted_v1";
    const cacheKey = "mx_visit_count_v1";
    const apiBase = "https://abacus.jasoncameron.dev";
    const ns = "mingxu.mingxu.workers.dev";
    const key = "mingxu";

    function paint(n) {
      const num = Number(n);
      if (!isFinite(num) || num < 0) {
        el.textContent = "—";
        return;
      }
      el.textContent = Math.floor(num).toLocaleString("zh-TW");
    }

    const cached = sessionStorage.getItem(cacheKey);
    if (cached) paint(cached);

    const already = sessionStorage.getItem(storageKey) === "1";
    const endpoint = already
      ? apiBase + "/get/" + encodeURIComponent(ns) + "/" + encodeURIComponent(key)
      : apiBase + "/hit/" + encodeURIComponent(ns) + "/" + encodeURIComponent(key);

    fetch(endpoint, { method: "GET", mode: "cors", credentials: "omit" })
      .then(function (res) {
        if (!res.ok) throw new Error("counter failed");
        return res.json();
      })
      .then(function (data) {
        const value = data && (data.value != null ? data.value : data.count);
        if (value == null) throw new Error("no value");
        sessionStorage.setItem(cacheKey, String(value));
        if (!already) sessionStorage.setItem(storageKey, "1");
        paint(value);
      })
      .catch(function () {
        if (!cached) el.textContent = "—";
      });
  })();
})();
