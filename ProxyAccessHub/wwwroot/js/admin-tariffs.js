let antiforgeryToken;
let tariffsPageRoot;
let tariffsFeedbackContainer;
let tariffsTableBody;
let tariffsEmptyStateContainer;
const TWO_WEEKS_PERIOD_MONTHS = "-14";
const WEEKLY_PERIOD_MONTHS = "-7";
const MONTHLY_PERIOD_MONTHS = "1";
const HALF_YEAR_PERIOD_MONTHS = "6";
const YEARLY_PERIOD_MONTHS = "12";
const UNLIMITED_PERIOD_MONTHS = "0";

document.addEventListener("DOMContentLoaded", () => {
    initAdminTariffsPage();
});

async function initAdminTariffsPage() {
    antiforgeryToken = getRequestVerificationToken();
    tariffsPageRoot = document.getElementById("adminTariffsPage");

    buildTariffsPageShell();
    await loadTariffs();
}

async function loadTariffs() {
    clearFeedback();

    try {
        const pageData = await sendJsonRequest("/Admin/Tariffs?handler=Data", "GET", buildJsonHeaders(antiforgeryToken));
        renderTariffs(pageData.tariffs ?? []);
    }
    catch (error) {
        showFeedback(error.message, "danger");
        renderTariffs([]);
    }
}

function buildTariffsPageShell() {
    const wrapper = createElement("section", ["d-flex", "flex-column", "gap-4"]);
    wrapper.append(
        buildPageHeader("Тарифы"),
        buildFeedbackContainer(),
        buildCreateCard(),
        buildTableCard()
    );

    tariffsPageRoot.replaceChildren(wrapper);
}

function buildCreateCard() {
    const section = createElement("section", ["card", "border-0", "shadow-sm"]);
    const body = createElement("div", ["card-body", "p-4"]);
    const titleRow = createElement("div", ["d-flex", "flex-column", "flex-md-row", "justify-content-md-between", "align-items-md-center", "gap-3", "mb-3"]);
    const title = createElement("h2", ["h5", "mb-0"]);
    title.textContent = "Новый тариф";
    const createButton = createElement("button", ["btn", "btn-dark", "align-self-start", "align-self-md-center"]);
    createButton.type = "button";
    createButton.textContent = "Добавить тариф";

    const row = createElement("div", ["row", "g-3", "align-items-end"]);
    const nameInput = createInput("tariffCreateName", "tariffCreateName", "text");
    const priceInput = createInput("tariffCreatePrice", "tariffCreatePrice", "number", "0.00");
    priceInput.min = "0";
    priceInput.step = "0.01";
    const periodSelect = createPeriodSelect("tariffCreatePeriodMonths", "tariffCreatePeriodMonths", MONTHLY_PERIOD_MONTHS);
    const activeInput = createCheckbox("tariffCreateIsActive", "tariffCreateIsActive", true);
    const defaultInput = createCheckbox("tariffCreateIsDefault", "tariffCreateIsDefault", false);
    createButton.addEventListener("click", async () => {
        await createTariff(nameInput, priceInput, periodSelect, activeInput, defaultInput);
    });

    row.append(
        createColumn(createField("Название", nameInput), ["col-12", "col-lg-3"]),
        createColumn(createField("Стоимость, ₽", priceInput), ["col-12", "col-lg-2"]),
        createColumn(createField("Срок", periodSelect), ["col-12", "col-lg-2"]),
        createColumn(createCheckboxField("Активен", activeInput), ["col-12", "col-lg-2"]),
        createColumn(createCheckboxField("По умолчанию", defaultInput), ["col-12", "col-lg-3"])
    );

    bindPeriodPriceState(periodSelect, priceInput);
    titleRow.append(title, createButton);
    body.append(titleRow, row);
    section.append(body);
    return section;
}

function buildTableCard() {
    const section = createElement("section", ["card", "border-0", "shadow-sm"]);
    const body = createElement("div", ["card-body", "p-0"]);
    const tableResponsive = createElement("div", ["table-responsive"]);
    const table = createElement("table", ["table", "table-hover", "align-middle", "mb-0"]);
    const thead = createElement("thead", ["table-light"]);
    const headRow = createElement("tr");

    for (const heading of ["Название", "Стоимость", "Срок", "Активен", "По умолчанию", "Действия"]) {
        const th = createElement("th", ["px-3", "py-3", "text-nowrap"]);
        th.scope = "col";
        th.textContent = heading;
        headRow.append(th);
    }

    thead.append(headRow);
    tariffsTableBody = createElement("tbody");
    table.append(thead, tariffsTableBody);
    tableResponsive.append(table);

    tariffsEmptyStateContainer = createElement("div", ["p-4", "d-none"]);
    body.append(tableResponsive, tariffsEmptyStateContainer);
    section.append(body);
    return section;
}

