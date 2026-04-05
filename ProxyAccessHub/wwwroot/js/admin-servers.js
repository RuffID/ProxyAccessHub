let antiforgeryToken;
let serversPageRoot;
let serversFeedbackContainer;
let serversTableBody;
let serversEmptyStateContainer;

document.addEventListener("DOMContentLoaded", () => {
    initAdminServersPage();
});

async function initAdminServersPage() {
    antiforgeryToken = getRequestVerificationToken();
    serversPageRoot = document.getElementById("adminServersPage");

    buildServersPageShell();
    await loadServers();
}

async function loadServers() {
    clearFeedback();

    try {
        const pageData = await sendJsonRequest("/Admin/Servers?handler=Data", "GET", buildJsonHeaders(antiforgeryToken));
        renderServers(pageData.servers ?? []);
    }
    catch (error) {
        showFeedback(error.message, "danger");
        renderServers([]);
    }
}

function buildServersPageShell() {
    const wrapper = createElement("section", ["d-flex", "flex-column", "gap-4"]);
    wrapper.append(
        buildPageHeader("Серверы"),
        buildFeedbackContainer(),
        buildCreateCard(),
        buildTableCard()
    );

    serversPageRoot.replaceChildren(wrapper);
}

function buildCreateCard() {
    const section = createElement("section", ["card", "border-0", "shadow-sm"]);
    const body = createElement("div", ["card-body", "p-4"]);
    const header = createElement("div", ["d-flex", "align-items-center", "justify-content-between", "gap-3", "mb-3"]);
    const title = createElement("h2", ["h5", "mb-0"]);
    title.textContent = "Новый сервер";

    const row = createElement("div", ["row", "g-3", "align-items-end"]);
    const nameInput = createInput("serverCreateName", "serverCreateName", "text");
    const hostInput = createInput("serverCreateHost", "serverCreateHost", "text");
    const apiPortInput = createInput("serverCreateApiPort", "serverCreateApiPort", "number", "9091");
    apiPortInput.min = "1";
    apiPortInput.max = "65535";
    apiPortInput.step = "1";
    const apiBearerTokenInput = createInput("serverCreateApiBearerToken", "serverCreateApiBearerToken", "text");
    const maxUsersInput = createInput("serverCreateMaxUsers", "serverCreateMaxUsers", "number");
    maxUsersInput.min = "1";
    maxUsersInput.step = "1";
    maxUsersInput.value = "50";
    const activeInput = createCheckbox("serverCreateIsActive", "serverCreateIsActive", true);
    const syncEnabledInput = createCheckbox("serverCreateSyncEnabled", "serverCreateSyncEnabled", true);
    const syncIntervalInput = createInput("serverCreateSyncIntervalMinutes", "serverCreateSyncIntervalMinutes", "number", "15");
    syncIntervalInput.min = "1";
    syncIntervalInput.step = "1";
    const createButton = createElement("button", ["btn", "btn-dark"]);
    createButton.type = "button";
    createButton.textContent = "Добавить сервер";
    createButton.addEventListener("click", async () => {
        await createServer(nameInput, hostInput, apiPortInput, apiBearerTokenInput, maxUsersInput, activeInput, syncEnabledInput, syncIntervalInput);
    });

    const createButtonContainer = createElement("div", ["d-flex", "justify-content-end"]);
    createButtonContainer.append(createButton);
    header.append(title, createButtonContainer);

    row.append(
        createColumn(createField("Название", nameInput), ["col-12", "col-lg-2"]),
        createColumn(createField("Хост", hostInput), ["col-12", "col-lg-2"]),
        createColumn(createField("API порт", apiPortInput), ["col-12", "col-lg-1"]),
        createColumn(createField("Bearer-токен", apiBearerTokenInput), ["col-12", "col-lg-2"]),
        createColumn(createField("Лимит пользователей", maxUsersInput), ["col-12", "col-lg-1"]),
        createColumn(createCheckboxField("Активен", activeInput), ["col-12", "col-lg-2"]),
        createColumn(createCheckboxField("Синхронизация", syncEnabledInput), ["col-12", "col-lg-2"]),
        createColumn(createField("Интервал синхронизации, мин", syncIntervalInput), ["col-12", "col-lg-2"])
    );

    body.append(header, row);
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

    for (const heading of ["Название", "Хост", "API порт", "Bearer-токен", "Лимит", "Статус", "Синхронизация", "Интервал", "Действия"]) {
        const th = createElement("th", ["px-3", "py-3", "text-nowrap"]);
        th.scope = "col";
        th.textContent = heading;
        headRow.append(th);
    }

    thead.append(headRow);
    serversTableBody = createElement("tbody");
    table.append(thead, serversTableBody);
    tableResponsive.append(table);

    serversEmptyStateContainer = createElement("div", ["p-4", "d-none"]);
    body.append(tableResponsive, serversEmptyStateContainer);
    section.append(body);
    return section;
}

