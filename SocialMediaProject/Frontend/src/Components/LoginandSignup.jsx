import React, { useEffect, useState } from "react";
import "../Styles/LoginandSignup.css";
import { jwtDecode } from "jwt-decode";
import { useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import { login } from "../Slices/AuthSlice";
import { useGlobalContext } from "../context/GlobalContext";


function LoginandSignup() {

  const [username, setusername] =
    useState("");

  const [email, setemail] =
    useState("");

  const [password, setpassword] =
    useState("");

  const [loggedin, setloggedin] =
    useState(false);

  const [authmode, setauthmode] =
    useState("login");


  const navigate =
    useNavigate();

  const dispatch =
    useDispatch();


  const trial =
    useSelector(
      (state) => state.auth.username
    );


  const {
    apiFetch,
    setLoginToken,
    setUser,
    accessToken,
    authLoading
  } = useGlobalContext();


  // =========================
  // AUTH STATE
  // =========================

  useEffect(() => {

    if (accessToken) {

      setloggedin(true);

    }
    else {

      setloggedin(false);
    }

  }, [accessToken]);


  // =========================
  // LOGIN / REGISTER
  // =========================

  const handleOnSubmit = async (e) => {

    console.log(
      "Login/Register method started"
    );


    e.preventDefault();


    // =========================
    // LOGIN
    // =========================

    if (authmode === "login") {

      try {

        const response =
          await apiFetch(
            "/api/RegisterandLogin/login",
            {
              method: "POST",
              credentials: "include",
              headers: {
                "Content-Type":
                  "application/json"
              },

              body: JSON.stringify({

                Identifier:
                  email,

                Password:
                  password

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


        if (!response.ok) {

          console.log(
            "❌ Login failed:",
            data
          );

          alert(
            typeof data === "string"
              ? data
              : "Login failed"
          );

          return;
        }


        console.log(
          "✅ Login Successful"
        );


        if (!data.token) {

          console.log(
            "❌ Login response does not contain token"
          );

          alert(
            "Login failed"
          );

          return;
        }


        console.log(
          "🔥 LOGIN TOKEN:",
          data.token
        );


        // =========================
        // SET ACCESS TOKEN
        // =========================

        setLoginToken(
          data.token
        );


        // =========================
        // DECODE TOKEN
        // =========================

        const decodedtoken =
          jwtDecode(
            data.token
          );


        const decodedUsername =
          decodedtoken.username;


        console.log(
          "Username:",
          decodedUsername
        );


        // =========================
        // REDUX
        // =========================

        dispatch(
          login({

            username:
              decodedUsername,

            token:
              data.token

          })
        );


        // =========================
        // USER STATE
        // =========================

        setUser(data);


        // =========================
        // GO HOME
        // =========================

        navigate("/home");

      }
      catch (error) {

        console.error(
          "❌ Login error:",
          error
        );

        alert(
          "Unable to connect to server"
        );
      }

    }


    // =========================
    // REGISTER
    // =========================

    else {

      try {

        const response =
          await apiFetch(
            "/api/RegisterandLogin/register",
            {
              method: "POST",

              headers: {
                "Content-Type":
                  "application/json"
              },

              body: JSON.stringify({

                UserName:
                  username,

                Email:
                  email,

                Password:
                  password

              })
            }
          );


        if (response.ok) {

          console.log(
            "✅ Registration successful"
          );

          setauthmode("login");

        }
        else {

          console.log(
            "❌ Registration failed"
          );

          setauthmode("Create");
        }

      }
      catch (error) {

        console.error(
          "Registration error:",
          error
        );
      }
    }


    console.log(
      "login/register method ended"
    );
  };


  // =========================
  // SWITCH LOGIN / REGISTER
  // =========================

  const handlechoice = () => {

    if (authmode === "Create") {

      setauthmode("login");

    }
    else {

      setauthmode("Create");
    }
  };


  // =========================
  // WAIT FOR AUTH CHECK
  // =========================

  if (authLoading) {

    return (
      <div>
        Loading...
      </div>
    );
  }


  // =========================
  // UI
  // =========================

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

                setusername(
                  e.target.value
                );

              }}

              value={username}

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

            value={email}

            placeholder="Enter Email Address or Username"

            className="w-full border rounded-xl p-3 m-2 border-slate-800 outline-none focus:border-blue-500"

          />


          <input

            type="password"

            onChange={(e) => {

              setpassword(
                e.target.value
              );

            }}

            value={password}

            placeholder="Password"

            className="w-full border border-slate-800 rounded-xl p-3 m-2 outline-none focus:border-blue-500"

          />


          <button

            type="submit"

            className="rounded-2xl w-full p-2 m-2 bg-blue-700 hover:bg-blue-500"

          >

            {
              authmode === "Create"
                ? "Sign Up"
                : "Log In"
            }

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

            {
              authmode === "Create"
                ? "Log In"
                : "Create an Account"
            }

          </button>

        </div>

      </div>

    </div>
  );
}


export default LoginandSignup;