function renderTariffs(tariffs) {
    if (tariffs.length === 0) {
        tariffsTableBody.replaceChildren();
        renderEmptyState("Список тарифов пока пуст.");
        return;
    }

    tariffsEmptyStateContainer.classList.add("d-none");
    const rows = tariffs.map(tariff => buildTariffRow(tariff));
    tariffsTableBody.replaceChildren(...rows);
}

function buildTariffRow(tariff) {
    const row = createElement("tr");
    const nameInput = createInput(`tariffName_${tariff.id}`, `tariffName_${tariff.id}`, "text", tariff.name);
    const priceInput = createInput(`tariffPrice_${tariff.id}`, `tariffPrice_${tariff.id}`, "number", Number(tariff.periodPriceRub).toFixed(2));
    priceInput.min = "0";
    priceInput.step = "0.01";
    const periodSelect = createPeriodSelect(`tariffPeriod_${tariff.id}`, `tariffPeriod_${tariff.id}`, String(tariff.periodMonths));
    const activeInput = createCheckbox(`tariffIsActive_${tariff.id}`, `tariffIsActive_${tariff.id}`, tariff.isActive);
    const defaultInput = createCheckbox(`tariffIsDefault_${tariff.id}`, `tariffIsDefault_${tariff.id}`, tariff.isDefault);
    const saveButton = createElement("button", ["btn", "btn-outline-dark", "btn-sm"]);
    saveButton.type = "button";
    saveButton.textContent = "Сохранить";
    saveButton.addEventListener("click", async () => {
        await updateTariff(tariff.id, nameInput, priceInput, periodSelect, activeInput, defaultInput);
    });

    row.append(
        createCell(nameInput),
        createCell(priceInput),
        createCell(periodSelect),
        createCell(createCheckboxField("Активен", activeInput)),
        createCell(createCheckboxField("По умолчанию", defaultInput)),
        createCell(saveButton)
    );

    bindPeriodPriceState(periodSelect, priceInput);
    return row;
}

async function createTariff(nameInput, priceInput, periodSelect, activeInput, defaultInput) {
    try {
        const url = `/Admin/Tariffs?handler=Create&name=${encodeURIComponent(nameInput.value)}&periodPriceRub=${encodeURIComponent(priceInput.value)}&periodMonths=${encodeURIComponent(periodSelect.value)}&isActive=${encodeURIComponent(String(activeInput.checked))}&isDefault=${encodeURIComponent(String(defaultInput.checked))}`;
        const response = await sendJsonRequest(url, "POST", buildJsonHeaders(antiforgeryToken));
        unwrapOrThrow(response, "Не удалось создать тариф.");

        nameInput.value = "";
        priceInput.value = "0.00";
        periodSelect.value = MONTHLY_PERIOD_MONTHS;
        updatePriceInputState(periodSelect, priceInput);
        activeInput.checked = true;
        defaultInput.checked = false;
        showFeedback("Тариф добавлен.", "success");
        await loadTariffs();
    }
    catch (error) {
        showFeedback(error.message, "danger");
    }
}

async function updateTariff(id, nameInput, priceInput, periodSelect, activeInput, defaultInput) {
    try {
        const url = `/Admin/Tariffs?handler=Update&id=${encodeURIComponent(id)}&name=${encodeURIComponent(nameInput.value)}&periodPriceRub=${encodeURIComponent(priceInput.value)}&periodMonths=${encodeURIComponent(periodSelect.value)}&isActive=${encodeURIComponent(String(activeInput.checked))}&isDefault=${encodeURIComponent(String(defaultInput.checked))}`;
        const response = await sendJsonRequest(url, "POST", buildJsonHeaders(antiforgeryToken));
        unwrapOrThrow(response, "Не удалось обновить тариф.");

        showFeedback("Тариф обновлён.", "success");
        await loadTariffs();
    }
    catch (error) {
        showFeedback(error.message, "danger");
    }
}

