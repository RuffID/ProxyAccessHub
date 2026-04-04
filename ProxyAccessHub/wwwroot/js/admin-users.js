let antiforgeryToken;
let pageRoot;
let feedbackContainer;
let filtersContainer;
let tableSectionContainer;
let tableBody;
let emptyStateContainer;
let searchInput;
let serverSelect;
let state = {
    pageData: null,
    filteredUsers: []
};

document.addEventListener("DOMContentLoaded", () => {
    initAdminUsersPage();
});

async function initAdminUsersPage() {
    antiforgeryToken = getRequestVerificationToken();
    pageRoot = document.getElementById("adminUsersPage");

    buildPageShell();
    await loadPageData();
}

async function loadPageData() {
    setLoadingState(true);
    clearFeedback();

    try {
        state.pageData = await sendJsonRequest(
            "/Admin/Users?handler=Data",
            "GET",
            buildJsonHeaders(antiforgeryToken)
        );

        populateServerFilter();
        applyFilters();
    }
    catch (error) {
        showFeedback(error.message, "danger");
        renderEmptyState("Не удалось загрузить список пользователей.");
    }
    finally {
        setLoadingState(false);
    }
}

function buildPageShell() {
    const wrapper = createElement("section", ["d-flex", "flex-column", "gap-4"]);
    wrapper.append(
        buildHeader(),
        buildFeedbackContainer(),
        buildFiltersCard(),
        buildTableCard()
    );

    pageRoot.replaceChildren(wrapper);
}

function buildHeader() {
    const header = createElement("section", ["d-flex", "flex-column", "gap-2"]);
    const badge = createElement("span", ["badge", "text-bg-dark", "align-self-start", "px-3", "py-2", "rounded-pill"]);
    badge.textContent = "Административная панель";

    const title = createElement("h1", ["h2", "mb-0"]);
    title.textContent = "Пользователи";

    header.append(badge, title);
    return header;
}

function buildFeedbackContainer() {
    feedbackContainer = createElement("div", ["d-none"]);
    return feedbackContainer;
}

function buildFiltersCard() {
    const section = createElement("section", ["card", "border-0", "shadow-sm"]);
    const body = createElement("div", ["card-body", "p-4"]);

    const row = createElement("div", ["row", "g-3", "align-items-end"]);
    const searchColumn = createElement("div", ["col-12", "col-lg-6"]);
    const serverColumn = createElement("div", ["col-12", "col-lg-4"]);
    const infoColumn = createElement("div", ["col-12", "col-lg-2"]);

    searchInput = createInputField(
        "usersSearchInput",
        "usersSearchInput",
        "Поиск по telemt userId",
        "Начните вводить telemt userId"
    );
    searchInput.addEventListener("input", () => {
        applyFilters();
    });

    serverSelect = createSelectField("usersServerFilter", "usersServerFilter", "Сервер");
    serverSelect.addEventListener("change", () => {
        applyFilters();
    });

    const infoBlock = createElement("div", ["small", "text-body-secondary"]);
    infoBlock.textContent = "Цена меняется при потере фокуса поля.";

    searchColumn.append(createFieldWrapper("Поиск по telemt userId", searchInput));
    serverColumn.append(createFieldWrapper("Сервер", serverSelect));
    infoColumn.append(infoBlock);

    row.append(searchColumn, serverColumn, infoColumn);
    body.append(row);
    section.append(body);

    filtersContainer = section;
    return section;
}

function buildTableCard() {
    const section = createElement("section", ["card", "border-0", "shadow-sm"]);
    const body = createElement("div", ["card-body", "p-0"]);
    const tableResponsive = createElement("div", ["table-responsive"]);
    const table = createElement("table", ["table", "table-hover", "align-middle", "mb-0"]);
    const thead = createElement("thead", ["table-light"]);
    const headRow = createElement("tr");
    const headings = [
        "Telemt userId",
        "Сервер",
        "Тариф",
        "Баланс",
        "Оплачено до",
        "Ручная обработка"
    ];

    for (const heading of headings) {
        const th = createElement("th", ["text-nowrap", "px-3", "py-3"]);
        th.scope = "col";
        th.textContent = heading;
        headRow.append(th);
    }

    thead.append(headRow);
    tableBody = createElement("tbody");
    table.append(thead, tableBody);
    tableResponsive.append(table);

    emptyStateContainer = createElement("div", ["p-4", "d-none"]);
    body.append(tableResponsive, emptyStateContainer);
    section.append(body);
    tableSectionContainer = section;
    return section;
}

