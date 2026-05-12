const API_BASE_URL = "/api";

function token() {
    return localStorage.getItem("bms_token");
}

async function api(endpoint, options = {}) {
    const headers = { "Content-Type": "application/json", ...(options.headers || {}) };
    if (token()) {
        headers.Authorization = `Bearer ${token()}`;
    }

    const response = await fetch(`${API_BASE_URL}${endpoint}`, { ...options, headers });
    if (!response.ok) {
        const error = await response.json().catch(() => ({ message: "Request failed" }));
        throw new Error(error.message || "Request failed");
    }

    const contentType = response.headers.get("content-type") || "";
    return contentType.includes("application/json") ? response.json() : response.text();
}

function money(value) {
    return new Intl.NumberFormat("en-MY", { style: "currency", currency: "MYR" }).format(value || 0);
}
