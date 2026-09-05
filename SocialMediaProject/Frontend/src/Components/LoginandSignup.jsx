import React, { useEffect, useState } from "react";
import "../Styles/LoginandSignup.css";
import { jwtDecode } from "jwt-decode";
import { useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import { login } from "../Slices/AuthSlice";
import { useGlobalContext } from "../context/GlobalContext";
import { GoogleLogin } from "@react-oauth/google";


function LoginandSignup() {

  const [username, setusername] = useState("");
  const [email, setemail] = useState("");
  const [password, setpassword] = useState("");
  const [loggedin, setloggedin] = useState(false);
  const [authmode, setauthmode] = useState("login");
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const trial = useSelector((state) => state.auth.username);
  const [errors, setErrors] = useState({});
  const passwordConditions = {
    minLength: password.length >= 8,
    uppercase: /[A-Z]/.test(password),
    lowercase: /[a-z]/.test(password),
    number: /[0-9]/.test(password),
    special: /[!@#$%^&*(),.?":{}|<>]/.test(password)
  };

  const isStrongPassword =
    passwordConditions.minLength &&
    passwordConditions.uppercase &&
    passwordConditions.lowercase &&
    passwordConditions.number &&
    passwordConditions.special;


  const { apiFetch, setLoginToken, setUserId, setUser, accessToken, authLoading, setaccessToken } = useGlobalContext();


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

  const validateForm = () => {

    const newErrors = {};

    if (authmode === "Create") {

      // Username
      if (!username.trim()) {
        newErrors.username = "Username is required";
      }
      else if (username.trim().length < 3) {
        newErrors.username =
          "Username must be at least 3 characters";
      }
      else if (!/^[a-zA-Z0-9_]+$/.test(username.trim())) {
        newErrors.username =
          "Username can only contain letters, numbers and _";
      }


      // Email
      if (!email.trim()) {
        newErrors.email = "Email is required";
      }
      else if (
        !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())
      ) {
        newErrors.email =
          "Please enter a valid email address";
      }


      // Password
      const passwordError =
        validatePassword(password);

      if (passwordError) {
        newErrors.password = passwordError;
      }
    }
    else {

      // Login
      if (!email.trim()) {
        newErrors.email =
          "Email or username is required";
      }

      if (!password) {
        newErrors.password =
          "Password is required";
      }
    }

    setErrors(newErrors);

    return Object.keys(newErrors).length === 0;
  };

  const validatePassword = (value) => {

    if (!value) {
      return "Password is required";
    }

    if (value.length < 8) {
      return "Password must be at least 8 characters";
    }

    if (!/[A-Z]/.test(value)) {
      return "Password must contain an uppercase letter";
    }

    if (!/[a-z]/.test(value)) {
      return "Password must contain a lowercase letter";
    }

    if (!/[0-9]/.test(value)) {
      return "Password must contain a number";
    }

    if (!/[!@#$%^&*(),.?":{}|<>]/.test(value)) {
      return "Password must contain a special character";
    }

    return "";
  };


  // =========================
  // LOGIN / REGISTER
  // =========================

  const handleOnSubmit = async (e) => {

    console.log(
      "Login/Register method started"
    );


    e.preventDefault();
    if (!validateForm()) {
      return;
    }


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

        setUserId(decodedtoken.userId);


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
              data.token,

            profilePictureUrl:
              data.user?.profilePictureUrl || null

          })
        );
        console.log(data.user);

          


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
          " Login error:",
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
    setemail("");
      setpassword("");
      setusername("");

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

  //if (authLoading) {

    //return (
     // <div>
      //  Loading...
      //</div>
    //);
  //}

  const handleGoogleSuccess = async (credentialResponse) => {
    console.log("🚀 Google Login Token Received:", credentialResponse);
    try {
      const response = await apiFetch("/api/RegisterandLogin/google-login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ Token: credentialResponse.credential })
      });

      const contentType = response.headers.get("content-type");
      let data = contentType && contentType.includes("application/json") ? await response.json() : await response.text();

      if (response.ok) {
        console.log("🎯 Backend Google Login Success:", data);
        setaccessToken(data.token);
        navigate("/home");
        alert("Logged in with Google successfully!");
      } else {
        console.log("⚠️ Backend rejected Google token:", data);
        alert(`Google login failed on server: ${data}`);
      }
    } catch (err) {
      console.error("💥 Network error connecting to backend:", err);
    }

  }


  // =========================
  // UI
  // =========================

  return (

    <div className="min-h-screen bg-slate-900 border ">

      <div className="flex justify-center items-center flex-col text-white mt-32 -translate-y-10">

        <h1 className="text-2xl">
          Log in to Rhino
        </h1>


        <form onSubmit={handleOnSubmit} className="flex justify-center items-center flex-col text-white w-full max-w-sm" >

          {authmode === "Create" &&
            (<input onChange={(e) => { setusername(e.target.value); }} value={username} placeholder="Enter Username"
              className="w-full border rounded-xl p-3 m-2 border-slate-800 outline-none focus:border-blue-500" />)}
          <input
            onChange={(e) => { setemail(e.target.value); setErrors((prev) => ({ ...prev, email: "" })); }} value={email}
            placeholder={authmode === "Create" ? "Enter Email Address" : "Enter Email Address or Username"}
            className={`w-full border rounded-xl p-3 m-2 outline-none focus:border-blue-500 ${errors.email ? "border-red-500" : "border-slate-800"}`} />

          {errors.email && (
            <p className="text-red-400 text-sm px-3">
              {errors.email}
            </p>
          )}


          <div className="w-full">

            <input
              type="password"
              onChange={(e) => {

                const value = e.target.value;

                setpassword(value);

                if (authmode === "Create") {

                  const passwordError =
                    validatePassword(value);

                  setErrors((prev) => ({
                    ...prev,
                    password: passwordError
                  }));
                }

              }}
              value={password}
              placeholder="Password"
              className={`w-full border rounded-xl p-3 m-2 outline-none transition-colors duration-200 ${errors?.password
                ? "border-red-500 focus:border-red-500"
                : authmode === "Create" &&
                  password.length > 0 &&
                  isStrongPassword
                  ? "border-green-500 focus:border-green-500"
                  : "border-slate-800 focus:border-blue-500"
                }`}
            />

            {errors?.password && (
              <p className="text-red-400 text-sm px-3">
                {errors.password}
              </p>
            )}

          </div>


          <button type="submit" className="rounded-2xl w-full p-2 m-2 bg-blue-700 hover:bg-blue-500" >
            {authmode === "Create" ? "Sign Up" : "Log In"} </button>
          {/* Google login - only show on Login */}
          {authmode !== "Create" && (
            <>
              <div className="flex items-center w-full my-3">
                <div className="flex-1 border-t border-slate-700"></div>

                <span className="px-3 text-sm text-slate-400">
                  OR
                </span>

                <div className="flex-1 border-t border-slate-700"></div>
              </div>

              <GoogleLogin
                onSuccess={handleGoogleSuccess}
                onError={() => {
                  console.log("Google Login Failed");
                }}
                width="100"
              />
            </>)}

        </form>


        <div className="flex items-center my-4 max-w-sm w-full">

          <div className="flex-1 border-t border-slate-500"></div>

          <p className="px-3 text-xs text-slate-500 font-semibold uppercase">
            Or
          </p>

          <div className="flex-1 border-t border-slate-500"></div>

        </div>


        <div>
          <button onClick={handlechoice} className="hover:underline text-blue-400 cursor-pointer" >
            {authmode === "Create" ? "Log In" : "Create an Account"}
          </button>
        </div>

      </div>

    </div>
  );
}


export default LoginandSignup;