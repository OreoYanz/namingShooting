/** 名序官網 · Meta Pixel + Google 轉換追蹤 */
(function () {
  var cfg = window.MX_TRACKING || {};
  var metaId = String(cfg.metaPixelId || "").trim();
  var awId = String(cfg.googleAdsId || "").trim();
  var awLabel = String(cfg.googleAdsConversionLabel || "").trim();
  var ga4Id = String(cfg.ga4MeasurementId || "").trim();

  function loadScript(src, attrs) {
    var s = document.createElement("script");
    s.async = true;
    s.src = src;
    if (attrs) {
      Object.keys(attrs).forEach(function (k) {
        s.setAttribute(k, attrs[k]);
      });
    }
    var first = document.getElementsByTagName("script")[0];
    if (first && first.parentNode) first.parentNode.insertBefore(s, first);
    else document.head.appendChild(s);
  }

  /* —— Meta Pixel —— */
  if (metaId) {
    !(function (f, b, e, v, n, t, s) {
      if (f.fbq) return;
      n = f.fbq = function () {
        n.callMethod ? n.callMethod.apply(n, arguments) : n.queue.push(arguments);
      };
      if (!f._fbq) f._fbq = n;
      n.push = n;
      n.loaded = !0;
      n.version = "2.0";
      n.queue = [];
      t = b.createElement(e);
      t.async = !0;
      t.src = v;
      s = b.getElementsByTagName(e)[0];
      s.parentNode.insertBefore(t, s);
    })(window, document, "script", "https://connect.facebook.net/en_US/fbevents.js");
    window.fbq("init", metaId);
    window.fbq("track", "PageView");
  }

  /* —— Google Ads / GA4 (gtag) —— */
  if (awId || ga4Id) {
    window.dataLayer = window.dataLayer || [];
    window.gtag =
      window.gtag ||
      function () {
        window.dataLayer.push(arguments);
      };
    loadScript(
      "https://www.googletagmanager.com/gtag/js?id=" + encodeURIComponent(awId || ga4Id)
    );
    window.gtag("js", new Date());
    if (awId) window.gtag("config", awId);
    if (ga4Id) window.gtag("config", ga4Id);
  }

  function isLineOutbound(href, el) {
    if (el && el.getAttribute("data-mx-track") === "line_add") return true;
    if (!href) return false;
    return /line\.me/i.test(href) || /lin\.ee\//i.test(href);
  }

  /**
   * 關鍵轉換：點擊前往 LINE（加好友意圖）
   * Meta → Lead；Google Ads → conversion（需填 Label）
   */
  function trackLineAdd(source) {
    try {
      if (metaId && typeof window.fbq === "function") {
        window.fbq("track", "Lead", {
          content_name: "LINE_Add",
          content_category: "contact",
          source: source || "line",
        });
      }
      if (awId && awLabel && typeof window.gtag === "function") {
        window.gtag("event", "conversion", {
          send_to: awId + "/" + awLabel,
        });
      }
      if (ga4Id && typeof window.gtag === "function") {
        window.gtag("event", "generate_lead", {
          method: "line",
          event_category: "conversion",
          event_label: source || "line_add",
        });
      }
    } catch (err) {
      /* 追蹤失敗不影響使用者前往 LINE */
    }
  }

  document.addEventListener(
    "click",
    function (e) {
      var t = e.target;
      if (!t || !t.closest) return;
      var a = t.closest("a");
      if (!a) return;
      var href = a.getAttribute("href") || "";
      if (isLineOutbound(href, a)) trackLineAdd(href);
    },
    true
  );

  window.MX_TRACK = {
    lineAdd: trackLineAdd,
  };
})();
