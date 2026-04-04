let antiforgeryToken;
let pageRoot;
let feedbackContainer;
let tableSectionContainer;
let tableBody;
let emptyStateContainer;
let searchInput;
let serverSelect;
let createUserModal;
let createUserForm;
let createUserModalFeedback;
let createUserTelemtUserIdInput;
let createUserServerSelect;
let createUserTariffSelect;
let createUserPriceInput;
let createUserPriceNote;
let createUserSubmitButton;
let createUserBackdrop;
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
        syncCreateUserModalOptions();
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
        buildTableCard(),
        buildCreateUserModal()
    );

    pageRoot.replaceChildren(wrapper);
}

function buildHeader() {
    const header = createElement("section", ["d-flex", "flex-column", "gap-3"]);
    const badge = createElement("span", ["badge", "text-bg-dark", "align-self-start", "px-3", "py-2", "rounded-pill"]);
    badge.textContent = "Административная панель";

    const titleRow = createElement("div", ["d-flex", "flex-column", "flex-md-row", "justify-content-md-between", "align-items-md-center", "gap-3"]);
    const title = createElement("h1", ["h2", "mb-0"]);
    title.textContent = "Пользователи";

    const button = createElement("button", ["btn", "btn-dark", "align-self-start", "align-self-md-center"]);
    button.type = "button";
    button.textContent = "Создать пользователя";
    button.addEventListener("click", () => {
        openCreateUserModal();
    });

    titleRow.append(title, button);
    header.append(badge, titleRow);
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
    const searchColumn = createElement("div", ["col-12", "col-lg-8"]);
    const serverColumn = createElement("div", ["col-12", "col-lg-4"]);

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

    searchColumn.append(createFieldWrapper("Поиск по telemt userId", searchInput));
    serverColumn.append(createFieldWrapper("Сервер", serverSelect));

    row.append(searchColumn, serverColumn);
    body.append(row);
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
    const headings = [
        createHeaderContent("Telemt userId", "Идентификатор пользователя в telemt. По нему пользователя можно найти и сопоставить с записью в локальной базе."),
        createHeaderContent("Сервер"),
        createHeaderContent("Тариф"),
        createHeaderContent("Баланс"),
        createHeaderContent("Оплачено до", "Дата и время, до которых у пользователя оплачен доступ. Если значение пустое, оплаченного периода сейчас нет."),
        createHeaderContent("Ручная обработка", "Показывает, требуется ли вмешательство администратора из-за проблем синхронизации или несогласованных данных.")
    ];

    for (const heading of headings) {
        const th = createElement("th", ["text-nowrap", "px-3", "py-3"]);
        th.scope = "col";
        th.append(heading);
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

function buildCreateUserModal() {
    createUserModal = createElement("div", ["modal", "fade"]);
    createUserModal.id = "createUserModal";
    createUserModal.tabIndex = -1;
    createUserModal.setAttribute("aria-hidden", "true");

    const dialog = createElement("div", ["modal-dialog", "modal-dialog-centered"]);
    const content = createElement("div", ["modal-content", "border-0", "shadow"]);
    const header = createElement("div", ["modal-header"]);
    const title = createElement("h2", ["modal-title", "fs-5"]);
    title.textContent = "Создать пользователя";

    const closeButton = createElement("button", ["btn-close"]);
    closeButton.type = "button";
    closeButton.setAttribute("aria-label", "Закрыть");
    closeButton.addEventListener("click", () => {
        closeCreateUserModal();
    });

    header.append(title, closeButton);

    createUserForm = createElement("form", ["modal-body", "d-flex", "flex-column", "gap-3"]);
    createUserForm.id = "createUserForm";
    createUserForm.addEventListener("submit", async event => {
        event.preventDefault();
        await handleCreateUserSubmit();
    });

    createUserModalFeedback = createElement("div", ["d-none"]);
    createUserTelemtUserIdInput = createInputField(
        "createUserTelemtUserId",
        "createUserTelemtUserId",
        "Telemt userId",
        "Введите telemt userId"
    );
    createUserTelemtUserIdInput.maxLength = 64;

    createUserServerSelect = createSelectField("createUserServerId", "createUserServerId", "Сервер");
    createUserServerSelect.required = true;

    createUserTariffSelect = createSelectField("createUserTariffId", "createUserTariffId", "Тариф");
    createUserTariffSelect.required = true;
    createUserTariffSelect.addEventListener("change", () => {
        updateCreateUserPriceFieldState();
    });

    createUserPriceInput = createElement("input", ["form-control"]);
    createUserPriceInput.type = "number";
    createUserPriceInput.id = "createUserCustomPriceRub";
    createUserPriceInput.name = "createUserCustomPriceRub";
    createUserPriceInput.min = "0.01";
    createUserPriceInput.max = "1000";
    createUserPriceInput.step = "0.01";
    createUserPriceInput.inputMode = "decimal";
    createUserPriceNote = createElement("div", ["small", "text-body-secondary"]);

    const priceFieldWrapper = createFieldWrapper("Стоимость периода", createUserPriceInput);
    priceFieldWrapper.append(createUserPriceNote);

    createUserForm.append(
        createUserModalFeedback,
        createFieldWrapper("Telemt userId", createUserTelemtUserIdInput),
        createFieldWrapper("Сервер", createUserServerSelect),
        createFieldWrapper("Тариф", createUserTariffSelect),
        priceFieldWrapper
    );

    const footer = createElement("div", ["modal-footer"]);
    const cancelButton = createElement("button", ["btn", "btn-outline-secondary"]);
    cancelButton.type = "button";
    cancelButton.textContent = "Отмена";
    cancelButton.addEventListener("click", () => {
        closeCreateUserModal();
    });

    createUserSubmitButton = createElement("button", ["btn", "btn-dark"]);
    createUserSubmitButton.type = "submit";
    createUserSubmitButton.setAttribute("form", "createUserForm");
    createUserSubmitButton.textContent = "Создать";

    footer.append(cancelButton, createUserSubmitButton);
    content.append(header, createUserForm, footer);
    dialog.append(content);
    createUserModal.append(dialog);
    createUserModal.addEventListener("click", event => {
        if (event.target === createUserModal) {
            closeCreateUserModal();
        }
    });

    return createUserModal;
}

function populateServerFilter() {
    const optionNodes = [];
    const defaultOption = createElement("option");
    defaultOption.value = "";
    defaultOption.textContent = "Все сервера";
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

function syncCreateUserModalOptions() {
    if (!createUserServerSelect || !createUserTariffSelect) {
        return;
    }

    const serverOptions = [];
    const defaultServerOption = createElement("option");
    defaultServerOption.value = "";
    defaultServerOption.textContent = "Выберите сервер";
    serverOptions.push(defaultServerOption);

    for (const server of state.pageData?.servers ?? []) {
        const option = createElement("option");
        option.value = server.id;
        option.textContent = server.name;
        serverOptions.push(option);
    }

    createUserServerSelect.replaceChildren(...serverOptions);

    const tariffOptions = [];
    const defaultTariffOption = createElement("option");
    defaultTariffOption.value = "";
    defaultTariffOption.textContent = "Выберите тариф";
    tariffOptions.push(defaultTariffOption);

    for (const tariff of state.pageData?.tariffs ?? []) {
        const option = createElement("option");
        option.value = tariff.id;
        option.textContent = tariff.name;
        tariffOptions.push(option);
    }

    createUserTariffSelect.replaceChildren(...tariffOptions);
    updateCreateUserPriceFieldState();
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

async function handleCreateUserSubmit() {
    const telemtUserId = createUserTelemtUserIdInput.value.trim();
    const serverId = createUserServerSelect.value.trim();
    const tariffId = createUserTariffSelect.value.trim();
    const selectedTariff = getSelectedTariffOption();

    if (!telemtUserId) {
        showCreateUserModalFeedback("Укажите telemt userId.", "danger");
        createUserTelemtUserIdInput.focus();
        return;
    }

    if (!serverId) {
        showCreateUserModalFeedback("Выберите сервер.", "danger");
        createUserServerSelect.focus();
        return;
    }

    if (!tariffId || !selectedTariff) {
        showCreateUserModalFeedback("Выберите тариф.", "danger");
        createUserTariffSelect.focus();
        return;
    }

    const normalizedPrice = normalizePriceValue(createUserPriceInput.value);
    if (selectedTariff.requiresRenewal && normalizedPrice.length > 0 && !isValidTariffPrice(normalizedPrice)) {
        showCreateUserModalFeedback("Стоимость периода должна быть больше 0 и не превышать 1000 руб.", "danger");
        createUserPriceInput.focus();
        return;
    }

    const customPriceRub = resolveCreateUserCustomPrice(selectedTariff, normalizedPrice);

    toggleCreateUserFormState(true);
    showCreateUserModalFeedback("", "danger");
    clearFeedback();

    try {
        const requestUrl = `/Admin/Users?handler=Create&telemtUserId=${encodeURIComponent(telemtUserId)}&serverId=${encodeURIComponent(serverId)}&tariffId=${encodeURIComponent(tariffId)}${customPriceRub === null ? "" : `&customPriceRub=${encodeURIComponent(customPriceRub)}`}`;
        const response = await sendJsonRequest(
            requestUrl,
            "POST",
            buildJsonHeaders(antiforgeryToken)
        );

        unwrapOrThrow(response, "Не удалось создать пользователя.");

        closeCreateUserModal();
        await loadPageData();
        showFeedback(`Пользователь ${telemtUserId} создан.`, "success");
    }
    catch (error) {
        showCreateUserModalFeedback(error.message, "danger");
    }
    finally {
        toggleCreateUserFormState(false);
    }
}

function openCreateUserModal() {
    if ((state.pageData?.servers ?? []).length === 0) {
        showFeedback("Нет доступных активных серверов для создания пользователя.", "danger");
        return;
    }

    if ((state.pageData?.tariffs ?? []).length === 0) {
        showFeedback("Нет доступных тарифов для создания пользователя.", "danger");
        return;
    }

    resetCreateUserModal();
    createUserModal.classList.add("show", "d-block");
    createUserModal.removeAttribute("aria-hidden");
    document.body.classList.add("modal-open");
    document.body.append(buildCreateUserBackdrop());
    createUserTelemtUserIdInput.focus();
}

function closeCreateUserModal() {
    createUserModal.classList.remove("show", "d-block");
    createUserModal.setAttribute("aria-hidden", "true");
    document.body.classList.remove("modal-open");

    if (createUserBackdrop) {
        createUserBackdrop.remove();
        createUserBackdrop = null;
    }
}

function resetCreateUserModal() {
    if (!window.crypto || typeof window.crypto.randomUUID !== "function") {
        throw new Error("Браузер не поддерживает генерацию идентификатора пользователя.");
    }

    showCreateUserModalFeedback("", "danger");
    syncCreateUserModalOptions();

    createUserTelemtUserIdInput.value = window.crypto.randomUUID().replaceAll("-", "").toLowerCase();
    createUserServerSelect.value = "";
    createUserTariffSelect.value = (state.pageData?.tariffs ?? [])[0]?.id ?? "";
    updateCreateUserPriceFieldState();
    toggleCreateUserFormState(false);
}

function toggleCreateUserFormState(isBusy) {
    createUserTelemtUserIdInput.disabled = isBusy;
    createUserServerSelect.disabled = isBusy;
    createUserTariffSelect.disabled = isBusy;
    createUserPriceInput.disabled = isBusy || !(getSelectedTariffOption()?.requiresRenewal === true);
    createUserSubmitButton.disabled = isBusy;
}

function updateCreateUserPriceFieldState() {
    const selectedTariff = getSelectedTariffOption();

    if (!selectedTariff) {
        createUserPriceInput.value = "";
        createUserPriceInput.disabled = true;
        createUserPriceNote.textContent = "Сначала выберите тариф.";
        return;
    }

    if (!selectedTariff.requiresRenewal) {
        createUserPriceInput.value = "";
        createUserPriceInput.disabled = true;
        createUserPriceNote.textContent = "Для этого тарифа индивидуальная цена недоступна.";
        return;
    }

    createUserPriceInput.disabled = false;
    createUserPriceInput.value = formatDecimalValue(selectedTariff.periodPriceRub);
    createUserPriceNote.textContent = `По умолчанию будет использоваться цена тарифа ${formatMoney(selectedTariff.periodPriceRub)} за ${formatTariffPeriod(selectedTariff.periodMonths)}. При необходимости её можно изменить.`;
}

function getSelectedTariffOption() {
    const selectedTariffId = createUserTariffSelect?.value ?? "";
    return (state.pageData?.tariffs ?? []).find(tariff => tariff.id === selectedTariffId) ?? null;
}

function resolveCreateUserCustomPrice(selectedTariff, normalizedPrice) {
    if (!selectedTariff.requiresRenewal) {
        return null;
    }

    if (normalizedPrice.length === 0) {
        return null;
    }

    const defaultPrice = formatDecimalValue(selectedTariff.periodPriceRub);
    return normalizedPrice === defaultPrice ? null : normalizedPrice;
}

function buildCreateUserBackdrop() {
    createUserBackdrop = createElement("div", ["modal-backdrop", "fade", "show"]);
    return createUserBackdrop;
}

function showCreateUserModalFeedback(message, type) {
    if (!message) {
        createUserModalFeedback.className = "d-none";
        createUserModalFeedback.replaceChildren();
        return;
    }

    const alert = createElement("div", ["alert", `alert-${type}`, "mb-0"]);
    alert.role = "alert";
    alert.textContent = message;

    createUserModalFeedback.className = "";
    createUserModalFeedback.replaceChildren(alert);
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

function formatTariffPeriod(periodMonths) {
    if (periodMonths === 1) {
        return "1 месяц";
    }

    if (periodMonths >= 2 && periodMonths <= 4) {
        return `${periodMonths} месяца`;
    }

    return `${periodMonths} месяцев`;
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
