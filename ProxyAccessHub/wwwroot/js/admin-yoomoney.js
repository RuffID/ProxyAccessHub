let antiforgeryToken;
let pageRoot;
let feedbackContainer;
let receiverInput;
let notificationSecretInput;
let successUrlInput;
let clientIdInput;
let clientSecretInput;
let redirectUriInput;
let accessTokenInput;
let accessTokenExpiresAtInput;
let statusBadge;
let connectButton;

document.addEventListener("DOMContentLoaded", () => {
    initAdminYooMoneyPage();
});

async function initAdminYooMoneyPage() {
    antiforgeryToken = getRequestVerificationToken();
    pageRoot = document.getElementById("adminYooMoneyPage");

    buildPageShell();
    showOAuthFeedbackFromQuery();
    await loadPageData();
}

async function loadPageData() {
    clearFeedback();

    try {
        const pageData = await sendJsonRequest("/Admin/YooMoney?handler=Data", "GET", buildJsonHeaders(antiforgeryToken));
        applyPageData(pageData);
    }
    catch (error) {
        showFeedback(error.message, "danger");
    }
}

function buildPageShell() {
    const wrapper = createElement("section", ["d-flex", "flex-column", "gap-4"]);
    wrapper.append(
        buildHeader(),
        buildFeedbackContainer(),
        buildSettingsCard()
    );

    pageRoot.replaceChildren(wrapper);
}

function buildHeader() {
    const header = createElement("section", ["d-flex", "flex-column", "gap-3"]);
    const badge = createElement("span", ["badge", "text-bg-dark", "align-self-start", "px-3", "py-2", "rounded-pill"]);
    badge.textContent = "Административная панель";

    const title = createElement("h1", ["h2", "mb-0"]);
    title.textContent = "ЮMoney";

    statusBadge = createElement("span", ["badge", "align-self-start", "px-3", "py-2", "rounded-pill", "text-bg-secondary"]);
    statusBadge.textContent = "Статус неизвестен";

    header.append(badge, title, statusBadge);
    return header;
}

function buildFeedbackContainer() {
    feedbackContainer = createElement("div", ["d-none"]);
    return feedbackContainer;
}

function buildSettingsCard() {
    const section = createElement("section", ["card", "border-0", "shadow-sm"]);
    const body = createElement("div", ["card-body", "p-4"]);
    const title = createElement("h2", ["h5", "mb-4"]);
    title.textContent = "Настройки интеграции";

    receiverInput = createInput("yooMoneyReceiver", "yooMoneyReceiver", "text");
    notificationSecretInput = createInput("yooMoneyNotificationSecret", "yooMoneyNotificationSecret", "text");
    successUrlInput = createInput("yooMoneySuccessUrl", "yooMoneySuccessUrl", "text");
    clientIdInput = createInput("yooMoneyClientId", "yooMoneyClientId", "text");
    clientSecretInput = createInput("yooMoneyClientSecret", "yooMoneyClientSecret", "text");
    redirectUriInput = createInput("yooMoneyRedirectUri", "yooMoneyRedirectUri", "text");
    accessTokenInput = createTextarea("yooMoneyAccessToken", "yooMoneyAccessToken");
    accessTokenExpiresAtInput = createInput("yooMoneyAccessTokenExpiresAtUtc", "yooMoneyAccessTokenExpiresAtUtc", "text");

    const row = createElement("div", ["row", "g-3"]);
    row.append(
        createColumn(createField("Номер кошелька", receiverInput), ["col-12", "col-xl-4"]),
        createColumn(createField("Секрет уведомлений", notificationSecretInput), ["col-12", "col-xl-4"]),
        createColumn(createField("SuccessUrl", successUrlInput), ["col-12", "col-xl-4"]),
        createColumn(createField("ClientId", clientIdInput), ["col-12", "col-xl-4"]),
        createColumn(createField("ClientSecret", clientSecretInput), ["col-12", "col-xl-4"]),
        createColumn(createField("RedirectUri", redirectUriInput), ["col-12", "col-xl-4"]),
        createColumn(createField("AccessToken", accessTokenInput), ["col-12"]),
        createColumn(createField("AccessTokenExpiresAtUtc", accessTokenExpiresAtInput), ["col-12"])
    );

    const note = createElement("div", ["alert", "alert-secondary", "mb-0"]);
    note.textContent = "RedirectUri должен совпадать с адресом callback, зарегистрированным в приложении ЮMoney. Для этого проекта подойдёт URL вида https://ваш-домен/Admin/YooMoney?handler=Callback";

    const actions = createElement("div", ["d-flex", "flex-wrap", "gap-2", "mt-4"]);
    const saveButton = createElement("button", ["btn", "btn-dark"]);
    saveButton.type = "button";
    saveButton.textContent = "Сохранить настройки";
    saveButton.addEventListener("click", async () => {
        await saveSettings();
    });

    connectButton = createElement("button", ["btn", "btn-outline-dark"]);
    connectButton.type = "button";
    connectButton.textContent = "Подключить YooMoney";
    connectButton.addEventListener("click", () => {
        window.location.href = "/Admin/YooMoney?handler=Connect";
    });

    const reloadButton = createElement("button", ["btn", "btn-outline-secondary"]);
    reloadButton.type = "button";
    reloadButton.textContent = "Обновить";
    reloadButton.addEventListener("click", async () => {
        await loadPageData();
    });

    actions.append(saveButton, connectButton, reloadButton);
    body.append(title, row, note, actions);
    section.append(body);
    return section;
}