function createPeriodSelect(id, name, value) {
    const select = createElement("select", ["form-select", "form-select-sm"]);
    select.id = id;
    select.name = name;

    const twoWeeksOption = createElement("option");
    twoWeeksOption.value = TWO_WEEKS_PERIOD_MONTHS;
    twoWeeksOption.textContent = "Две недели";
    twoWeeksOption.textContent = "Две недели";

    twoWeeksOption.textContent = "\u0414\u0432\u0435 \u043d\u0435\u0434\u0435\u043b\u0438";

    const weeklyOption = createElement("option");
    weeklyOption.value = WEEKLY_PERIOD_MONTHS;
    weeklyOption.textContent = "Неделя";
    weeklyOption.textContent = "Неделя";

    weeklyOption.textContent = "\u041d\u0435\u0434\u0435\u043b\u044f";

    const monthlyOption = createElement("option");
    monthlyOption.value = MONTHLY_PERIOD_MONTHS;
    monthlyOption.textContent = "Месяц";
    monthlyOption.textContent = "Месяц";

    monthlyOption.textContent = "\u041c\u0435\u0441\u044f\u0446";

    const halfYearOption = createElement("option");
    halfYearOption.value = HALF_YEAR_PERIOD_MONTHS;
    halfYearOption.textContent = "Полгода";
    halfYearOption.textContent = "РџРѕР»РіРѕРґР°";

    halfYearOption.textContent = "\u041f\u043e\u043b\u0433\u043e\u0434\u0430";

    const yearlyOption = createElement("option");
    yearlyOption.value = YEARLY_PERIOD_MONTHS;
    yearlyOption.textContent = "Год";
    yearlyOption.textContent = "Год";

    yearlyOption.textContent = "\u0413\u043e\u0434";

    const unlimitedOption = createElement("option");
    unlimitedOption.value = UNLIMITED_PERIOD_MONTHS;
    unlimitedOption.textContent = "Навсегда";

    select.append(twoWeeksOption, weeklyOption, monthlyOption, halfYearOption, yearlyOption, unlimitedOption);
    select.append(weeklyOption, twoWeeksOption, monthlyOption, halfYearOption, yearlyOption, unlimitedOption);
    select.value = value;
    return select;
}

function bindPeriodPriceState(periodSelect, priceInput) {
    periodSelect.addEventListener("change", () => {
        updatePriceInputState(periodSelect, priceInput);
    });

    updatePriceInputState(periodSelect, priceInput);
}

function updatePriceInputState(periodSelect, priceInput) {
    const isUnlimitedPeriod = periodSelect.value === UNLIMITED_PERIOD_MONTHS;
    priceInput.disabled = isUnlimitedPeriod;

    if (isUnlimitedPeriod) {
        priceInput.value = "0.00";
    }
}

function buildPageHeader(titleText) {
    const header = createElement("section", ["d-flex", "flex-column", "gap-2"]);
    const badge = createElement("span", ["badge", "text-bg-dark", "align-self-start", "px-3", "py-2", "rounded-pill"]);
    badge.textContent = "Административная панель";
    const title = createElement("h1", ["h2", "mb-0"]);
    title.textContent = titleText;
    header.append(badge, title);
    return header;
}

function buildFeedbackContainer() {
    tariffsFeedbackContainer = createElement("div", ["d-none"]);
    return tariffsFeedbackContainer;
}

function showFeedback(message, type) {
    const alert = createElement("div", ["alert", `alert-${type}`, "mb-0"]);
    alert.role = "alert";
    alert.textContent = message;
    tariffsFeedbackContainer.className = "";
    tariffsFeedbackContainer.replaceChildren(alert);
}

function clearFeedback() {
    tariffsFeedbackContainer.className = "d-none";
    tariffsFeedbackContainer.replaceChildren();
}

function renderEmptyState(message) {
    const title = createElement("div", ["fw-semibold", "mb-1"]);
    title.textContent = "Нет данных";
    const text = createElement("div", ["text-body-secondary"]);
    text.textContent = message;
    tariffsEmptyStateContainer.replaceChildren(title, text);
    tariffsEmptyStateContainer.classList.remove("d-none");
}

function createField(labelText, field) {
    const wrapper = createElement("div", ["d-flex", "flex-column", "gap-2"]);
    const label = createElement("label", ["form-label", "mb-0"]);
    label.htmlFor = field.id;
    label.textContent = labelText;
    wrapper.append(label, field);
    return wrapper;
}

function createCheckboxField(labelText, field) {
    const wrapper = createElement("div", ["form-check", "mb-0"]);
    const label = createElement("label", ["form-check-label"]);
    label.htmlFor = field.id;
    label.textContent = labelText;
    wrapper.append(field, label);
    return wrapper;
}

function createColumn(content, classes) {
    const column = createElement("div", classes);
    column.append(content);
    return column;
}

function createCell(content) {
    const cell = createElement("td", ["px-3", "py-3"]);
    cell.append(content);
    return cell;
}

function createInput(id, name, type, value = "") {
    const input = createElement("input", ["form-control", "form-control-sm"]);
    input.id = id;
    input.name = name;
    input.type = type;
    input.value = value;
    return input;
}

function createCheckbox(id, name, checked) {
    const input = createElement("input", ["form-check-input"]);
    input.id = id;
    input.name = name;
    input.type = "checkbox";
    input.checked = checked;
    return input;
}

function createElement(tagName, classes = []) {
    const element = document.createElement(tagName);
    if (classes.length > 0) {
        element.classList.add(...classes);
    }

    return element;
}

async function sendJsonRequest(url, method = "GET", headers = {}, body = null) {
    const options = { method, headers };

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
    return text && text.trim().length > 0 ? JSON.parse(text) : {};
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

function unwrapOrThrow(response, defaultMessage) {
    if (response && typeof response === "object" && "success" in response && response.success !== true) {
        throw new Error(response.message || defaultMessage || "Ошибка операции.");
    }
}