function renderServers(servers) {
    if (servers.length === 0) {
        serversTableBody.replaceChildren();
        renderEmptyState("Список серверов пока пуст.");
        return;
    }

    serversEmptyStateContainer.classList.add("d-none");
    const rows = servers.map(server => buildServerRow(server));
    serversTableBody.replaceChildren(...rows);
}

function buildServerRow(server) {
    const row = createElement("tr");
    const nameInput = createInput(`serverName_${server.id}`, `serverName_${server.id}`, "text", server.name);
    const hostInput = createInput(`serverHost_${server.id}`, `serverHost_${server.id}`, "text", server.host);
    const apiPortInput = createInput(`serverApiPort_${server.id}`, `serverApiPort_${server.id}`, "number", String(server.apiPort));
    apiPortInput.min = "1";
    apiPortInput.max = "65535";
    apiPortInput.step = "1";
    const apiBearerTokenInput = createInput(`serverApiBearerToken_${server.id}`, `serverApiBearerToken_${server.id}`, "text", server.apiBearerToken);
    const maxUsersInput = createInput(`serverMaxUsers_${server.id}`, `serverMaxUsers_${server.id}`, "number", String(server.maxUsers));
    maxUsersInput.min = "1";
    maxUsersInput.step = "1";
    const activeInput = createCheckbox(`serverIsActive_${server.id}`, `serverIsActive_${server.id}`, server.isActive);
    const syncEnabledInput = createCheckbox(`serverSyncEnabled_${server.id}`, `serverSyncEnabled_${server.id}`, server.syncEnabled);
    const syncIntervalInput = createInput(`serverSyncIntervalMinutes_${server.id}`, `serverSyncIntervalMinutes_${server.id}`, "number", String(server.syncIntervalMinutes));
    syncIntervalInput.min = "1";
    syncIntervalInput.step = "1";
    const actions = createElement("div", ["d-flex", "flex-wrap", "gap-2"]);
    const saveButton = createElement("button", ["btn", "btn-outline-dark", "btn-sm"]);
    const checkButton = createElement("button", ["btn", "btn-outline-secondary", "btn-sm"]);
    const deleteButton = createElement("button", ["btn", "btn-outline-danger", "btn-sm"]);

    saveButton.type = "button";
    saveButton.textContent = "Сохранить";
    saveButton.addEventListener("click", async () => {
        await updateServer(server.id, nameInput, hostInput, apiPortInput, apiBearerTokenInput, maxUsersInput, activeInput, syncEnabledInput, syncIntervalInput);
    });

    checkButton.type = "button";
    checkButton.textContent = "Проверить связь";
    checkButton.addEventListener("click", async () => {
        await checkServerConnection(server, checkButton);
    });

    deleteButton.type = "button";
    deleteButton.textContent = "Удалить";
    deleteButton.addEventListener("click", async () => {
        await deleteServer(server, deleteButton);
    });

    actions.append(saveButton, checkButton, deleteButton);

    row.append(
        createCell(nameInput),
        createCell(hostInput),
        createCell(apiPortInput),
        createCell(apiBearerTokenInput),
        createCell(maxUsersInput),
        createCell(createCheckboxField("Активен", activeInput)),
        createCell(createCheckboxField("Синхронизация", syncEnabledInput)),
        createCell(syncIntervalInput),
        createCell(actions)
    );

    return row;
}

