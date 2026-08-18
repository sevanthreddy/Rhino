import { getApiUrl } from "../config";

export const apiFetch = async (path, options = {}) => {
    let token = localStorage.getItem("token");
    let headers = { ...(options.headers || {}) };

    if (token) {
        headers.Authorization = `Bearer ${token}`;
    }

    let response = await fetch(getApiUrl(path), {
        ...options,
        headers,
    });

    if (response.status === 401) {
        console.log("refresh access token method started")
        const refreshToken = localStorage.getItem("refreshtoken");

        const refreshResponse = await fetch(
            getApiUrl(
                `/api/RegisterandLogin/refresh?accesstoken=${encodeURIComponent(token)}&refreshtoken=${encodeURIComponent(refreshToken)}`
            ),
            {
                method: "POST"
            }
        );

        if (refreshResponse.ok) {
            const data = await refreshResponse.json();
            console.log(data);

            // New access token
            const newAccessToken = data.newAccessToken;
            localStorage.setItem("token",newAccessToken);
            headers.Authorization = `Bearer ${newAccessToken}`;
            // Retry the original request
            response = await fetch(getApiUrl(path), {
                ...options,
                headers,
            });
        } else {
            // Refresh token is expired/invalid
            // Log the user out
            console.log("Session expired");
        }
    }

    return response;
};