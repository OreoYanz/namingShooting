/** 名序官網 · 共用 Header / Footer */
(function () {
  const NAV = [
    { href: "index.html", label: "首頁" },
    { href: "about.html", label: "關於名序" },
    { href: "newborn.html", label: "新生兒命名" },
    { href: "rename.html", label: "成人改名" },
    { href: "liunian.html", label: "流年分析" },
    { href: "works.html", label: "命名作品" },
    { href: "memorial.html", label: "命名紀念" },
    { href: "faq.html", label: "常見問題" },
    { href: "contact.html", label: "聯絡方式" },
  ];

  const page = (document.body.dataset.page || "").trim();
  const year = new Date().getFullYear();

  function isActive(href) {
    const key = href === "index.html" ? "home" : href.replace(".html", "");
    return page === key;
  }

  function navHtml() {
    return NAV.map(
      (n) =>
        `<li><a href="${n.href}" class="${isActive(n.href) ? "active" : ""}">${n.label}</a></li>`
    ).join("");
  }

  const header = document.getElementById("site-header");
  if (header) {
    header.className = "site-header";
    header.innerHTML = `
      <div class="header-inner">
        <a class="brand-link" href="index.html" aria-label="名序首頁">
          <img class="brand-logo" src="assets/logo.png" width="40" height="40" alt="" />
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
          <a class="btn btn-primary" href="contact.html">聯絡名序</a>
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
              <li><a href="newborn.html">新生兒命名</a></li>
              <li><a href="rename.html">成人改名</a></li>
              <li><a href="liunian.html">流年分析</a></li>
              <li><a href="contact.html">聯絡洽詢</a></li>
              <li><a href="index.html#purchase-flow">購買流程</a></li>
            </ul>
          </div>
          <div>
            <h4>認識名序</h4>
            <ul>
              <li><a href="about.html">品牌故事</a></li>
              <li><a href="works.html">命名作品</a></li>
              <li><a href="memorial.html">命名紀念</a></li>
              <li><a href="faq.html">常見問題</a></li>
            </ul>
          </div>
          <div>
            <h4>聯絡</h4>
            <ul>
              <li><a href="contact.html">聯絡方式</a></li>
              <li><a href="contact.html#line">Line 官方帳號</a></li>
              <li><a href="mailto:nameshootingmingxu@gmail.com">電子信箱</a></li>
              <li><a href="contact.html#shopee">蝦皮賣場</a></li>
            </ul>
          </div>
        </div>
        <div class="footer-bottom">
          <p>名序 · 版權所有 © ${year}</p>
          <p class="footer-legal">本網站內容僅供文化參考，命理分析不構成人生保證。用字與改名請向戶政事務所確認。</p>
        </div>
      </div>`;
  }
})();
