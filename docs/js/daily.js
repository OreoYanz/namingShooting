/** 名序｜每日吉祥：依台北時間顯示當日；可預先發布未來日 */
(function (global) {
  function gifFilename(dateKey) {
    const key = String(dateKey || "").replace(/\D/g, "") || "latest";
    return "名序_每日吉祥_" + key + ".gif";
  }

  function gifUrl(data) {
    if (!data) return "";
    if (data.media) {
      return data.media.gifDated || data.media.gif || data.media.webp || "";
    }
    return data.gif || "";
  }

  function escapeHtml(s) {
    return String(s == null ? "" : s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  /** Asia/Taipei calendar date as YYYYMMDD */
  function taiwanDateKey(dateObj) {
    const d = dateObj || new Date();
    const parts = new Intl.DateTimeFormat("en-CA", {
      timeZone: "Asia/Taipei",
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
    }).formatToParts(d);
    const get = function (type) {
      const hit = parts.find(function (p) {
        return p.type === type;
      });
      return hit ? hit.value : "";
    };
    return get("year") + get("month") + get("day");
  }

  async function fetchJson(path) {
    const res = await fetch(path + (path.indexOf("?") >= 0 ? "&" : "?") + "t=" + Date.now());
    if (!res.ok) throw new Error("fetch failed: " + path);
    return res.json();
  }

  async function fetchDailyByKey(dateKey) {
    return fetchJson("data/daily/" + dateKey + ".json");
  }

  async function fetchDailyLatest() {
    return fetchJson("data/daily_latest.json");
  }

  async function fetchDailyArchive() {
    return fetchJson("data/daily_archive.json");
  }

  /**
   * Resolve what the site should show "today":
   * 1) exact Taipei today if published
   * 2) else newest published day <= today
   * 3) else daily_latest.json fallback
   */
  async function resolveDisplayDaily() {
    const todayKey = taiwanDateKey();
    try {
      const exact = await fetchDailyByKey(todayKey);
      return { data: exact, mode: "today", todayKey: todayKey };
    } catch (e) {
      /* continue */
    }

    try {
      const archive = await fetchDailyArchive();
      const days = (archive.days || [])
        .map(function (d) {
          return d;
        })
        .filter(function (d) {
          return d && String(d.dateKey || "") <= todayKey;
        })
        .sort(function (a, b) {
          return String(b.dateKey).localeCompare(String(a.dateKey));
        });
      if (days.length) {
        try {
          const keyed = await fetchDailyByKey(days[0].dateKey);
          return {
            data: keyed,
            mode: "fallback",
            todayKey: todayKey,
            fallbackKey: days[0].dateKey,
          };
        } catch (e2) {
          return {
            data: {
              dateKey: days[0].dateKey,
              dateLine1: days[0].dateLine1,
              fortuneLevel: days[0].fortuneLevel,
              mainTheme: days[0].mainTheme,
              secondaryTheme: days[0].secondaryTheme,
              summaryText: days[0].summaryText,
              media: { gifDated: days[0].gif, gif: days[0].gif },
            },
            mode: "fallback",
            todayKey: todayKey,
            fallbackKey: days[0].dateKey,
          };
        }
      }
    } catch (e3) {
      /* continue */
    }

    const latest = await fetchDailyLatest();
    const latestKey = String(latest.dateKey || "");
    if (latestKey && latestKey <= todayKey) {
      return { data: latest, mode: "latest", todayKey: todayKey };
    }
    throw new Error("no displayable daily for " + todayKey);
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
    const todayKey = opts.todayKey || taiwanDateKey();
    const items = Array.isArray(days) ? days.slice() : [];
    const shown = limit > 0 ? items.slice(0, limit) : items;

    if (!shown.length) {
      listEl.innerHTML = '<p class="daily-archive-empty">尚無歷日內容。</p>';
      return;
    }

    listEl.innerHTML = shown
      .map(function (day) {
        const key = escapeHtml(day.dateKey || "");
        const rawKey = String(day.dateKey || "");
        const isFuture = rawKey > todayKey;
        const isToday = rawKey === todayKey;
        const badge = isToday
          ? '<span class="daily-archive-badge is-today">今日</span>'
          : isFuture
            ? '<span class="daily-archive-badge is-scheduled">已排程</span>'
            : "";
        const title = escapeHtml(day.dateLine1 || day.date || key);
        const theme = escapeHtml(
          (day.fortuneLevel ? "今日" + day.fortuneLevel : "") +
            (day.mainTheme ? "｜" + day.mainTheme : "")
        );
        const summary = escapeHtml(day.summaryText || "");
        const preview = escapeHtml(day.preview || "");
        const gif = escapeHtml(day.gif || "");
        return (
          '<article class="daily-archive-item' +
          (isFuture ? " is-scheduled" : "") +
          (isToday ? " is-today" : "") +
          '" data-date-key="' +
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
          " " +
          badge +
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
    const todayKey = taiwanDateKey();

    try {
      const archive = await fetchDailyArchive();
      const days = archive.days || [];
      if (statusEl) statusEl.hidden = true;
      if (section) section.hidden = false;
      if (countEl) countEl.textContent = String(archive.count || days.length);
      const limitAttr = listEl.getAttribute("data-limit");
      const limit = limitAttr ? parseInt(limitAttr, 10) : 0;
      renderArchiveList(listEl, days, {
        limit: limit > 0 ? limit : 0,
        todayKey: todayKey,
      });
    } catch (e) {
      if (statusEl) {
        statusEl.hidden = false;
        statusEl.textContent = "歷日內容準備中。";
      }
      if (listEl) listEl.innerHTML = "";
    }
  }

  function applyDailyData(d, meta) {
    const mode = (meta && meta.mode) || "today";
    const todayKey = (meta && meta.todayKey) || taiwanDateKey();
    return { d: d, mode: mode, todayKey: todayKey, fallbackKey: meta && meta.fallbackKey };
  }

  async function mountDailyPage() {
    const empty = document.getElementById("dailyEmpty");
    const kicker = document.getElementById("dailyKicker");
    const noteEl = document.getElementById("dailyScheduleNote");
    try {
      const resolved = await resolveDisplayDaily();
      const info = applyDailyData(resolved.data, resolved);
      const d = info.d;
      if (kicker) {
        kicker.textContent = (d.brandName || "名序") + "｜" + (d.brandTagline || "");
      }
      if (noteEl) {
        if (info.mode === "today") {
          noteEl.hidden = true;
        } else {
          noteEl.hidden = false;
          noteEl.textContent =
            "今日（" +
            info.todayKey +
            "）尚未到點或尚未發布，暫顯示最近一日" +
            (info.fallbackKey || d.dateKey || "") +
            "。";
        }
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
      if (noteEl) {
        noteEl.hidden = false;
        noteEl.textContent = "今日內容尚未發布；若已預先排程，等到當日 00:00（台北時間）後會自動顯示。";
      }
    }
  }

  async function mountHomeDaily() {
    const root = document.getElementById("homeDaily");
    if (!root) return;
    const status = document.getElementById("homeDailyStatus");
    const body = document.getElementById("homeDailyBody");
    try {
      const resolved = await resolveDisplayDaily();
      const info = applyDailyData(resolved.data, resolved);
      const d = info.d;
      if (status) {
        if (info.mode === "today") {
          status.hidden = true;
        } else {
          status.hidden = false;
          status.textContent =
            "今日內容尚未發布，暫顯示最近一日。預先排程的日期到當天會自動切換。";
        }
      }
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
        status.textContent = "今日內容準備中；預先排程後，到當天會自動顯示。";
      }
      if (body) body.hidden = true;
    }
  }

  function closeDailyGifModal() {
    const modal = document.getElementById("dailyGifModal");
    if (!modal) return;
    modal.hidden = true;
    document.body.style.overflow = "";
    const img = document.getElementById("dailyGifModalImg");
    if (img) img.removeAttribute("src");
  }

  function openDailyGifModal(day) {
    const modal = document.getElementById("dailyGifModal");
    if (!modal || !day) return;
    const title = document.getElementById("dailyGifModalTitle");
    const meta = document.getElementById("dailyGifModalMeta");
    const img = document.getElementById("dailyGifModalImg");
    const dl = document.getElementById("dailyGifModalDownload");
    const todayKey = taiwanDateKey();

    if (title) title.textContent = day.dateLine1 || day.date || day.dateKey || "每日吉祥";
    if (meta) {
      const theme =
        (day.fortuneLevel ? "今日" + day.fortuneLevel : "") +
        (day.mainTheme ? "｜" + day.mainTheme : "") +
        (day.secondaryTheme ? "・" + day.secondaryTheme : "");
      const summary = day.summaryText || "";
      meta.textContent =
        [theme, summary, String(day.dateKey) > todayKey ? "已排程，尚未到當天。" : ""]
          .filter(Boolean)
          .join("　");
    }
    if (img) {
      const src = day.gif || day.preview || "";
      img.src = src ? src + "?t=" + Date.now() : "";
      img.alt = (day.dateLine1 || day.dateKey || "") + " GIF";
    }
    bindDownloadButton(dl, day.gif || day.preview || "", day.dateKey);
    modal.hidden = false;
    document.body.style.overflow = "hidden";
  }

  function ensureGifModalHandlers() {
    const modal = document.getElementById("dailyGifModal");
    if (!modal || modal.dataset.bound === "1") return;
    modal.dataset.bound = "1";
    const closeIds = ["dailyGifModalBackdrop", "dailyGifModalClose", "dailyGifModalDismiss"];
    closeIds.forEach(function (id) {
      const el = document.getElementById(id);
      if (el) el.addEventListener("click", closeDailyGifModal);
    });
    document.addEventListener("keydown", function (ev) {
      if (ev.key === "Escape" && !modal.hidden) closeDailyGifModal();
    });
  }

  function renderHomeCalendar(year, month, byKey, todayKey) {
    const grid = document.getElementById("homeCalGrid");
    const title = document.getElementById("homeCalTitle");
    if (!grid) return;

    if (title) title.textContent = year + "年" + month + "月";

    const first = new Date(year, month - 1, 1);
    const startPad = first.getDay(); // 0 Sun
    const daysInMonth = new Date(year, month, 0).getDate();
    const cells = [];

    for (let i = 0; i < startPad; i++) {
      cells.push('<button type="button" class="home-cal-cell is-muted" tabindex="-1" disabled></button>');
    }
    for (let day = 1; day <= daysInMonth; day++) {
      const key =
        String(year) +
        String(month).padStart(2, "0") +
        String(day).padStart(2, "0");
      const hit = byKey[key];
      const isToday = key === todayKey;
      const classes = ["home-cal-cell"];
      if (hit) classes.push("has-gif");
      if (isToday) classes.push("is-today");
      cells.push(
        '<button type="button" class="' +
          classes.join(" ") +
          '" data-date-key="' +
          key +
          '"' +
          (hit ? "" : " disabled") +
          ' aria-label="' +
          year +
          "年" +
          month +
          "月" +
          day +
          "日" +
          (hit ? "，有剪紙可下載" : "") +
          '">' +
          day +
          "</button>"
      );
    }
    grid.innerHTML = cells.join("");

    grid.querySelectorAll(".home-cal-cell.has-gif").forEach(function (btn) {
      btn.addEventListener("click", function () {
        const day = byKey[btn.getAttribute("data-date-key")];
        if (day) openDailyGifModal(day);
      });
    });
  }

  async function mountHomeAlbum() {
    const root = document.getElementById("homeAlbum");
    if (!root) return;
    const status = document.getElementById("homeAlbumStatus");
    const body = document.getElementById("homeAlbumBody");
    const todayKey = taiwanDateKey();
    ensureGifModalHandlers();

    try {
      const archive = await fetchDailyArchive();
      const days = archive.days || [];
      if (!days.length) throw new Error("empty archive");
      if (status) status.hidden = true;
      if (body) body.hidden = false;

      const byKey = {};
      days.forEach(function (day) {
        if (day && day.dateKey) byKey[String(day.dateKey)] = day;
      });

      let viewYear = parseInt(todayKey.slice(0, 4), 10);
      let viewMonth = parseInt(todayKey.slice(4, 6), 10);
      // Prefer month of newest available pack if today has none yet
      if (!byKey[todayKey] && days[0] && String(days[0].dateKey).length === 8) {
        const k = String(days[0].dateKey);
        viewYear = parseInt(k.slice(0, 4), 10);
        viewMonth = parseInt(k.slice(4, 6), 10);
      }

      function paint() {
        renderHomeCalendar(viewYear, viewMonth, byKey, todayKey);
      }
      paint();

      const prev = document.getElementById("homeCalPrev");
      const next = document.getElementById("homeCalNext");
      if (prev) {
        prev.onclick = function () {
          viewMonth -= 1;
          if (viewMonth < 1) {
            viewMonth = 12;
            viewYear -= 1;
          }
          paint();
        };
      }
      if (next) {
        next.onclick = function () {
          viewMonth += 1;
          if (viewMonth > 12) {
            viewMonth = 1;
            viewYear += 1;
          }
          paint();
        };
      }
    } catch (e) {
      if (status) {
        status.hidden = false;
        status.textContent = "日曆準備中，發布每日內容後即可挑選。";
      }
      if (body) body.hidden = true;
    }
  }

  global.MingxuDaily = {
    taiwanDateKey: taiwanDateKey,
    resolveDisplayDaily: resolveDisplayDaily,
    fetchDailyLatest: fetchDailyLatest,
    fetchDailyArchive: fetchDailyArchive,
    downloadGif: downloadGif,
    mountDailyPage: mountDailyPage,
    mountHomeDaily: mountHomeDaily,
    mountHomeAlbum: mountHomeAlbum,
    mountDailyArchive: mountDailyArchive,
  };

  document.addEventListener("DOMContentLoaded", function () {
    if (document.body && document.body.dataset.page === "daily") {
      mountDailyPage();
    }
    if (document.getElementById("homeDaily")) {
      mountHomeDaily();
    }
    if (document.getElementById("homeAlbum")) {
      mountHomeAlbum();
    }
    if (document.getElementById("dailyArchiveList")) {
      mountDailyArchive();
    }
  });
})(window);
