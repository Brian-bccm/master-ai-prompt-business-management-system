const page = document.body.dataset.page;
const navItems = [
    { label: "Dashboard", href: "/dashboard.html", roles: ["Owner", "Manager", "Admin", "Staff"] },
    { label: "Inventory", href: "/inventory.html", roles: ["Owner", "Manager", "Admin"] },
    { label: "POS", href: "/pos.html", roles: ["Owner", "Manager", "Admin", "Staff"] },
    { label: "Reports", href: "/reports.html", roles: ["Owner", "Manager", "Admin"] },
    { label: "Suppliers", href: "/suppliers.html", roles: ["Owner", "Manager", "Admin"] },
    { label: "Employees", href: "/employees.html", roles: ["Owner", "Manager"] },
    { label: "Audit Logs", href: "/audit.html", roles: ["Owner", "Manager"] },
    { label: "Notifications", href: "/notifications.html", roles: ["Owner", "Manager", "Admin", "Staff"] }
];
let cart = [];
let posProducts = [];

document.addEventListener("DOMContentLoaded", () => {
    buildShell();
    document.getElementById("logoutBtn")?.addEventListener("click", logout);

    if (page !== "login" && !token()) {
        location.href = "/index.html";
        return;
    }

    if (page !== "login" && !canAccessPage(location.pathname)) {
        location.href = "/dashboard.html";
        return;
    }

    if (page === "login") initLogin();
    if (page === "dashboard") loadDashboard();
    if (page === "inventory") initInventory();
    if (page === "pos") initPos();
    if (page === "reports") initReports();
    if (page === "suppliers") initSuppliers();
    if (page === "employees") initEmployees();
    if (page === "audit") loadAudit();
    if (page === "notifications") loadNotificationsPage();
});

function buildShell() {
    const sidebar = document.querySelector(".sidebar");
    if (!sidebar) return;

    const user = currentUser();
    const availableNav = navItems.filter(item => canAccessRole(item.roles));
    sidebar.innerHTML = `
        <div class="brand">BMS</div>
        <div class="sidebar-card">
            <span>${user?.role || "Workspace"}</span>
            <strong>${user?.fullName || "Retail Command"}</strong>
        </div>
        <nav class="nav">
            ${availableNav.map(({ label, href }) => `<a class="${location.pathname === href ? "active" : ""}" href="${href}">${label}</a>`).join("")}
        </nav>
    `;
}

function currentUser() {
    try {
        return JSON.parse(localStorage.getItem("bms_user") || "null");
    } catch {
        return null;
    }
}

function canAccessRole(roles) {
    const role = currentUser()?.role;
    return !roles || roles.includes(role);
}

function canAccessPage(pathname) {
    const route = navItems.find(item => item.href === pathname);
    return !route || canAccessRole(route.roles);
}

function initLogin() {
    document.getElementById("togglePassword").addEventListener("click", () => {
        const passwordInput = document.getElementById("password");
        const toggleButton = document.getElementById("togglePassword");
        const shouldShow = passwordInput.type === "password";
        passwordInput.type = shouldShow ? "text" : "password";
        toggleButton.classList.toggle("is-visible", shouldShow);
        toggleButton.setAttribute("aria-label", shouldShow ? "Hide password" : "Show password");
    });

    document.getElementById("loginForm").addEventListener("submit", async event => {
        event.preventDefault();
        const message = document.getElementById("message");
        try {
            const result = await api("/auth/login", {
                method: "POST",
                body: JSON.stringify({
                    email: document.getElementById("email").value,
                    password: document.getElementById("password").value
                })
            });
            localStorage.setItem("bms_token", result.token);
            localStorage.setItem("bms_user", JSON.stringify(result));
            location.href = "/dashboard.html";
        } catch (error) {
            message.textContent = error.message;
        }
    });
}

async function logout() {
    try {
        if (token()) await api("/auth/logout", { method: "POST" });
    } catch {
        // Local logout should still complete even if the server session log fails.
    }
    localStorage.removeItem("bms_token");
    localStorage.removeItem("bms_user");
    location.href = "/index.html";
}

