let antiforgeryToken;
let pageRoot;
let feedbackContainer;
let tableBody;
let emptyStateContainer;
let periodFromInput;
let periodToInput;
let searchInput;
let serverSelect;
let statusSelect;
let paymentsModalElement;
let paymentsModal;
let paymentsModalTitle;
let paymentsModalBody;
let paymentsModalFeedback;
let paymentsModalAttachButton;
let paymentsModalSpinner;
let state = {
    pageData: null,
    filteredPayments: [],
    selectedPaymentRequestId: null,
    selectedPaymentLabel: "",
    selectedCheckDetails: null
};

document.addEventListener("DOMContentLoaded", () => {
    initAdminPaymentsPage();
});

async function initAdminPaymentsPage() {
    antiforgeryToken = getRequestVerificationToken();
    pageRoot = document.getElementById("adminPaymentsPage");

    buildPageShell();
    applyDefaultPeriodFilter();
    await loadPageData();
}

async function loadPageData() {
    setLoadingState(true);
    clearFeedback();

    try {
        state.pageData = await sendJsonRequest(
            "/Admin/Payments?handler=Data",
            "GET",
            buildJsonHeaders(antiforgeryToken)
        );

        populateFilterOptions();
        applyFilters();
    }
    catch (error) {
        showFeedback(error.message, "danger");
        renderEmptyState("Не удалось загрузить список платежей.");
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
        buildTableCard(),
        buildCheckModal()
    );

    pageRoot.replaceChildren(wrapper);
}

function buildHeader() {
    const header = createElement("section", ["d-flex", "flex-column", "gap-3"]);
    const badge = createElement("span", ["badge", "text-bg-dark", "align-self-start", "px-3", "py-2", "rounded-pill"]);
    badge.textContent = "Административная панель";

    const row = createElement("div", ["d-flex", "flex-column", "flex-md-row", "justify-content-md-between", "align-items-md-center", "gap-3"]);
    const title = createElement("h1", ["h2", "mb-0"]);
    title.textContent = "Платежи";

    const actions = createElement("div", ["d-flex", "gap-2"]);
    const refreshButton = createElement("button", ["btn", "btn-dark"]);
    refreshButton.type = "button";
    refreshButton.textContent = "Обновить";
    refreshButton.addEventListener("click", async () => {
        if (details.addedPaymentCount > 0) {
            showModalFeedback(`Добавлено ${details.addedPaymentCount} платеж(ей) на сумму заявки ${formatMoney(details.addedAppliedAmountRub)} ₽. Фактическая сумма: ${formatMoney(details.addedActualAmountRub)} ₽.`, "success");
        }
        else if (details.updatedActualAmountCount > 0) {
            showModalFeedback(`Фактическая сумма обновлена у ${details.updatedActualAmountCount} платеж(ей).`, "success");
        }

        await loadPageData();
    });

    actions.append(refreshButton);
    row.append(title, actions);
    header.append(badge, row);
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

    periodFromInput = createDateInput("paymentsPeriodFrom", "paymentsPeriodFrom");
    periodToInput = createDateInput("paymentsPeriodTo", "paymentsPeriodTo");
    searchInput = createInputField("paymentsSearch", "paymentsSearch", "Поиск", "telemt userId или заявка");
    serverSelect = createSelectField("paymentsServer", "paymentsServer");
    statusSelect = createSelectField("paymentsStatus", "paymentsStatus");

    const fields = [
        createFieldColumn("col-12 col-md-6 col-xl-2", "Период с", periodFromInput),
        createFieldColumn("col-12 col-md-6 col-xl-2", "Период по", periodToInput),
        createFieldColumn("col-12 col-xl-3", "Поиск", searchInput),
        createFieldColumn("col-12 col-md-6 col-xl-2", "Сервер", serverSelect),
        createFieldColumn("col-12 col-md-6 col-xl-2", "Статус", statusSelect),
    ];

    for (const field of fields) {
        row.append(field);
    }

    body.append(row);
    section.append(body);

    periodFromInput.addEventListener("input", () => applyFilters());
    periodToInput.addEventListener("input", () => applyFilters());
    searchInput.addEventListener("input", () => applyFilters());
    serverSelect.addEventListener("change", () => applyFilters());
    statusSelect.addEventListener("change", () => applyFilters());

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
        createHeaderContent("Заявка", "Идентификатор локальной заявки на оплату. Он же передаётся в label платежа YooMoney."),
        createHeaderContent("Пользователь", "Идентификатор пользователя в telemt, к которому относится заявка."),
        createHeaderContent("Сервер"),
        createHeaderContent("Сумма", "Ожидаемая сумма заявки, привязанная сумма и число найденных локальных оплат."),
        createHeaderContent("Статус", "Краткий итог по заявке: ожидает оплату, оплачена или переплачена."),
        createHeaderContent("Создано", "Дата создания заявки и срок её действия."),
        createHeaderContent("Последняя оплата", "Время последней локально привязанной оплаты по этой заявке."),
        createHeaderContent("Платежи")
    ];

    for (const headingContent of headings) {
        const th = createElement("th", ["text-nowrap", "px-3", "py-3"]);
        th.scope = "col";
        th.append(headingContent);
        headRow.append(th);
    }

    thead.append(headRow);
    tableBody = createElement("tbody");
    table.append(thead, tableBody);
    tableResponsive.append(table);

    emptyStateContainer = createElement("div", ["p-4", "d-none"]);
    body.append(tableResponsive, emptyStateContainer);
    section.append(body);
    return section;
}