async function createServer(nameInput, hostInput, apiPortInput, apiBearerTokenInput, maxUsersInput, activeInput, syncEnabledInput, syncIntervalInput) {
    try {
        const url = `/Admin/Servers?handler=Create&name=${encodeURIComponent(nameInput.value)}&host=${encodeURIComponent(hostInput.value)}&apiPort=${encodeURIComponent(apiPortInput.value)}&apiBearerToken=${encodeURIComponent(apiBearerTokenInput.value)}&maxUsers=${encodeURIComponent(maxUsersInput.value)}&isActive=${encodeURIComponent(String(activeInput.checked))}&syncEnabled=${encodeURIComponent(String(syncEnabledInput.checked))}&syncIntervalMinutes=${encodeURIComponent(syncIntervalInput.value)}`;
        const response = await sendJsonRequest(url, "POST", buildJsonHeaders(antiforgeryToken));
        unwrapOrThrow(response, "Не удалось создать сервер.");

        nameInput.value = "";
        hostInput.value = "";
        apiPortInput.value = "9091";
        apiBearerTokenInput.value = "";
        maxUsersInput.value = "50";
        activeInput.checked = true;
        syncEnabledInput.checked = true;
        syncIntervalInput.value = "15";
        showFeedback("Сервер добавлен.", "success");
        await loadServers();
    }
    catch (error) {
        showFeedback(error.message, "danger");
    }
}

async function updateServer(id, nameInput, hostInput, apiPortInput, apiBearerTokenInput, maxUsersInput, activeInput, syncEnabledInput, syncIntervalInput) {
    try {
        const url = `/Admin/Servers?handler=Update&id=${encodeURIComponent(id)}&name=${encodeURIComponent(nameInput.value)}&host=${encodeURIComponent(hostInput.value)}&apiPort=${encodeURIComponent(apiPortInput.value)}&apiBearerToken=${encodeURIComponent(apiBearerTokenInput.value)}&maxUsers=${encodeURIComponent(maxUsersInput.value)}&isActive=${encodeURIComponent(String(activeInput.checked))}&syncEnabled=${encodeURIComponent(String(syncEnabledInput.checked))}&syncIntervalMinutes=${encodeURIComponent(syncIntervalInput.value)}`;
        const response = await sendJsonRequest(url, "POST", buildJsonHeaders(antiforgeryToken));
        unwrapOrThrow(response, "Не удалось обновить сервер.");

        showFeedback("Сервер обновлён.", "success");
        await loadServers();
    }
    catch (error) {
        showFeedback(error.message, "danger");
    }
}

async function checkServerConnection(server, button) {
    button.disabled = true;
    clearFeedback();

    try {
        const url = `/Admin/Servers?handler=CheckConnection&id=${encodeURIComponent(server.id)}`;
        const response = await sendJsonRequest(url, "POST", buildJsonHeaders(antiforgeryToken));
        unwrapOrThrow(response, "Не удалось проверить связь с сервером.");

        showFeedback(`Связь с сервером '${server.name}' успешно проверена.`, "success");
    }
    catch (error) {
        showFeedback(error.message, "danger");
    }
    finally {
        button.disabled = false;
    }
}

async function deleteServer(server, button) {
    if (!window.confirm(`Удалить сервер '${server.name}'?`)) {
        return;
    }

    button.disabled = true;
    clearFeedback();

    try {
        const url = `/Admin/Servers?handler=Delete&id=${encodeURIComponent(server.id)}`;
        const response = await sendJsonRequest(url, "POST", buildJsonHeaders(antiforgeryToken));
        unwrapOrThrow(response, "Не удалось удалить сервер.");

        showFeedback(`Сервер '${server.name}' удалён.`, "success");
        await loadServers();
    }
    catch (error) {
        showFeedback(error.message, "danger");
    }
    finally {
        button.disabled = false;
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
    serversFeedbackContainer = createElement("div", ["d-none"]);
    return serversFeedbackContainer;
}

function showFeedback(message, type) {
    const alert = createElement("div", ["alert", `alert-${type}`, "mb-0"]);
    alert.role = "alert";
    alert.textContent = message;
    serversFeedbackContainer.className = "";
    serversFeedbackContainer.replaceChildren(alert);
}

function clearFeedback() {
    serversFeedbackContainer.className = "d-none";
    serversFeedbackContainer.replaceChildren();
}

function renderEmptyState(message) {
    const title = createElement("div", ["fw-semibold", "mb-1"]);
    title.textContent = "Нет данных";
    const text = createElement("div", ["text-body-secondary"]);
    text.textContent = message;
    serversEmptyStateContainer.replaceChildren(title, text);
    serversEmptyStateContainer.classList.remove("d-none");
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
