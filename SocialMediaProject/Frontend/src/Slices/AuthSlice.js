import { createSlice } from "@reduxjs/toolkit";

const initialState = {
    username: "",
    token: "",
    isLoggedIn: false
};

const authSlice = createSlice({
    name: "auth",
    initialState,
    reducers: {
        login(state, action) {
            state.username = action.payload.username;
            state.token = action.payload.token;
            state.isLoggedIn = action.payload.isLoggedIn;
        },
        logout(state) {

            state.username = "";

            state.token = "";

            state.isLoggedIn = false;

        }
    }

});

export const { login, logout } = authSlice.actions;

export default authSlice.reducer;