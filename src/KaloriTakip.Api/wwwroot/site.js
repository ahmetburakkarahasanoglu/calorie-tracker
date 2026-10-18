(() => {
  const dateFormatter = new Intl.DateTimeFormat("en-CA", {
    timeZone: "Europe/Istanbul",
    year: "numeric",
    month: "2-digit",
    day: "2-digit"
  });

  function normalizeDailyRoute() {
    const currentDay = today();
    const previousActiveDay = sessionStorage.getItem("kaloritakip:last-active-day");
    const currentUrl = new URL(window.location.href);
    if (previousActiveDay && previousActiveDay !== currentDay
        && currentUrl.searchParams.get("date") === previousActiveDay) {
      currentUrl.searchParams.delete("date");
      window.history.replaceState(null, "", `${currentUrl.pathname}${currentUrl.search}${currentUrl.hash}`);
    }
    sessionStorage.setItem("kaloritakip:last-active-day", currentDay);
  }

  function today() {
    const parts = dateFormatter.formatToParts(new Date());
    const value = (type) => parts.find((part) => part.type === type).value;
    return `${value("year")}-${value("month")}-${value("day")}`;
  }

  function selectedDate() {
    const fromUrl = new URLSearchParams(window.location.search).get("date");
    return /^\d{4}-\d{2}-\d{2}$/.test(fromUrl || "")
      ? fromUrl
      : today();
  }

  async function api(url, options = {}) {
    const headers = new Headers(options.headers || {});
    headers.set("Accept", "application/json");
    const response = await fetch(url, { ...options, headers });
    if (response.status === 401 && document.body.dataset.authRequired === "true") {
      window.location.replace("/");
      throw new Error("Oturumun sona erdi. Yeniden giriş yap.");
    }
    if (response.status === 204) return null;

    const contentType = response.headers.get("content-type") || "";
    const payload = contentType.includes("json") ? await response.json() : null;
    if (!response.ok) {
      throw new Error(payload?.detail || payload?.title || `İstek başarısız oldu (${response.status}).`);
    }
    return payload;
  }

  const formatCalories = (value) => value === null || value === undefined
    ? "Belirlenmedi"
    : `${new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 2 }).format(value)} kcal`;
  const formatDate = (value) => new Intl.DateTimeFormat("tr-TR", { dateStyle: "long" })
    .format(new Date(`${value}T12:00:00+03:00`));

  function selectDate(value) {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return;
    sessionStorage.setItem("kaloritakip:selected-date", value);
    const currentUrl = new URL(window.location.href);
    if (value === today()) currentUrl.searchParams.delete("date");
    else currentUrl.searchParams.set("date", value);
    window.history.replaceState(null, "", `${currentUrl.pathname}${currentUrl.search}${currentUrl.hash}`);
    document.querySelectorAll("[data-selected-date-link]").forEach((link) => {
      const baseUrl = link.getAttribute("href").split("?")[0];
      link.href = value === today()
        ? baseUrl
        : `${baseUrl}?date=${encodeURIComponent(value)}`;
    });
  }

  normalizeDailyRoute();
  window.KaloriApp = { api, formatCalories, formatDate, selectedDate, selectDate, today };

  if (document.body.dataset.authRequired === "true") {
    selectDate(selectedDate());
  } else {
    const loginUrl = new URL(window.location.href);
    if (loginUrl.searchParams.has("date")) {
      loginUrl.searchParams.delete("date");
      window.history.replaceState(null, "", `${loginUrl.pathname}${loginUrl.search}${loginUrl.hash}`);
    }
  }

  if (document.body.dataset.authRequired === "true") {
    const errorTarget = document.querySelector("#global-error");
    api("/api/auth/me")
      .then((account) => {
        document.querySelectorAll("[data-username]").forEach((element) => {
          element.textContent = account.username;
        });
      })
      .catch((error) => {
        if (window.location.pathname === "/") return;
        if (errorTarget) {
          errorTarget.textContent = error.message;
          errorTarget.hidden = false;
        }
      });

    document.querySelectorAll("[data-logout]").forEach((button) => {
      button.addEventListener("click", async () => {
        button.disabled = true;
        button.textContent = "Çıkış yapılıyor…";
        const target = document.querySelector("#global-error");
        try {
          await api("/api/auth/logout", { method: "POST" });
          sessionStorage.clear();
          window.location.replace("/");
        } catch (error) {
          if (target) {
            target.textContent = error.message;
            target.hidden = false;
          }
          button.disabled = false;
          button.textContent = "Çıkış yap";
        }
      });
    });
  }
})();
