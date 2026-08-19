import React, { useEffect, useState } from 'react';
import { FiHeart, FiShare2, FiSend, FiTrendingUp } from "react-icons/fi";
import { FaRegComment } from "react-icons/fa";
import { useNavigate, useParams } from 'react-router-dom';
import CommentCard from './Commentcard';
import { getAssetUrl } from '../config';
import { apiFetch } from '../api/apiClient';
import { useGlobalContext } from "../context/GlobalContext";


function PostCard({ post, onProfileClick, oncommentclick, onClick }) {
  const { id, username, initials, timeAgo, content, avatarColor = 'bg-blue-500', likeCount, isLiked,profileImage } = post;
  var imagesRelatedtoPost = post.imagesRelatedtoPost;
  const [likecountofpost, setlikecountofpost] = useState(likeCount);
  const [liked, setliked] = useState(isLiked);
  const [commentclick, setcommentclick] = useState(false);
  const { accessToken } = useGlobalContext();


  const handleClickLike = async (e) => {
    e.stopPropagation();
    console.log("entered click like function", post.id);
    const token = accessToken;
    console.log(post.id);
    const response = await apiFetch(`/api/Posts/like/${post.id}`, {
      method: "POST",
      headers: { "content-type": "application/json", "Authorization": `Bearer ${token}` },
    });
    var data = await response.json()
    console.log(data);
    if (response.ok) {
      setlikecountofpost(data.likeCount);
      liked === false ? setliked(true) : setliked(false);
    }

  }

  const handleCommentClick = async (e) => {
    e.stopPropagation();
    oncommentclick(post);
  }

  const handleshare = async (e) => {
    try {
      e.stopPropagation();
      if (navigator.share) {
        await navigator.share({
          title: "Post",
          text: "Check Out this Post",
          url: getApiUrl(`/api/home/post/${post.id}`)
        })
      } else {
        console.log("Share not supported");
      }
    } catch(err) {
      console.log("Share cancelled or failed", err);
    }

  }

  const handleProfileClick = (e) => {
    e.stopPropagation();
    onProfileClick(username);
  }

  return (
    <div>
      <div onClick={onClick} className='flex hover:bg-gray-100 border border-gray-200 p-2'>
        {/* Profile Header Row */}
        <div className='flex'>
          <div onClick={handleProfileClick} className={`w-8 h-8 rounded-full ${avatarColor} flex items-center justify-center text-white font-bold cursor-pointer`}>
            {profileImage ? (
              <img src={getAssetUrl(`/Uploads/${profileImage}`)} className="w-full h-full rounded-full" />
            ) : (
              <span>{initials}</span>
            )}
          </div>
        </div>
        <div className='flex flex-col ml-1  w-full cursor-pointer'>
          <div className=' h-8'>{username}</div>
          <div>
            <div className=''>{content}</div>
            {imagesRelatedtoPost &&
              imagesRelatedtoPost.map((everyimage) => {
                return (
                  <img
                    key={everyimage}
                    src={getAssetUrl(`/${everyimage}`)}
                    alt="image related to the post"
                    className="w-full rounded-lg mt-2 mb-2"
                  />
                );
              })}
          </div>
          <div className='flex mt-2  w-full justify-between '>
            <div className='flex items-center hover:text-red-500'>{/*For Likes*/}
              <FiHeart onClick={handleClickLike} className={`text-sm cursor-pointer ${liked ? "fill-red-500 text-red-500" : "hover:text-red-500"}`} />
              <p className='ml-1 text-sm'>{likecountofpost}</p>
            </div>
            <div className='flex items-center hover:text-blue-500'>
              <FaRegComment onClick={handleCommentClick} className='text-sm cursor-pointer '></FaRegComment>
              <p className='ml-1 text-sm'>0</p>
            </div>
            <div className='flex items-center hover:text-blue-500'>
              <FiShare2 className='text-sm cursor-pointer hover:text-blue-500' onClick={handleshare}></FiShare2>
            </div>
            <div className='flex items-center'>
              <FiTrendingUp className='text-sm cursor-pointer'></FiTrendingUp>
              <p className='ml-1 text-sm'>0</p>
            </div>
          </div>
        </div>
      </div>
    </div>

  );
}

export default PostCard;