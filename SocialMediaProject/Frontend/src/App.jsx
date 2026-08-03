import React, { useEffect, useState } from 'react';
import './App.css';
import Homepage from './Components/Homepage';
import { jwtDecode } from 'jwt-decode';
import { RouterProvider, Routes, Route, Navigate } from 'react-router-dom';
import LoginandSignup from './Components/LoginandSignup';
import PostCard from './Components/PostCard';
import SinglePost from './Components/SinglePost';
import ProfileView from './Components/ProfileView';
import ChatView from './Components/ChatView';
import SingleChat from './Components/SingleChat';


function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/login" replace />} />

      <Route path="/login" element={<LoginandSignup />} />

      <Route path="/home" element={<Homepage />}>
        <Route path="chat" element={<ChatView></ChatView>}>
          <Route path=":userid" element={<SingleChat></SingleChat>}></Route>

        </Route>

        <Route path="post/:id" element={<SinglePost />} />
        <Route path=":username" element={<ProfileView></ProfileView>} />
      </Route>
    </Routes>
  )


}

export default App;