async function loadDashboard() {
    const data = await api("/dashboard/summary");
    const canMonitorEmployees = canAccessRole(["Owner", "Manager"]);
    document.getElementById("dailySales").textContent = money(data.dailySales);
    document.getElementById("weeklySales").textContent = money(data.weeklySales);
    document.getElementById("monthlyRevenue").textContent = money(data.monthlyRevenue);
    document.getElementById("grossProfit").textContent = money(data.grossProfit);
    document.getElementById("productsInStock").textContent = data.productsInStock;
    document.getElementById("lowStockCount").textContent = data.lowStockCount;
    document.getElementById("activeEmployees").textContent = data.activeEmployees;
    document.getElementById("failedLoginAttempts").textContent = data.failedLoginAttempts;
    document.getElementById("topProducts").innerHTML = (data.topProducts.length ? data.topProducts : [{ productName: "No sales yet", quantitySold: 0, revenue: 0 }])
        .map((item, index) => `<div class="list-row" style="animation: fadeUp .3s ease ${index * 60}ms both"><span>${item.productName}</span><strong>${item.quantitySold} sold</strong></div>`).join("");
    document.getElementById("stockAlerts").innerHTML = (data.stockAlerts.length ? data.stockAlerts : [{ name: "No low stock items", stockQuantity: 0, lowStockThreshold: 0 }])
        .map((item, index) => `<div class="list-row warning" style="animation: fadeUp .3s ease ${index * 60}ms both"><span>${item.name}</span><strong>${item.stockQuantity} / ${item.lowStockThreshold}</strong></div>`).join("");
    document.getElementById("employeeActivity").closest(".feature-panel").classList.toggle("is-hidden", !canMonitorEmployees);
    if (canMonitorEmployees) {
        document.getElementById("employeeActivity").innerHTML = (data.employeeActivity.length ? data.employeeActivity : [])
            .map((item, index) => `<div class="list-row" style="animation: fadeUp .3s ease ${index * 60}ms both"><span>${item.employeeCode} ${item.fullName}<small>${item.role}</small></span><strong>${item.actionsToday} actions</strong></div>`).join("");
    }
    document.getElementById("dashboardNotifications").innerHTML = (data.notifications.length ? data.notifications : [])
        .map((item, index) => `<div class="list-row ${item.severity === "Warning" || item.severity === "Critical" ? "warning" : ""}" style="animation: fadeUp .3s ease ${index * 60}ms both"><span>${item.title}<small>${item.message}</small></span><strong>${item.severity}</strong></div>`).join("");
}

async function initInventory() {
    await loadSelects();
    await loadProducts();
    document.getElementById("reloadProducts").addEventListener("click", loadProducts);
    document.getElementById("productForm").addEventListener("submit", async event => {
        event.preventDefault();
        await api("/products", {
            method: "POST",
            body: JSON.stringify({
                name: productName.value,
                sku: sku.value,
                categoryId: Number(categoryId.value),
                supplierId: supplierId.value ? Number(supplierId.value) : null,
                costPrice: Number(costPrice.value),
                sellingPrice: Number(sellingPrice.value),
                stockQuantity: Number(stockQuantity.value),
                lowStockThreshold: Number(lowStockThreshold.value)
            })
        });
        event.target.reset();
        await loadProducts();
    });
}

async function loadSelects() {
    const [categories, suppliers] = await Promise.all([api("/categories"), api("/suppliers")]);
    categoryId.innerHTML = categories.map(c => `<option value="${c.id}">${c.name}</option>`).join("");
    supplierId.innerHTML = `<option value="">No supplier</option>${suppliers.map(s => `<option value="${s.id}">${s.name}</option>`).join("")}`;
}

async function loadProducts() {
    const query = document.getElementById("productSearch")?.value || "";
    const products = await api(`/products${query ? `?query=${encodeURIComponent(query)}` : ""}`);
    const table = document.getElementById("productsTable");
    if (table) {
        table.innerHTML = products.map(p => `
            <tr>
                <td>${p.name}</td><td>${p.sku}</td><td>${p.categoryName}</td>
                <td>${p.stockQuantity}${p.isLowStock ? " Low" : ""}</td><td>${money(p.sellingPrice)}</td>
            </tr>`).join("");
    }
    return products;
}

async function initPos() {
    posProducts = await loadProducts();
    renderProductTiles(posProducts);
    document.getElementById("posSearch").addEventListener("input", event => {
        const term = event.target.value.toLowerCase();
        renderProductTiles(posProducts.filter(p => p.name.toLowerCase().includes(term) || p.sku.toLowerCase().includes(term)));
    });
    document.getElementById("checkoutBtn").addEventListener("click", checkout);
}

function renderProductTiles(products) {
    productTiles.innerHTML = products.map(p => `
        <button class="product-tile" onclick="addToCart(${p.id})">
            <strong>${p.name}</strong><span>${p.sku}</span><span>${money(p.sellingPrice)} | Stock ${p.stockQuantity}</span>
        </button>`).join("");
}

function addToCart(productId) {
    const product = posProducts.find(p => p.id === productId);
    const existing = cart.find(i => i.productId === productId);
    if (existing) existing.quantity += 1;
    else cart.push({ productId, name: product.name, quantity: 1, unitPrice: product.sellingPrice });
    renderCart();
}

function renderCart() {
    cartItems.innerHTML = cart.map(item => `<div class="list-row"><span>${item.name} x ${item.quantity}</span><strong>${money(item.quantity * item.unitPrice)}</strong></div>`).join("");
    cartTotal.textContent = money(cart.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0));
}

