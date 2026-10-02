/** 名序｜每日吉祥：載入、今日下載、歷日下載 */
(function (global) {
  function gifFilename(dateKey) {
    const key = String(dateKey || "").replace(/\D/g, "") || "latest";
    return "名序_每日吉祥_" + key + ".gif";
  }

  function gifUrl(data) {
    if (!data || !data.media) return "";
    return data.media.gifDated || data.media.gif || data.media.webp || "";
  }

  function escapeHtml(s) {
    return String(s == null ? "" : s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  async function fetchDailyLatest() {
    const res = await fetch("data/daily_latest.json?t=" + Date.now());
    if (!res.ok) throw new Error("no daily data");
    return res.json();
  }

  async function fetchDailyArchive() {
    const res = await fetch("data/daily_archive.json?t=" + Date.now());
    if (!res.ok) throw new Error("no archive");
    return res.json();
  }

  async function downloadGif(url, filename) {
    if (!url) return;
    const clean = url.split("?")[0];
    const name = filename || "名序_每日吉祥.gif";
    try {
      const res = await fetch(clean + "?t=" + Date.now());
      if (!res.ok) throw new Error("fetch failed");
      const blob = await res.blob();
      const objectUrl = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = objectUrl;
      a.download = name;
      a.rel = "noopener";
      document.body.appendChild(a);
      a.click();
      a.remove();
      setTimeout(function () {
        URL.revokeObjectURL(objectUrl);
      }, 1500);
    } catch (err) {
      const a = document.createElement("a");
      a.href = clean;
      a.download = name;
      a.target = "_blank";
      a.rel = "noopener";
      document.body.appendChild(a);
      a.click();
      a.remove();
    }
  }

  function bindDownloadButton(btn, dataOrUrl, dateKey) {
    if (!btn) return;
    let url = "";
    let key = dateKey;
    if (typeof dataOrUrl === "string") {
      url = dataOrUrl;
    } else if (dataOrUrl) {
      url = gifUrl(dataOrUrl) || dataOrUrl.gif || "";
      key = key || dataOrUrl.dateKey || dataOrUrl.date;
    }
    if (!url) {
      btn.hidden = true;
      return;
    }
    btn.hidden = false;
    btn.disabled = false;
    const name = gifFilename(key);
    btn.onclick = async function () {
      btn.disabled = true;
      const prev = btn.textContent;
      btn.textContent = "下載中…";
      try {
        await downloadGif(url, name);
      } finally {
        btn.textContent = prev;
        btn.disabled = false;
      }
    };
  }

  function renderArchiveList(listEl, days, options) {
    if (!listEl) return;
    const opts = options || {};
    const limit = opts.limit || 0;
    const items = Array.isArray(days) ? days.slice() : [];
    const shown = limit > 0 ? items.slice(0, limit) : items;

    if (!shown.length) {
      listEl.innerHTML = '<p class="daily-archive-empty">尚無歷日內容。</p>';
      return;
    }

    listEl.innerHTML = shown
      .map(function (day) {
        const key = escapeHtml(day.dateKey || "");
        const title = escapeHtml(day.dateLine1 || day.date || key);
        const theme = escapeHtml(
          (day.fortuneLevel ? "今日" + day.fortuneLevel : "") +
            (day.mainTheme ? "｜" + day.mainTheme : "")
        );
        const summary = escapeHtml(day.summaryText || "");
        const preview = escapeHtml(day.preview || "");
        const gif = escapeHtml(day.gif || "");
        return (
          '<article class="daily-archive-item" data-date-key="' +
          key +
          '">' +
          '<div class="daily-archive-thumb">' +
          (preview
            ? '<img src="' + preview + '" alt="' + title + ' 預覽" loading="lazy" />'
            : "") +
          "</div>" +
          '<div class="daily-archive-meta">' +
          "<h3>" +
          title +
          "</h3>" +
          (theme ? '<p class="daily-archive-theme">' + theme + "</p>" : "") +
          (summary ? '<p class="daily-archive-summary">' + summary + "</p>" : "") +
          '<button type="button" class="btn btn-outline daily-archive-dl" data-gif="' +
          gif +
          '" data-key="' +
          key +
          '">下載 GIF</button>' +
          "</div>" +
          "</article>"
        );
      })
      .join("");

    listEl.querySelectorAll(".daily-archive-dl").forEach(function (btn) {
      bindDownloadButton(btn, btn.getAttribute("data-gif"), btn.getAttribute("data-key"));
    });
  }

  async function mountDailyArchive() {
    const section = document.getElementById("dailyArchive");
    const listEl = document.getElementById("dailyArchiveList");
    const statusEl = document.getElementById("dailyArchiveStatus");
    const countEl = document.getElementById("dailyArchiveCount");
    if (!listEl) return;

    try {
      const archive = await fetchDailyArchive();
      const days = archive.days || [];
      if (statusEl) statusEl.hidden = true;
      if (section) section.hidden = false;
      if (countEl) countEl.textContent = String(archive.count || days.length);
      const limitAttr = listEl.getAttribute("data-limit");
      const limit = limitAttr ? parseInt(limitAttr, 10) : 0;
      renderArchiveList(listEl, days, { limit: limit > 0 ? limit : 0 });
    } catch (e) {
      if (statusEl) {
        statusEl.hidden = false;
        statusEl.textContent = "歷日內容準備中。";
      }
      if (listEl) listEl.innerHTML = "";
    }
  }

  async function mountDailyPage() {
    const empty = document.getElementById("dailyEmpty");
    const kicker = document.getElementById("dailyKicker");
    try {
      const d = await fetchDailyLatest();
      if (kicker) {
        kicker.textContent = (d.brandName || "名序") + "｜" + (d.brandTagline || "");
      }

      const dateCard = document.getElementById("dailyDateCard");
      if (dateCard) {
        dateCard.hidden = false;
        document.getElementById("dailyDateLines").innerHTML =
          (d.dateLine1 || "") + "<br/>" + (d.dateLine2 || "");
      }

      const fortuneCard = document.getElementById("dailyFortuneCard");
      if (fortuneCard) {
        fortuneCard.hidden = false;
        document.getElementById("dailyFortuneLines").textContent =
          "今日" + (d.fortuneLevel || "") + "｜" + (d.mainTheme || "") +
          (d.secondaryTheme ? "・" + d.secondaryTheme : "");
        document.getElementById("dailyYi").textContent = "宜：" + (d.yi || []).join("、");
        document.getElementById("dailyJi").textContent = "忌：" + (d.ji || []).join("、");
      }

      if (d.summaryText) {
        document.getElementById("dailySummaryCard").hidden = false;
        document.getElementById("dailySummary").textContent = d.summaryText;
      }

      const gifPath = gifUrl(d);
      if (gifPath) {
        document.getElementById("dailyMediaCard").hidden = false;
        const gif = document.getElementById("dailyGif");
        if (gif) gif.src = gifPath + "?t=" + Date.now();
        const actions = document.getElementById("dailyActions");
        if (actions) actions.hidden = false;
        bindDownloadButton(document.getElementById("btnDownloadGif"), d);
      }
    } catch (e) {
      if (kicker) kicker.textContent = "尚無內容";
      if (empty) empty.hidden = false;
    }
  }

  async function mountHomeDaily() {
    const root = document.getElementById("homeDaily");
    if (!root) return;
    const status = document.getElementById("homeDailyStatus");
    const body = document.getElementById("homeDailyBody");
    try {
      const d = await fetchDailyLatest();
      if (status) status.hidden = true;
      if (body) body.hidden = false;

      const dateEl = document.getElementById("homeDailyDate");
      if (dateEl) dateEl.textContent = d.dateLine1 || d.date || "";

      const fortuneEl = document.getElementById("homeDailyFortune");
      if (fortuneEl) {
        fortuneEl.textContent =
          "今日" + (d.fortuneLevel || "") +
          (d.mainTheme ? "｜" + d.mainTheme : "") +
          (d.secondaryTheme ? "・" + d.secondaryTheme : "");
      }

      const yiEl = document.getElementById("homeDailyYi");
      if (yiEl) yiEl.textContent = "宜：" + (d.yi || []).join("、");
      const jiEl = document.getElementById("homeDailyJi");
      if (jiEl) jiEl.textContent = "忌：" + (d.ji || []).join("、");

      const summaryEl = document.getElementById("homeDailySummary");
      if (summaryEl) summaryEl.textContent = d.summaryText || d.shortMessage || "";

      const gifPath = gifUrl(d);
      const img = document.getElementById("homeDailyGif");
      if (img && gifPath) {
        img.src = gifPath + "?t=" + Date.now();
        img.hidden = false;
      }
      bindDownloadButton(document.getElementById("homeDownloadGif"), d);
    } catch (e) {
      if (status) {
        status.hidden = false;
        status.textContent = "今日內容準備中，請稍後再來。";
      }
      if (body) body.hidden = true;
    }
  }

  global.MingxuDaily = {
    fetchDailyLatest: fetchDailyLatest,
    fetchDailyArchive: fetchDailyArchive,
    downloadGif: downloadGif,
    mountDailyPage: mountDailyPage,
    mountHomeDaily: mountHomeDaily,
    mountDailyArchive: mountDailyArchive,
  };

  document.addEventListener("DOMContentLoaded", function () {
    if (document.body && document.body.dataset.page === "daily") {
      mountDailyPage();
    }
    if (document.getElementById("homeDaily")) {
      mountHomeDaily();
    }
    if (document.getElementById("dailyArchiveList")) {
      mountDailyArchive();
    }
  });
})(window);