function applyPageData(pageData) {
    receiverInput.value = pageData.receiver ?? "";
    notificationSecretInput.value = pageData.notificationSecret ?? "";
    successUrlInput.value = pageData.successUrl ?? "";
    clientIdInput.value = pageData.clientId ?? "";
    clientSecretInput.value = pageData.clientSecret ?? "";
    redirectUriInput.value = pageData.redirectUri ?? "";
    accessTokenInput.value = pageData.accessToken ?? "";
    accessTokenExpiresAtInput.value = pageData.accessTokenExpiresAtUtc ?? "";
    statusBadge.textContent = pageData.statusName ?? "Статус неизвестен";
    statusBadge.className = `badge align-self-start px-3 py-2 rounded-pill ${resolveStatusClass(pageData)}`;
    connectButton.textContent = pageData.isConnected ? "Переподключить YooMoney" : "Подключить YooMoney";
}

function resolveStatusClass(pageData) {
    if (pageData.isConnected) {
        return "text-bg-success";
    }

    if (pageData.isAccessTokenExpired) {
        return "text-bg-danger";
    }

    return "text-bg-secondary";
}

async function saveSettings() {
    try {
        const url = `/Admin/YooMoney?handler=Save&receiver=${encodeURIComponent(receiverInput.value)}&notificationSecret=${encodeURIComponent(notificationSecretInput.value)}&successUrl=${encodeURIComponent(successUrlInput.value)}&clientId=${encodeURIComponent(clientIdInput.value)}&clientSecret=${encodeURIComponent(clientSecretInput.value)}&redirectUri=${encodeURIComponent(redirectUriInput.value)}&accessToken=${encodeURIComponent(accessTokenInput.value)}&accessTokenExpiresAtUtc=${encodeURIComponent(accessTokenExpiresAtInput.value)}`;
        const response = await sendJsonRequest(url, "POST", buildJsonHeaders(antiforgeryToken));
        unwrapOrThrow(response, "Не удалось сохранить настройки ЮMoney.");

        showFeedback("Настройки ЮMoney сохранены.", "success");
        await loadPageData();
    }
    catch (error) {
        showFeedback(error.message, "danger");
    }
}

function showOAuthFeedbackFromQuery() {
    const searchParams = new URLSearchParams(window.location.search);
    const oauthStatus = searchParams.get("oauthStatus");
    const oauthMessage = searchParams.get("oauthMessage");

    if (!oauthStatus || !oauthMessage) {
        return;
    }

    showFeedback(oauthMessage, oauthStatus === "success" ? "success" : "danger");
    const nextUrl = `${window.location.pathname}`;
    window.history.replaceState({}, document.title, nextUrl);
}

function createField(labelText, field) {
    const wrapper = createElement("div", ["d-flex", "flex-column", "gap-2"]);
    const label = createElement("label", ["form-label", "mb-0"]);
    label.htmlFor = field.id;
    label.textContent = labelText;
    wrapper.append(label, field);
    return wrapper;
}

function createColumn(content, classes) {
    const column = createElement("div", classes);
    column.append(content);
    return column;
}

function createInput(id, name, type) {
    const input = createElement("input", ["form-control"]);
    input.id = id;
    input.name = name;
    input.type = type;
    return input;
}

function createTextarea(id, name) {
    const textarea = createElement("textarea", ["form-control"]);
    textarea.id = id;
    textarea.name = name;
    textarea.rows = 4;
    return textarea;
}

function createElement(tagName, classes = []) {
    const element = document.createElement(tagName);

    if (classes.length > 0) {
        element.classList.add(...classes);
    }

    return element;
}

function showFeedback(message, type) {
    feedbackContainer.className = `alert alert-${type}`;
    feedbackContainer.textContent = message;
}

function clearFeedback() {
    feedbackContainer.className = "d-none";
    feedbackContainer.textContent = "";
}

async function sendJsonRequest(url, method = "GET", headers = {}, body = null) {
    const options = {
        method: method,
        headers: headers
    };

    if (body !== null) {
        if (typeof body === "object" && !(body instanceof FormData)) {
            options.body = JSON.stringify(body);

            if (!options.headers["Content-Type"]) {
                options.headers["Content-Type"] = "application/json";
            }
        }
        else {
            options.body = body;
        }
    }

    const response = await fetch(url, options);

    if (!response.ok) {
        const errorText = await response.text();
        let message;

        try {
            const parsed = JSON.parse(errorText);
            message = parsed && parsed.message ? parsed.message : errorText;
        }
        catch {
            message = errorText || `HTTP error ${response.status}`;
        }

        throw new Error(message);
    }

    const text = await response.text();
    if (!text || text.trim() === "") {
        return {};
    }

    return JSON.parse(text);
}

function buildJsonHeaders(requestToken) {
    const headers = {
        "Accept": "application/json",
        "Content-Type": "application/json"
    };

    if (requestToken) {
        headers["RequestVerificationToken"] = requestToken;
    }

    return headers;
}

function getRequestVerificationToken() {
    const element = document.querySelector('meta[name="request-verification-token"]');
    return element ? element.getAttribute("content") : null;
}

function unwrapServiceResult(resp) {
    if (resp && typeof resp === "object" && ("success" in resp)) {
        const success = resp.success === true;
        const data = resp.data;
        const message = resp.message ?? "";
        return { success, data, message, raw: resp };
    }

    return { success: true, data: resp, message: "", raw: resp };
}

function unwrapOrThrow(resp, defaultMessage) {
    const result = unwrapServiceResult(resp);
    if (!result.success) {
        throw new Error(result.message || defaultMessage || "Ошибка операции.");
    }

    return result.data;
}
