import { getApiUrl } from "../config";

let token = null;
let updateGlobalToken = null;

// Setting the access token inside apiClient
export const setToken = (newToken) => {
    console.log("apiClient token being set:", newToken);
    token = newToken;
};

// Allows apiClient to update GlobalContext when refresh happens
export const setTokenUpdater = (callback) => {
    updateGlobalToken = callback;
};

// Get current token if needed elsewhere
export const getToken = () => {
    return token;
};

export const apiFetch = async (path, options = {}) => {

    console.log("apiFetch token:", token);

    let headers = {
        ...(options.headers || {})
    };

    // Add access token to request
    if (token) {
        headers.Authorization = `Bearer ${token}`;
    }

    let response = await fetch(getApiUrl(path), {
        ...options,
        headers,
        credentials: "include"
    });

    /*
     * We are leaving your refresh logic here for now.
     * Later we can improve the refresh flow.
     */
    if (response.status === 401) {

        console.log("refresh access token api started");

        const refreshResponse = await fetch(
            getApiUrl("/api/RegisterandLogin/refresh"),
            {
                method: "POST",
                credentials: "include"
            }
        );

        console.log(
            "refresh response status:",
            refreshResponse.status
        );

        if (refreshResponse.ok) {

            const data = await refreshResponse.json();

            console.log("refresh data:", data);

            const newAccessToken = data.newAccessToken;

            // Update apiClient token
            token = newAccessToken;

            console.log(
                "apiClient token updated after refresh:",
                token
            );

            // Update GlobalContext
            if (updateGlobalToken) {
                updateGlobalToken(newAccessToken);
            }

            // Retry original request
            headers.Authorization = `Bearer ${newAccessToken}`;

            response = await fetch(getApiUrl(path), {
                ...options,
                headers,
                credentials: "include"
            });

        } else {

            console.log("Session expired");

            token = null;

            if (updateGlobalToken) {
                updateGlobalToken(null);
            }
        }
    }

    return response;
};