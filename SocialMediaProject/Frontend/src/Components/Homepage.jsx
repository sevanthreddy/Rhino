import { useState, useEffect } from 'react';
import PostCard from './PostCard';
import CreatePostCard from './CreatePostCard';
import '../Styles/Homepage.css';
import { Outlet, useLocation, useNavigate, useParams } from 'react-router-dom';
import SinglePost from './SinglePost';
import CommentCard from './Commentcard';
import { MdHome, MdChat, MdLogout } from "react-icons/md";
import { HiOutlineChatAlt2 } from "react-icons/hi";
import { useGlobalContext } from "../context/GlobalContext";
import { useMediaQuery } from "react-responsive";
import { IoNotificationsOutline } from "react-icons/io5";








function Homepage() {
  const [posts, setposts] = useState([]);
  const navigate = useNavigate();
  const location = useLocation();
  const [isCommentClicked, setisCommentClicked] = useState(false);
  const [selectedpost, setselectedpost] = useState(null);
  const [mode, setmode] = useState("Post");
  const [fold, setfold] = useState(false);
  const { connection, setOnlineUsers, setConnection, setUser } = useGlobalContext();
  const isMobile = useMediaQuery({ maxWidth: 767 });
  const { unreadMessagesCount, setUnreadMessagesCount, setLatestMessage, notifications, accessToken, setaccessToken,apiFetch } = useGlobalContext();


  const fetchpostsfromdb = async () => {
    const response = await apiFetch(('/api/Posts'), {
      method: "GET",

    });

    if (response.ok) {
      var data = await response.json();
      console.log("fetched posts from db", data);
      setposts(data);
    } else {
      console.log("issue in fetching posts");
    }
  }

  const handleClickPost = (id) => {
    navigate(`/home/post/${id}`);
  }

  const onlogout = async () => {
    if (connection) {
      console.log("connection close for signalR");
      await connection.stop();
    }
    setOnlineUsers(new Set());
    setUser(null);

    localStorage.removeItem("initials");
    setLatestMessage(null);
    setUnreadMessagesCount(0);
    setConnection(null);
    const response = await apiFetch(('/api/RegisterandLogin/logout'), {
      method: "POST",
      credentials: "include"
    });
    if (response.ok) {
      console.log("logout successfull");
    }
    setaccessToken(null);
    navigate("/login");
  }

  const handlehomeclick = () => {
    setfold(false);
    navigate("/home");
  }

  useEffect(() => {
    if(!accessToken){
      return
    }
    fetchpostsfromdb();
    setfold(location.pathname.startsWith("/home/chat"));
  }, [accessToken]);

  const handleCommentClick = (post) => {
    setisCommentClicked(true);
    setselectedpost(post);
  }

  const handleCreatePost = async (e, mode, content, selectedFiles) => {
    e.preventDefault();

    const formData = new FormData();
    console.log(content)
    formData.append("Content", content);

    if (selectedFiles != undefined) {
      // Append each image separately
      selectedFiles.forEach((file) => {
        formData.append("Images", file);
      });

    }


    // Debug
    for (const [key, value] of formData.entries()) {
      console.log(key, value);
    }
    const postId = location.pathname.split("/home/post/")[1];
    if (postId != null || postId != undefined) {
      formData.append("PostId", postId);
    } else {
    }

    if (mode === "Post") {
      const response = await apiFetch('/api/Posts/create', {
        method: "POST",
        body: formData,
      });

      if (response.ok) {
        fetchpostsfromdb();
      } else {
        console.log(await response.text());
      }
    } else {
      if (selectedpost) {
        formData.append("PostId", selectedpost.id);
      } else {
        formData.append("PostId", postId);
      }


      const response = await apiFetch('/api/ReplyTo/post', {
        method: "POST",
        body: formData,
      });

      if (response.ok) {
        console.log("reply successfull");
      } else {
        console.log(await response.text());
      }
    }

  };

  const handleclosecommentmodal = () => {
    setisCommentClicked(false);
  }

  const handleProfileClick = (username) => {
    navigate(`/home/${username}`);
  }

  const handleChatClick = () => {
    setfold(true);
    navigate("/home/chat");
  }

  const handleNotificationsClick = () => {

    navigate("/home/notifications");
  }

  return (
    <div className='h-screen w-full overflow-hidden'>
      <div className='grid grid-cols-1 md:grid-cols-12!  h-full '>
        {(!isMobile || !location.pathname.startsWith("/home/chat/")) && (<div className={`fixed bottom-0 left-0 right-0 md:static! flex flex-row justify-around md:flex-col! md:justify-start! rounded-xl ${fold ? "col-span-1" : "col-span-3"}`}>
          <div onClick={handlehomeclick} className='flex  flex-row items-center hover:bg-gray-100 p-3'>
            <MdHome className='mr-2 shrink-0'></MdHome>
            {fold == false && <button className='rounded-xl font-bold'>Home</button>}
          </div>
          <div onClick={onlogout} className='flex flex-row items-center hover:bg-gray-100 p-3'>
            <MdLogout className='mr-2 shrink-0'></MdLogout>
            {fold == false && <button className='rounded-xl hover:bg-gray-100'>LogOut</button>}
          </div>
          <div onClick={handleChatClick} className='flex flex-row items-center hover:bg-gray-100 p-3 relative'>
            <div className="relative inline-block">
              <HiOutlineChatAlt2 className="mr-2 shrink-0" />

              <div className="absolute -top-3 -right-2 bg-indigo-500 text-white text-[9px] rounded-full min-w-5  flex items-center justify-center">
                {unreadMessagesCount > 0 ? unreadMessagesCount : ""}
              </div>
            </div>
            {fold == false && <button className='rounded-xl hover:bg-gray-100'>Chat</button>}

          </div>
          <div onClick={handleNotificationsClick} className='flex flex-row items-center hover:bg-gray-100 p-3'>
            <div className='relative'>
              <IoNotificationsOutline className='mr-2 shrink-0'></IoNotificationsOutline>
              <div className="absolute -top-3 -right-2 bg-indigo-500 text-white text-[9px] rounded-full min-w-5  flex items-center justify-center">
                {notifications.length > 0 ? notifications.length : ""}
              </div>
            </div>
            {fold == false && <button className='rounded-xl hover:bg-gray-100'>Notifications</button>}
          </div>
        </div>)}
        <div className={`${fold ? "col-span-11" : "col-span-6"} h-full  overflow-y-auto`}>
          {fold == false && location.pathname === "/home" && <CreatePostCard oncreate={fetchpostsfromdb} createpost={handleCreatePost}></CreatePostCard>}
          {fold == false && location.pathname === "/home" && posts.map((everypost) => (
            <PostCard key={everypost.id} onProfileClick={handleProfileClick} oncommentclick={handleCommentClick} post={everypost} onClick={() => handleClickPost(everypost.id)}></PostCard>
          ))}
          <Outlet context={{ handleCreatePost, handleCommentClick, handleProfileClick, handleClickPost }}></Outlet>
        </div>
        {!location.pathname.startsWith("/home/chat") && <div className='col-span-3'></div>}
      </div>
      {isCommentClicked && <CommentCard showoriginalpost={selectedpost.content} oncreatecomment={handleCreatePost} onclose={handleclosecommentmodal}></CommentCard>}
    </div>
  );

}
export default Homepage;