function populateServerFilter() {
    const optionNodes = [];
    const defaultOption = createElement("option");
    defaultOption.value = "";
    defaultOption.textContent = "Все серверы";
    optionNodes.push(defaultOption);

    const serverNames = Array.from(new Set((state.pageData?.users ?? []).map(user => user.serverName)))
        .sort((left, right) => left.localeCompare(right, "ru"));

    for (const serverName of serverNames) {
        const option = createElement("option");
        option.value = serverName;
        option.textContent = serverName;
        optionNodes.push(option);
    }

    serverSelect.replaceChildren(...optionNodes);
}

function applyFilters() {
    if (!state.pageData) {
        return;
    }

    const searchValue = searchInput.value.trim().toLowerCase();
    const serverValue = serverSelect.value.trim();

    state.filteredUsers = state.pageData.users.filter(user => {
        const telemtMatches = searchValue.length === 0
            || user.telemtUserId.toLowerCase().includes(searchValue);
        const serverMatches = serverValue.length === 0
            || user.serverName === serverValue;

        return telemtMatches && serverMatches;
    });

    renderUsersTable();
}

function renderUsersTable() {
    if (state.filteredUsers.length === 0) {
        tableBody.replaceChildren();
        renderEmptyState("По выбранным фильтрам пользователи не найдены.");
        return;
    }

    emptyStateContainer.classList.add("d-none");
    tableSectionContainer.classList.remove("opacity-50");

    const rows = state.filteredUsers.map(user => buildUserRow(user));
    tableBody.replaceChildren(...rows);
}

function buildUserRow(user) {
    const row = createElement("tr");

    row.append(
        createTextCell(user.telemtUserId, ["fw-semibold", "text-nowrap"]),
        createTextCell(user.serverName, ["text-nowrap"]),
        buildTariffCell(user),
        createTextCell(formatMoney(user.balanceRub)),
        createTextCell(formatDateTime(user.accessPaidToUtc, "Не оплачено"), ["text-nowrap"]),
        buildManualHandlingCell(user)
    );

    return row;
}

function buildTariffCell(user) {
    const cell = createElement("td", ["px-3", "py-3"]);
    const wrapper = createElement("div", ["d-flex", "flex-column", "gap-2"]);
    const tariffSelect = createTariffSelect(user);
    const inputGroup = createElement("div", ["input-group", "input-group-sm"]);
    const input = createElement("input", ["form-control"]);
    const suffix = createElement("span", ["input-group-text"]);
    const note = createElement("div", ["small", "text-body-secondary"]);

    input.type = "number";
    input.min = "0.01";
    input.max = "1000";
    input.step = "0.01";
    input.value = formatDecimalValue(user.effectivePeriodPriceRub);
    input.id = `tariffPrice_${user.userId}`;
    input.name = `tariffPrice_${user.userId}`;
    input.dataset.userId = user.userId;
    input.dataset.initialValue = formatDecimalValue(user.effectivePeriodPriceRub);
    input.disabled = user.tariffRequiresRenewal !== true;

    input.addEventListener("focus", () => {
        input.dataset.previousValue = input.value;
    });

    input.addEventListener("blur", async () => {
        await handleTariffPriceBlur(user, input);
    });

    tariffSelect.addEventListener("change", async () => {
        await handleTariffChange(user, tariffSelect, input);
    });

    suffix.textContent = "₽";

    updateTariffPriceNote(user, note);

    inputGroup.append(input, suffix);
    wrapper.append(tariffSelect, inputGroup, note);
    cell.append(wrapper);
    return cell;
}