function buildCheckModal() {
    paymentsModalElement = createElement("div", ["modal", "fade"]);
    paymentsModalElement.id = "paymentsCheckModal";
    paymentsModalElement.tabIndex = -1;
    paymentsModalElement.setAttribute("aria-hidden", "true");

    const dialog = createElement("div", ["modal-dialog", "modal-dialog-centered", "modal-xl", "modal-dialog-scrollable"]);
    const content = createElement("div", ["modal-content", "border-0", "shadow"]);
    const header = createElement("div", ["modal-header"]);
    paymentsModalTitle = createElement("h2", ["modal-title", "fs-5"]);
    paymentsModalTitle.textContent = "Платежи заявки";

    const closeButton = createElement("button", ["btn-close"]);
    closeButton.type = "button";
    closeButton.setAttribute("data-bs-dismiss", "modal");
    closeButton.setAttribute("aria-label", "Закрыть");

    header.append(paymentsModalTitle, closeButton);

    const modalBodyContainer = createElement("div", ["modal-body", "d-flex", "flex-column", "gap-3"]);
    paymentsModalFeedback = createElement("div", ["d-none"]);
    paymentsModalSpinner = createElement("div", ["text-center", "py-5", "d-none"]);
    const spinner = createElement("div", ["spinner-border", "text-dark"]);
    spinner.setAttribute("role", "status");
    const spinnerText = createElement("span", ["visually-hidden"]);
    spinnerText.textContent = "Загрузка";
    spinner.append(spinnerText);
    paymentsModalSpinner.append(spinner);

    paymentsModalBody = createElement("div", ["d-flex", "flex-column", "gap-3"]);
    modalBodyContainer.append(paymentsModalFeedback, paymentsModalSpinner, paymentsModalBody);

    const footer = createElement("div", ["modal-footer"]);
    paymentsModalAttachButton = createElement("button", ["btn", "btn-dark"]);
    paymentsModalAttachButton.type = "button";
    paymentsModalAttachButton.textContent = "Обновить платежи из YooMoney";
    paymentsModalAttachButton.textContent = "Получить информацию по платежу из YooMoney";
    paymentsModalAttachButton.textContent = "Обновить платежи из YooMoney";
    paymentsModalAttachButton.addEventListener("click", async () => {
        await handleApplyMissingOperations();
    });

    const closeFooterButton = createElement("button", ["btn", "btn-outline-secondary"]);
    closeFooterButton.type = "button";
    closeFooterButton.setAttribute("data-bs-dismiss", "modal");
    closeFooterButton.textContent = "Закрыть";

    footer.append(paymentsModalAttachButton, closeFooterButton);
    content.append(header, modalBodyContainer, footer);
    dialog.append(content);
    paymentsModalElement.append(dialog);

    paymentsModal = new bootstrap.Modal(paymentsModalElement);
    return paymentsModalElement;
}

function populateFilterOptions() {
    populateSelect(serverSelect, state.pageData.servers, "id", "name", "Все серверы");
    populateSelect(statusSelect, state.pageData.statuses, "code", "name", "Все статусы");
}

