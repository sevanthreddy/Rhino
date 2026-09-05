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
            state.profilePictureUrl = action.payload.profilePictureUrl || null;
        },
        logout(state) {

            state.username = "";

            state.token = "";

            state.isLoggedIn = false;
            state.profilePictureUrl = null;

        }
    }

});

export const { login, logout } = authSlice.actions;

export default authSlice.reducer;