function createTariffSelect(user) {
    const select = createElement("select", ["form-select", "form-select-sm"]);
    select.id = `tariffSelect_${user.userId}`;
    select.name = `tariffSelect_${user.userId}`;

    const options = (state.pageData?.tariffs ?? []).map(tariff => {
        const option = createElement("option");
        option.value = tariff.id;
        option.textContent = tariff.name;
        return option;
    });

    select.replaceChildren(...options);
    select.value = user.tariffId;
    return select;
}

function buildManualHandlingCell(user) {
    const cell = createElement("td", ["px-3", "py-3"]);
    const wrapper = createElement("div", ["d-flex", "flex-column", "gap-2"]);
    const badge = createElement("span", ["fw-semibold"]);
    const reason = createElement("div", ["small", "text-body-secondary"]);

    badge.textContent = user.manualHandlingStatusName;

    if (user.requiresManualHandling) {
        badge.classList.add("text-danger");
    }
    else {
        badge.classList.add("text-body");
    }

    if (user.manualHandlingReason) {
        reason.textContent = user.manualHandlingReason;
        wrapper.append(badge, reason);
    }
    else {
        wrapper.append(badge);
    }

    cell.append(wrapper);
    return cell;
}

async function handleTariffPriceBlur(user, input) {
    if (input.disabled) {
        return;
    }

    const normalizedValue = normalizePriceValue(input.value);
    const previousValue = input.dataset.previousValue ?? input.dataset.initialValue ?? formatDecimalValue(user.effectivePeriodPriceRub);

    if (normalizedValue === previousValue) {
        input.value = previousValue;
        return;
    }

    if (!isValidTariffPrice(normalizedValue)) {
        input.value = previousValue;
        showFeedback("Цена должна быть больше 0 и не превышать 1000 руб.", "danger");
        return;
    }

    input.disabled = true;
    clearFeedback();

    try {
        const requestUrl = `/Admin/Users?handler=UpdateTariffPrice&userId=${encodeURIComponent(user.userId)}&priceRub=${encodeURIComponent(normalizedValue)}`;
        const response = await sendJsonRequest(
            requestUrl,
            "POST",
            buildJsonHeaders(antiforgeryToken)
        );

        unwrapOrThrow(response, "Не удалось обновить цену тарифа.");

        user.effectivePeriodPriceRub = Number.parseFloat(normalizedValue);
        user.customPeriodPriceRub = Number.parseFloat(normalizedValue);
        user.discountPercent = null;

        input.value = formatDecimalValue(user.effectivePeriodPriceRub);
        input.dataset.initialValue = input.value;
        input.dataset.previousValue = input.value;

        showFeedback(`Цена для пользователя ${user.telemtUserId} обновлена.`, "success");
    }
    catch (error) {
        input.value = previousValue;
        showFeedback(error.message, "danger");
    }
    finally {
        input.disabled = false;
    }
}

async function handleTariffChange(user, select, priceInput) {
    const previousTariffId = user.tariffId;
    const nextTariffId = select.value;

    if (!nextTariffId || nextTariffId === previousTariffId) {
        select.value = previousTariffId;
        return;
    }

    select.disabled = true;
    priceInput.disabled = true;
    clearFeedback();

    try {
        const requestUrl = `/Admin/Users?handler=UpdateTariff&userId=${encodeURIComponent(user.userId)}&tariffId=${encodeURIComponent(nextTariffId)}`;
        const response = await sendJsonRequest(
            requestUrl,
            "POST",
            buildJsonHeaders(antiforgeryToken)
        );

        unwrapOrThrow(response, "Не удалось обновить тариф пользователя.");

        await loadPageData();
        showFeedback(`Тариф для пользователя ${user.telemtUserId} обновлён.`, "success");
    }
    catch (error) {
        select.value = previousTariffId;
        showFeedback(error.message, "danger");
    }
    finally {
        select.disabled = false;
        priceInput.disabled = user.tariffRequiresRenewal !== true;
    }
}