function applyFilters() {
    if (!state.pageData) {
        state.filteredPayments = [];
        renderTable();
        return;
    }

    const fromValue = parseDateBoundary(periodFromInput.value, false);
    const toValue = parseDateBoundary(periodToInput.value, true);
    const normalizedSearch = searchInput.value.trim().toLowerCase();
    const selectedServerId = serverSelect.value;
    const selectedStatus = statusSelect.value;

    state.filteredPayments = state.pageData.payments.filter(payment => {
        const createdAt = new Date(payment.createdAtUtc);
        if (fromValue !== null && createdAt < fromValue) {
            return false;
        }

        if (toValue !== null && createdAt > toValue) {
            return false;
        }

        if (selectedServerId && payment.serverId !== selectedServerId) {
            return false;
        }

        if (selectedStatus && payment.statusCode !== selectedStatus) {
            return false;
        }

        if (normalizedSearch.length > 0) {
            const values = [
                payment.telemtUserId,
                payment.label
            ];
            const found = values.some(value => String(value).toLowerCase().includes(normalizedSearch));
            if (!found) {
                return false;
            }
        }

        return true;
    });

    renderTable();
}

function renderTable() {
    tableBody.replaceChildren();

    if (state.filteredPayments.length === 0) {
        renderEmptyState("По выбранным фильтрам платежи не найдены.");
        return;
    }

    emptyStateContainer.classList.add("d-none");

    for (const payment of state.filteredPayments) {
        tableBody.append(buildPaymentRow(payment));
    }
}

function buildPaymentRow(payment) {
    const row = createElement("tr");
    row.append(
        createCell(buildRequestCell(payment)),
        createCell(buildUserCell(payment)),
        createCell(buildTextBlock(payment.serverName)),
        createCell(buildAmountCell(payment)),
        createCell(buildStatusBadge(payment)),
        createCell(buildDateBlock(payment.createdAtUtc, payment.expiresAtUtc)),
        createCell(buildLastPaymentCell(payment.lastPaymentAtUtc)),
        createCell(buildActionsCell(payment))
    );
    return row;
}

function buildRequestCell(payment) {
    const wrapper = createElement("div", ["fw-semibold", "text-break"]);
    wrapper.style.maxWidth = "11rem";
    wrapper.textContent = payment.label;
    return wrapper;
}

function buildUserCell(payment) {
    const wrapper = createElement("div", ["fw-semibold", "text-break"]);
    wrapper.style.maxWidth = "11rem";
    wrapper.textContent = payment.telemtUserId;
    return wrapper;
}

function buildAmountCell(payment) {
    const wrapper = createElement("div", ["d-flex", "flex-column", "gap-1"]);
    wrapper.append(
        buildTextBlock(`${formatMoney(payment.requestedAmountRub)} ₽`, ["fw-semibold"]),
        buildTextBlock(`Привязано: ${formatMoney(payment.attachedAmountRub)} ₽`, ["small", "text-body-secondary"]),
        buildTextBlock(`Платежей: ${payment.attachedPaymentCount}`, ["small", "text-body-secondary"])
    );
    return wrapper;
}

function buildStatusBadge(payment) {
    const badge = createElement("span", ["fw-semibold"]);

    if (payment.statusCode === "paid") {
        badge.classList.add("text-success");
    }
    else if (payment.statusCode === "overpaid") {
        badge.classList.add("text-warning");
    }
    else {
        badge.classList.add("text-primary");
    }

    badge.textContent = payment.statusName;
    return badge;
}

function buildDateBlock(createdAtUtc, expiresAtUtc) {
    const wrapper = createElement("div", ["d-flex", "flex-column", "gap-1"]);
    wrapper.append(
        buildTextBlock(formatDateTime(createdAtUtc), ["fw-semibold"]),
        buildTextBlock(`Истекает: ${formatDateTime(expiresAtUtc)}`, ["small", "text-body-secondary"])
    );
    return wrapper;
}

function buildLastPaymentCell(lastPaymentAtUtc) {
    return buildTextBlock(
        lastPaymentAtUtc ? formatDateTime(lastPaymentAtUtc) : "Нет оплат",
        lastPaymentAtUtc ? [] : ["text-body-secondary"]
    );
}

function buildActionsCell(payment) {
    const wrapper = createElement("div", ["d-flex"]);
    const button = createElement("button", ["btn", "btn-outline-dark", "btn-sm"]);
    button.type = "button";
    button.textContent = "Посмотреть";
    button.addEventListener("click", async () => {
        await openCheckModal(payment.paymentRequestId, payment.label);
    });

    wrapper.append(button);
    return wrapper;
}

