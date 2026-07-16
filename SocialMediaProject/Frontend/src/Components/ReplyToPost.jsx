import React, { useEffect, useState } from 'react';
import PostCard from './PostCard';
import CreatePostCard from './CreatePostCard';
import '../Styles/Homepage.css';
import { Outlet, useLocation, useNavigate, useParams } from 'react-router-dom';
import SinglePost from './SinglePost';



 function ReplyToPost() {
    return (
        <div>
            <CreatePostCard></CreatePostCard>

        </div>
    );

 }

 export default ReplyToPost;


