import React, { useEffect, useState } from 'react';

import '../Styles/LoginandSignup.css';

import { jwtDecode } from 'jwt-decode';

import { useNavigate } from 'react-router-dom';

import { useDispatch } from "react-redux";

import { login } from "../Slices/AuthSlice";

import { useGlobalContext } from "../context/GlobalContext";

import {
    apiFetch,
    setToken
} from "../api/apiClient";


function LoginandSignup() {

    const [username, setusername] = useState("");

    const [email, setemail] = useState("");

    const [password, setpassword] = useState("");

    const [loggedin, setloggedin] = useState(false);

    const [authmode, setauthmode] = useState("login");


    const navigate = useNavigate();

    const dispatch = useDispatch();


    const {
        setUser,
        accessToken,
        setaccessToken
    } = useGlobalContext();


    // ---------------------------------------------------
    // CHECK LOGIN STATE
    // ---------------------------------------------------

    useEffect(() => {

        if (accessToken) {

            setloggedin(true);

        } else {

            setloggedin(false);

        }

    }, [accessToken]);


    // ---------------------------------------------------
    // LOGIN / REGISTER
    // ---------------------------------------------------

    const handleOnSubmit = async (e) => {

        console.log(
            "Login/Register method started"
        );


        e.preventDefault();


        // =================================================
        // LOGIN
        // =================================================

        if (authmode === "login") {

            const response = await apiFetch(
                '/api/RegisterandLogin/login',
                {
                    method: "POST",

                    headers: {
                        "content-type":
                            "application/json",
                    },

                    body: JSON.stringify({

                        Identifier: email,

                        Password: password

                    })
                }
            );


            const contenttype =
                response.headers.get(
                    "content-type"
                );


            const data =
                contenttype &&
                contenttype.includes(
                    "application/json"
                )
                    ? await response.json()
                    : await response.text();


            if (response.ok) {

                console.log(
                    "Login Successful"
                );


                if (data.token) {

                    console.log(
                        "🔥 LOGIN TOKEN:",
                        data.token
                    );


                    // -----------------------------------------
                    // SET TOKEN IN GLOBAL CONTEXT
                    // -----------------------------------------

                    setaccessToken(
                        data.token
                    );


                    // -----------------------------------------
                    // SET TOKEN IN apiClient
                    // -----------------------------------------

                    setToken(
                        data.token
                    );


                    console.log(
                        "🔥 Token set in both GlobalContext and apiClient"
                    );


                    // -----------------------------------------
                    // DECODE TOKEN
                    // -----------------------------------------

                    const decodedtoken =
                        jwtDecode(
                            data.token
                        );


                    const username =
                        decodedtoken.username;


                    // -----------------------------------------
                    // REDUX
                    // -----------------------------------------

                    dispatch(
                        login({

                            username: username,

                            token: data.token

                        })
                    );


                    // -----------------------------------------
                    // USER
                    // -----------------------------------------

                    setUser(data);


                    // -----------------------------------------
                    // NAVIGATE
                    // -----------------------------------------

                    navigate("/home");

                } else {

                    alert(
                        "Login Failed"
                    );
                }

            } else {

                console.log(
                    "Login request failed"
                );

            }

        }

        // =================================================
        // REGISTER
        // =================================================

        else {

            const response = await apiFetch(
                '/api/RegisterandLogin/register',
                {
                    method: "POST",

                    headers: {
                        "content-type":
                            "application/json"
                    },

                    body: JSON.stringify({

                        UserName: username,

                        Email: email,

                        Password: password

                    })
                }
            );


            if (response.ok) {

                setauthmode("login");

            } else {

                setauthmode("Create");

            }
        }


        console.log(
            "login/register method ended"
        );
    };


    // ---------------------------------------------------
    // SWITCH LOGIN / REGISTER
    // ---------------------------------------------------

    const handlechoice = () => {

        if (authmode === "Create") {

            setauthmode("login");

        } else {

            setauthmode("Create");

        }
    };


    // ---------------------------------------------------
    // UI
    // ---------------------------------------------------

    return (

        <div className="min-h-screen bg-slate-900 border">

            <div className="flex justify-center items-center flex-col text-white mt-32">

                <h1 className="text-2xl">
                    Log in to Rhino
                </h1>


                <form
                    onSubmit={handleOnSubmit}
                    className="flex justify-center items-center flex-col text-white w-full max-w-sm"
                >

                    {authmode === "Create" && (

                        <input
                            onChange={(e) => {

                                console.log(
                                    "Typing username:",
                                    e.target.value
                                );

                                setusername(
                                    e.target.value
                                );

                            }}

                            placeholder="Enter Username"

                            className="w-full border rounded-xl p-3 m-2 border-slate-800 outline-none focus:border-blue-500"
                        />

                    )}


                    <input
                        onChange={(e) => {

                            setemail(
                                e.target.value
                            );

                        }}

                        placeholder="Enter Email Address or Username"

                        className="w-full border rounded-xl p-3 m-2 border-slate-800 outline-none focus:border-blue-500"
                    />


                    <input
                        onChange={(e) => {

                            setpassword(
                                e.target.value
                            );

                        }}

                        placeholder="password"

                        type="password"

                        className="w-full border border-slate-800 rounded-xl p-3 m-2 outline-none focus:border-blue-500"
                    />


                    <button
                        type="submit"
                        className="rounded-2xl w-full p-2 m-2 bg-blue-700 hover:bg-blue-500"
                    >

                        {authmode === "Create"
                            ? "Sign Up"
                            : "Log In"}

                    </button>

                </form>


                <div className="flex items-center my-4 max-w-sm w-full">

                    <div className="flex-1 border-t border-slate-500"></div>

                    <p className="px-3 text-xs text-slate-500 font-semibold uppercase">
                        Or
                    </p>

                    <div className="flex-1 border-t border-slate-500"></div>

                </div>


                <div>

                    <button
                        onClick={handlechoice}
                        className="hover:underline text-blue-400 cursor-pointer"
                    >

                        {authmode === "Create"
                            ? "Log In"
                            : "Create an Account"}

                    </button>

                </div>

            </div>

        </div>
    );
}


export default LoginandSignup;