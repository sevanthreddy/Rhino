import React, { useEffect, useState } from 'react';
import { FiHeart } from "react-icons/fi";
import { useNavigate, useParams } from 'react-router-dom';


function PostCard({ post,onClick }) {
  const { id, username, initials, timeAgo, content, avatarColor = 'bg-blue-500', likeCount, isLiked } = post;
  var imagesRelatedtoPost = post.imagesRelatedtoPost;
  const [likecountofpost, setlikecountofpost] = useState(likeCount);
  const [liked, setliked] = useState(isLiked);
 

  const handleClickLike = async (e) => {
    e.stopPropagation();
    console.log("entered click like function", post.id);
    var token = localStorage.getItem("token");
    console.log(post.id);
    const response = await fetch(`http://localhost:5040/api/Posts/like/${post.id}`, {
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
  return (
    <div>
      <div  onClick={onClick} className='flex hover:bg-gray-100 border border-gray-200 p-2'>
        {/* Profile Header Row */}
        <div className='flex'>
          <div className={`w-8 h-8 rounded-full ${avatarColor} flex items-center justify-center text-white font-bold`}>{initials}</div>
        </div>
        <div className='flex flex-col ml-1 '>
          <div className=' h-8'>{username}</div>
          <div>
            <div className=''>{content}</div>
            {imagesRelatedtoPost &&
              imagesRelatedtoPost.map((everyimage) => {
                return (
                  <img
                    key={everyimage}
                    src={`http://localhost:5040/${everyimage}`}
                    alt="image related to the post"
                    className="w-full rounded-lg mt-2 mb-2"
                  />
                );
              })}
          </div>
          <div className='flex m-2'>
            <div className='flex items-center'>{/*For Likes*/}
              <FiHeart onClick={handleClickLike} className={`text-sm cursor-pointer ${liked ? "fill-red-500 text-red-500" : "hover:text-red-500"}`} />
              <p className='ml-1 text-sm'>{likecountofpost}</p>

            </div>
          </div>
        </div>
      </div>
    </div>

  );
}

export default PostCard;