async function openCheckModal(paymentRequestId, label) {
    state.selectedPaymentRequestId = paymentRequestId;
    state.selectedPaymentLabel = label;
    state.selectedCheckDetails = null;
    paymentsModalTitle.textContent = `Платежи заявки ${label}`;
    paymentsModalBody.replaceChildren();
    clearModalFeedback();
    toggleModalSpinner(true);
    paymentsModalAttachButton.classList.remove("d-none");
    paymentsModalAttachButton.disabled = false;
    paymentsModalAttachButton.textContent = "Получить информацию по платежу из YooMoney";
    paymentsModalAttachButton.textContent = "Обновить платежи из YooMoney";
    paymentsModal.show();

    try {
        const details = await sendJsonRequest(
            `/Admin/Payments?handler=Check&paymentRequestId=${encodeURIComponent(paymentRequestId)}`,
            "GET",
            buildJsonHeaders(antiforgeryToken)
        );

        state.selectedCheckDetails = details;
        renderCheckDetails(details);
    }
    catch (error) {
        showModalFeedback(error.message, "danger");
    }
    finally {
        toggleModalSpinner(false);
    }
}

function renderCheckDetails(details) {
    state.selectedCheckDetails = details;
    paymentsModalBody.replaceChildren(buildAttachedPaymentsCard(details.attachedPayments));
}

function buildAttachedPaymentsCard(attachedPayments) {
    const section = createElement("section", ["card", "border-0", "shadow-sm"]);
    const body = createElement("div", ["card-body", "d-flex", "flex-column", "gap-3"]);

    if (!attachedPayments || attachedPayments.length === 0) {
        body.append(buildTextBlock("К заявке пока не привязано ни одного платежа из базы данных.", ["text-body-secondary"]));
        section.append(body);
        return section;
    }

    body.append(buildSimpleTable(
        ["Операция", "Сумма", "Фактическая сумма", "Получено"],
        attachedPayments.map(payment => [
            payment.providerOperationId,
            `${formatMoney(payment.amountRub)} ₽`,
            payment.actualAmountRub !== null && payment.actualAmountRub !== undefined
                ? `${formatMoney(payment.actualAmountRub)} ₽`
                : "—",
            formatDateTime(payment.receivedAtUtc)
        ])
    ));

    section.append(body);
    return section;
}

function buildYooMoneyStateCard() {
    const section = createElement("section", ["card", "border-0", "shadow-sm"]);
    const body = createElement("div", ["card-body", "d-flex", "flex-column", "gap-3"]);
    const title = createElement("h3", ["h5", "mb-0"]);
    title.textContent = "YooMoney";
    body.append(title);

    body.append(buildTextBlock(
        "Данные YooMoney ещё не загружены. Нажмите кнопку получения информации по платежу, чтобы автоматически добавить новые операции в базу.",
        ["text-body-secondary"]));

    section.append(body);
    return section;
}

async function handleApplyMissingOperations() {
    if (!state.selectedPaymentRequestId) {
        return;
    }

    clearModalFeedback();
    paymentsModalAttachButton.disabled = true;

    try {
        const details = await sendJsonRequest(
            `/Admin/Payments?handler=ApplyMissingOperations&paymentRequestId=${encodeURIComponent(state.selectedPaymentRequestId)}`,
            "POST",
            buildJsonHeaders(antiforgeryToken)
        );

        renderCheckDetails(details);
        showModalFeedback(
            details.addedPaymentCount > 0
                ? `Добавлено ${details.addedPaymentCount} платеж(ей) на сумму заявки ${formatMoney(details.addedAppliedAmountRub)} ₽. Фактическая сумма: ${formatMoney(details.addedActualAmountRub)} ₽.`
                : "Запрос в YooMoney выполнен. Новых платежей для добавления не найдено.",
            "success");
        await loadPageData();
    }
    catch (error) {
        showModalFeedback(error.message, "danger");
    }
    finally {
        paymentsModalAttachButton.disabled = false;
    }
}

function buildSimpleTable(headings, rows) {
    const responsive = createElement("div", ["table-responsive"]);
    const table = createElement("table", ["table", "table-sm", "align-middle", "mb-0"]);
    const thead = createElement("thead", ["table-light"]);
    const headRow = createElement("tr");

    for (const headingText of headings) {
        const th = createElement("th", ["text-nowrap"]);
        th.scope = "col";
        th.textContent = headingText;
        headRow.append(th);
    }

    const tbody = createElement("tbody");
    for (const rowValues of rows) {
        const row = createElement("tr");
        for (const value of rowValues) {
            const cell = createElement("td");
            cell.textContent = value;
            row.append(cell);
        }
        tbody.append(row);
    }

    thead.append(headRow);
    table.append(thead, tbody);
    responsive.append(table);
    return responsive;
}

