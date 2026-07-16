import React, { useEffect, useState } from 'react';
import './App.css';
import Homepage from './Components/Homepage';
import { jwtDecode } from 'jwt-decode';
import { RouterProvider, Routes, Route,Navigate } from 'react-router-dom';
import LoginandSignup from './Components/LoginandSignup';
import PostCard from './Components/PostCard';
import SinglePost from './Components/SinglePost';


function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/login" replace />} />

      <Route path="/login" element={<LoginandSignup />} />

      <Route path="/home" element={<Homepage />}>
        <Route path="post/:id" element={<SinglePost />} />
      </Route>
    </Routes>
  )


}

export default App;