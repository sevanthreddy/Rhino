import React, { useEffect, useState } from 'react';
import '../Styles/LoginandSignup.css';
import Homepage from './Homepage';
import { jwtDecode } from 'jwt-decode';
import { useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from "react-redux";
import { login } from "../Slices/AuthSlice";
import { useGlobalContext } from "../context/GlobalContext";
import { apiFetch } from "../api/apiClient";


function LoginandSignup() {
  const [username, setusername] = useState("");
  const [email, setemail] = useState("");
  const [password, setpassword] = useState("");
  const [loggedin, setloggedin] = useState(false);
  const [authmode, setauthmode] = useState("login");
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const trial = useSelector((state) => state.auth.username);
  const { connection,setOnlineUsers,setConnection,setUser } = useGlobalContext();


  useEffect(() => {
    const savedtoken = localStorage.getItem("token");
    if (savedtoken) {
      setloggedin(true);
    } else {
      setloggedin(false);
    }
  }, []);

  const handleOnSubmit = async (e) => {
    e.preventDefault();
    if (authmode === "login") {
      const response = await apiFetch('/api/RegisterandLogin/login', {
        method: "POST",
        headers: {
          "content-type": "application/json",
        },
        body: JSON.stringify({
          Identifier: email,
          Password: password
        })
      });
      const contenttype = response.headers.get("content-type");
      var data = contenttype && contenttype.includes("application/json") ? await response.json() : response.text();
      if (response.ok) {
        console.log("Login Successfull");
        if (data.token) {
          localStorage.setItem("token", data.token);
          localStorage.setItem("refreshtoken",data.refreshToken);
          const decodedtoken = jwtDecode(data.token);
          console.log(decodedtoken);

          localStorage.setItem("userid",decodedtoken.userId);
          let username = decodedtoken.username;
          dispatch(login({
            username: username,
            token: data.token
          }));
          setUser(data);
          localStorage.setItem("initials", username.length >= 2
            ? username.substring(0, 2).toUpperCase()
            : username.toUpperCase());
          localStorage.setItem("username", username);
          navigate("/home");
        } else {
          alert("Login Failed");
        }
      }
    } else {

      const response = await apiFetch('/api/RegisterandLogin/register', {
        method: "POST",
        headers: {
          "content-type": "application/json"
        },
        body: JSON.stringify({
          UserName: username,
          Email: email,
          Password: password
        })
      });
      if (response.ok) {
        setauthmode("login");
      } else {
        setauthmode("Create");
      }
    }


  }

  const handlechoice = () => {
    if (authmode === "Create") {
      setauthmode("login");

    }
    else {
      setauthmode("Create");
    }
  }


  return (
    <div className="min-h-screen bg-slate-900 border">
      <div className="flex justify-center items-center  flex-col text-white mt-32">
        <h1 className='text-2xl'>Log in to Rhino</h1>
        <form onSubmit={handleOnSubmit} className='flex justify-center items-center flex-col text-white w-full max-w-sm'>
          {authmode === "Create" && <input onChange={(e) => {
            console.log("Typing username:", e.target.value);
            setusername(e.target.value); console.log("username")
          }} placeholder='Enter Username' className='w-full border rounded-xl p-3 m-2 border-slate-800 outline-none focus:border-blue-500'></input>}
          <input onChange={(e) => { setemail(e.target.value) }} placeholder='Enter Email Address or Username' className='w-full border rounded-xl p-3 m-2 border-slate-800 outline-none focus:border-blue-500'></input>
          <input onChange={(e) => { setpassword(e.target.value) }} placeholder='password' className='w-full border border-slate-800 rounded-xl p-3 m-2 outline-none focus:border-blue-500'></input>
          <button type='submit' className='rounded-2xl  w-full p-2 m-2 bg-blue-700 hover:bg-blue-500'>{authmode === "Create" ? "Sign Up" : "Log In"}</button>
        </form>
        <div className="flex items-center my-4 max-w-sm w-full">
          <div className="flex-1 border-t border-slate-500"></div>
          <p className="px-3 text-xs text-slate-500 font-semibold uppercase">Or</p>
          <div className="flex-1 border-t border-slate-500"></div>
        </div>
        <div>
          <button onClick={handlechoice} className='hover:underline text-blue-400 cursor-pointer'>{authmode === "Create" ? "Log In" : "Create an Account"}</button>
        </div>
      </div>
    </div>
  );
}

export default LoginandSignup;