function createFieldColumn(columnClasses, labelText, field) {
    const column = createElement("div", columnClasses.split(" "));
    column.append(createFieldWrapper(labelText, field));
    return column;
}

function createFieldWrapper(labelText, field) {
    const wrapper = createElement("div", ["d-flex", "flex-column", "gap-2"]);
    const label = createElement("label", ["form-label", "mb-0"]);
    label.htmlFor = field.id;
    label.textContent = labelText;
    wrapper.append(label, field);
    return wrapper;
}

function createInputField(id, name, ariaLabel, placeholder) {
    const input = createElement("input", ["form-control"]);
    input.type = "text";
    input.id = id;
    input.name = name;
    input.placeholder = placeholder;
    input.setAttribute("aria-label", ariaLabel);
    return input;
}

function createDateInput(id, name) {
    const input = createElement("input", ["form-control"]);
    input.type = "date";
    input.id = id;
    input.name = name;
    return input;
}

function createSelectField(id, name) {
    const select = createElement("select", ["form-select"]);
    select.id = id;
    select.name = name;
    return select;
}

function populateSelect(select, items, valueField, textField, defaultText) {
    select.replaceChildren(createOption("", defaultText));

    for (const item of items) {
        select.append(createOption(item[valueField], item[textField]));
    }
}

function createOption(value, text) {
    const option = createElement("option");
    option.value = value;
    option.textContent = text;
    return option;
}

function createCell(content) {
    const cell = createElement("td", ["px-3", "py-3"]);
    cell.append(content);
    return cell;
}

function createElement(tagName, classes = []) {
    const element = document.createElement(tagName);

    if (classes.length > 0) {
        element.classList.add(...classes);
    }

    return element;
}

function buildTextBlock(text, classes = []) {
    const element = createElement("div", classes);
    element.textContent = text;
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

function showModalFeedback(message, type) {
    paymentsModalFeedback.className = `alert alert-${type}`;
    paymentsModalFeedback.textContent = message;
}

function clearModalFeedback() {
    paymentsModalFeedback.className = "d-none";
    paymentsModalFeedback.textContent = "";
}

function renderEmptyState(message) {
    emptyStateContainer.classList.remove("d-none");
    emptyStateContainer.textContent = message;
}

function setLoadingState(isLoading) {
    if (isLoading) {
        renderEmptyState("Загрузка платежей...");
    }
}

function toggleModalSpinner(isVisible) {
    paymentsModalSpinner.classList.toggle("d-none", !isVisible);
    paymentsModalBody.classList.toggle("d-none", isVisible);
}

function parseDateBoundary(value, endOfDay) {
    if (!value) {
        return null;
    }

    const parsed = new Date(endOfDay ? `${value}T23:59:59.999` : `${value}T00:00:00.000`);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
}

function formatDateTime(value) {
    if (!value) {
        return "—";
    }

    const date = new Date(value);
    return date.toLocaleString("ru-RU", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit"
    });
}

function formatMoney(value) {
    return Number(value).toLocaleString("ru-RU", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}

function applyDefaultPeriodFilter() {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, "0");
    const lastDay = new Date(year, now.getMonth() + 1, 0).getDate();

    periodFromInput.value = `${year}-${month}-01`;
    periodToInput.value = `${year}-${month}-${String(lastDay).padStart(2, "0")}`;
}

function createHeaderContent(text, description = "") {
    if (!description) {
        const label = createElement("span");
        label.textContent = text;
        return label;
    }

    const wrapper = createElement("span", ["d-inline-flex", "align-items-center", "gap-2"]);
    const label = createElement("span");
    label.textContent = text;

    const hint = createElement("button", ["btn", "btn-sm", "btn-outline-secondary", "rounded-circle", "d-inline-flex", "justify-content-center", "align-items-center", "fw-semibold", "p-0"]);
    hint.type = "button";
    hint.style.width = "1.5rem";
    hint.style.height = "1.5rem";
    hint.style.lineHeight = "1";
    hint.style.cursor = "pointer";
    hint.setAttribute("aria-label", description);
    hint.setAttribute("data-bs-toggle", "popover");
    hint.setAttribute("data-bs-trigger", "hover focus");
    hint.setAttribute("data-bs-placement", "top");
    hint.setAttribute("data-bs-content", description);
    hint.textContent = "?";
    new bootstrap.Popover(hint);

    wrapper.append(label, hint);
    return wrapper;
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