async function checkout() {
    if (!cart.length) return;
    const sale = await api("/sales", {
        method: "POST",
        body: JSON.stringify({ paymentMethod: "Cash", items: cart.map(i => ({ productId: i.productId, quantity: i.quantity })) })
    });
    await openInvoice(sale.id);
    cart = [];
    renderCart();
    posProducts = await api("/products");
    renderProductTiles(posProducts);
}

async function openInvoice(saleId) {
    const invoiceHtml = await api(`/sales/${saleId}/invoice`);
    const invoiceWindow = window.open("", "_blank");
    if (!invoiceWindow) {
        throw new Error("Allow popups to open the invoice.");
    }

    invoiceWindow.document.open();
    invoiceWindow.document.write(invoiceHtml);
    invoiceWindow.document.close();
}

async function initReports() {
    document.querySelectorAll(".report-link").forEach(link => {
        link.addEventListener("click", event => {
            event.preventDefault();
            downloadReport(link.href, link.textContent.trim().toLowerCase().replaceAll(" ", "-"));
        });
    });
    await loadReports();
}

async function downloadReport(url, fallbackName) {
    const response = await fetch(url, { headers: { Authorization: `Bearer ${token()}` } });
    if (!response.ok) throw new Error("Report export failed");
    const blob = await response.blob();
    const objectUrl = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = objectUrl;
    anchor.download = fallbackName.includes("pdf") ? `${fallbackName}.pdf` : `${fallbackName}.csv`;
    anchor.click();
    URL.revokeObjectURL(objectUrl);
}

async function loadReports() {
    const sales = await api("/sales");
    salesTable.innerHTML = sales.map(s => `<tr><td>${s.invoiceNumber}</td><td>${new Date(s.saleDate).toLocaleString()}</td><td>${s.paymentMethod}</td><td>${money(s.totalAmount)}</td></tr>`).join("");
}

async function initSuppliers() {
    await loadSuppliers();
    supplierForm.addEventListener("submit", async event => {
        event.preventDefault();
        await api("/suppliers", {
            method: "POST",
            body: JSON.stringify({
                name: supplierName.value,
                contactPerson: contactPerson.value,
                phone: phone.value,
                email: supplierEmail.value,
                address: address.value
            })
        });
        event.target.reset();
        await loadSuppliers();
    });
}

async function loadSuppliers() {
    const suppliers = await api("/suppliers");
    suppliersTable.innerHTML = suppliers.map(s => `<tr><td>${s.name}</td><td>${s.contactPerson || ""}</td><td>${s.phone || ""}</td><td>${s.email || ""}</td></tr>`).join("");
}

async function initEmployees() {
    await Promise.all([loadEmployees(), loadLoginLogs()]);
    employeeForm.addEventListener("submit", async event => {
        event.preventDefault();
        await api("/users", {
            method: "POST",
            body: JSON.stringify({
                employeeCode: employeeCode.value,
                fullName: employeeName.value,
                email: employeeEmail.value,
                password: employeePassword.value,
                role: employeeRole.value
            })
        });
        event.target.reset();
        await loadEmployees();
    });
}

async function loadEmployees() {
    const employees = await api("/employees/activity");
    employeesTable.innerHTML = employees.map(e => `
        <tr>
            <td>${e.employeeCode} ${e.fullName}<br><small>${e.email}</small></td>
            <td><span class="status-pill">${e.role}</span></td>
            <td>${e.lastLoginAt ? new Date(e.lastLoginAt).toLocaleString() : "Never"}</td>
            <td>${e.auditActions}</td>
            <td>${e.failedLogins}</td>
        </tr>`).join("");
}

async function loadLoginLogs() {
    const logs = await api("/login-logs");
    loginLogsTable.innerHTML = logs.map(log => `
        <tr>
            <td>${log.employee || ""}</td>
            <td>${log.emailAttempted}</td>
            <td><span class="status-pill ${log.status === "Failed" ? "danger" : "success"}">${log.status}</span></td>
            <td>${new Date(log.loginAt).toLocaleString()}</td>
            <td>${log.logoutAt ? new Date(log.logoutAt).toLocaleString() : ""}</td>
            <td>${log.reason || ""}</td>
        </tr>`).join("");
}

async function loadAudit() {
    const logs = await api("/audit-logs");
    auditTable.innerHTML = logs.map(log => `
        <tr>
            <td>${new Date(log.createdAt).toLocaleString()}</td>
            <td>${log.userName || "System"}</td>
            <td><span class="status-pill">${log.actionType}</span></td>
            <td>${log.module}</td>
            <td>${log.entityName} #${log.entityId || ""}</td>
            <td>${log.description}</td>
        </tr>`).join("");
}

async function loadNotificationsPage() {
    const notifications = await api("/notifications");
    notificationsList.innerHTML = notifications.map(n => `
        <article class="notification-card ${n.severity.toLowerCase()}">
            <span>${n.type}</span>
            <h2>${n.title}</h2>
            <p>${n.message}</p>
            <small>${new Date(n.createdAt).toLocaleString()}</small>
        </article>`).join("");
}