function updateTariffPriceNote(user, note) {
    if (user.tariffRequiresRenewal) {
        note.textContent = `Кастомная цена для тарифа: ${user.tariffName}`;
        return;
    }

    note.textContent = "Для этого тарифа индивидуальная цена недоступна.";
}

function renderEmptyState(message) {
    const title = createElement("div", ["fw-semibold", "mb-1"]);
    const text = createElement("div", ["text-body-secondary"]);

    title.textContent = "Нет данных для отображения";
    text.textContent = message;

    emptyStateContainer.replaceChildren(title, text);
    emptyStateContainer.classList.remove("d-none");
}

function showFeedback(message, type) {
    if (!message) {
        clearFeedback();
        return;
    }

    const alert = createElement("div", ["alert", `alert-${type}`, "mb-0"]);
    alert.role = "alert";
    alert.textContent = message;

    feedbackContainer.className = "";
    feedbackContainer.replaceChildren(alert);
}

function clearFeedback() {
    feedbackContainer.className = "d-none";
    feedbackContainer.replaceChildren();
}

function setLoadingState(isLoading) {
    if (!tableSectionContainer) {
        return;
    }

    if (isLoading) {
        tableSectionContainer.classList.add("opacity-50");
        renderEmptyState("Загрузка данных...");
        return;
    }

    tableSectionContainer.classList.remove("opacity-50");
}

function createFieldWrapper(labelText, field) {
    const wrapper = createElement("div", ["d-flex", "flex-column", "gap-2"]);
    const label = createElement("label", ["form-label", "mb-0"]);
    label.htmlFor = field.id;
    label.textContent = labelText;

    wrapper.append(label, field);
    return wrapper;
}

function createInputField(id, name, labelText, placeholder) {
    const input = createElement("input", ["form-control"]);
    input.type = "text";
    input.id = id;
    input.name = name;
    input.placeholder = placeholder;
    input.autocomplete = "off";
    input.setAttribute("aria-label", labelText);
    return input;
}

function createSelectField(id, name, labelText) {
    const select = createElement("select", ["form-select"]);
    select.id = id;
    select.name = name;
    select.setAttribute("aria-label", labelText);
    return select;
}

function createTextCell(text, classes = []) {
    const cell = createElement("td", ["px-3", "py-3", ...classes]);
    cell.textContent = text;
    return cell;
}

function createElement(tagName, classes = []) {
    const element = document.createElement(tagName);

    if (classes.length > 0) {
        element.classList.add(...classes);
    }

    return element;
}

function normalizePriceValue(value) {
    const parsed = Number.parseFloat(String(value).replace(",", "."));

    if (Number.isNaN(parsed)) {
        return "";
    }

    return parsed.toFixed(2);
}

function isValidTariffPrice(value) {
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) && parsed > 0 && parsed <= 1000;
}

function formatMoney(value) {
    return `${formatDecimalValue(value)} ₽`;
}

function formatDecimalValue(value) {
    return Number(value).toFixed(2);
}

function formatDateTime(value, fallback) {
    if (!value) {
        return fallback;
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return fallback;
    }

    return new Intl.DateTimeFormat("ru-RU", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit"
    }).format(date);
}

async function sendJsonRequest(url, method = "GET", headers = {}, body = null) {
    const options = {
        method,
        headers
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
        Accept: "application/json",
        "Content-Type": "application/json"
    };

    if (requestToken) {
        headers.RequestVerificationToken = requestToken;
    }

    return headers;
}

function getRequestVerificationToken() {
    const element = document.querySelector('meta[name="request-verification-token"]');
    return element ? element.getAttribute("content") : null;
}

function unwrapServiceResult(response) {
    if (response && typeof response === "object" && "success" in response) {
        return {
            success: response.success === true,
            data: response.data,
            message: response.message ?? "",
            raw: response
        };
    }

    return {
        success: true,
        data: response,
        message: "",
        raw: response
    };
}

function unwrapOrThrow(response, defaultMessage) {
    const result = unwrapServiceResult(response);

    if (!result.success) {
        throw new Error(result.message || defaultMessage || "Ошибка операции.");
    }

    return result.data;
}
