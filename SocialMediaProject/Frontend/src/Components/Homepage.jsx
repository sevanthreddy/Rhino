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
import { FaHippo } from "react-icons/fa";
import { BlockBlobClient } from "@azure/storage-blob";







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
  const [uploadProgress, setUploadProgress] = useState(0);
  const [isUploading, setIsUploading] = useState(false);
  const { unreadMessagesCount, setUnreadMessagesCount, setLatestMessage, unreadnotificationsCount, setunreadnotificationsCount, accessToken, setaccessToken, apiFetch } = useGlobalContext();

  useEffect(() => {
    const setAppHeight = () => {
      const vh = window.visualViewport ? window.visualViewport.height : window.innerHeight;
      document.documentElement.style.setProperty('--app-height', `${vh}px`);
    };

    setAppHeight();
    window.visualViewport?.addEventListener('resize', setAppHeight);
    window.addEventListener('resize', setAppHeight);

    return () => {
      window.visualViewport?.removeEventListener('resize', setAppHeight);
      window.removeEventListener('resize', setAppHeight);
    };
  }, []);

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
    if (!accessToken) {
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

    try {
      // -----------------------------------------
      // 1. Separate images and videos
      // -----------------------------------------

      const images = selectedFiles?.filter(
        file => file.type.startsWith("image/")
      ) || [];

      const videos = selectedFiles?.filter(
        file => file.type.startsWith("video/")
      ) || [];


      // -----------------------------------------
      // 2. Upload videos directly to Azure Blob
      // -----------------------------------------

      const uploadedVideos = [];

      for (const video of videos) {

        console.log("🎥 Requesting SAS for:", video.name);

        const params = new URLSearchParams({
          fileName: video.name,
          contentType: video.type
        });

        const sasResponse = await apiFetch(
          `/api/Posts/upload-sas?${params.toString()}`,
          {
            method: "GET"
          }
        );

        if (!sasResponse.ok) {
          console.error(
            "❌ Failed to get SAS:",
            await sasResponse.text()
          );

          throw new Error(
            "Failed to get video upload permission"
          );
        }


        const sasData = await sasResponse.json();

        console.log("✅ SAS received:", sasData);


        // -----------------------------------------
        // 3. Browser → Azure Blob directly
        // -----------------------------------------

        const blockBlobClient = new BlockBlobClient(
          sasData.uploadUrl
        );
        setIsUploading(true);
        setUploadProgress(0);

        await blockBlobClient.uploadData(video, {
          blobHTTPHeaders: {
            blobContentType: video.type
          }, onProgress: (progress) => {
            const percentage =
              Math.round((progress.loadedBytes / video.size) * 100);

            setUploadProgress(percentage);

            console.log(
              `🎥 Upload progress: ${percentage}%`
            );
          }
        });

        setUploadProgress(100);
        setIsUploading(false);

        console.log("✅ Video uploaded successfully");


        console.log( "✅ Video uploaded:", sasData.fileName );


        // -----------------------------------------
        // 4. Store metadata
        // -----------------------------------------

        uploadedVideos.push({
          blobName: sasData.fileName,
          contentType: video.type,
          fileSize: video.size
        });
      }


      // -----------------------------------------
      // 5. Create FormData
      // -----------------------------------------

      const formData = new FormData();

      formData.append(
        "Content",
        content
      );


      // -----------------------------------------
      // 6. Add ONLY images
      // -----------------------------------------

      images.forEach((image) => {

        formData.append(
          "Media",
          image
        );

      });


      // -----------------------------------------
      // 7. Add video metadata
      // -----------------------------------------

      if (uploadedVideos.length > 0) {

        formData.append(
          "Videos",
          JSON.stringify(uploadedVideos)
        );

      }


      // -----------------------------------------
      // 8. Get PostId
      // -----------------------------------------

      const postId =
        location.pathname.split("/home/post/")[1];


      if (postId) {

        formData.append(
          "PostId",
          postId
        );

      }


      // -----------------------------------------
      // 9. Create Post
      // -----------------------------------------

      if (mode === "Post") {

        const response = await apiFetch(
          "/api/Posts/create",
          {
            method: "POST",
            body: formData
          }
        );


        if (response.ok) {

          console.log(
            "✅ Post created successfully"
          );

          fetchpostsfromdb();

        } else {

          console.log(
            await response.text()
          );

        }

      }


      // -----------------------------------------
      // 10. Create Reply
      // -----------------------------------------

      else {

        if (selectedpost) {

          formData.append(
            "PostId",
            selectedpost.id
          );

        } else {

          formData.append(
            "PostId",
            postId
          );

        }


        const response = await apiFetch(
          "/api/ReplyTo/post",
          {
            method: "POST",
            body: formData
          }
        );


        if (response.ok) {

          console.log(
            "✅ Reply successful"
          );

        } else {

          console.log(
            await response.text()
          );

        }
      }

    } catch (error) {

      console.error(
        "💥 Create post failed:",
        error
      );
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

  const UpdatePostAfterLike = (postId, likeCount, isLiked) => {
    setposts(prevPosts =>
      prevPosts.map(post =>
        post.id === postId
          ? {
            ...post,
            likeCount: likeCount,
            isLiked: isLiked
          }
          : post
      )
    );
  };

  return (
    <div className='h-[var(--app-height,100dvh)] w-full overflow-hidden bg-[#FFF7E8]'>
      <div className='grid grid-cols-1 md:grid-cols-12!  h-full min-h-0'>
        {(!isMobile || !location.pathname.startsWith("/home/chat/")) && (
          <div className={`bg-[#FFFDF8]   shadow-lg m-1 fixed bottom-0 left-0 right-0 md:static! flex flex-row justify-around md:flex-col! md:justify-start! rounded-xl ${fold ? "col-span-1" : "col-span-3"}`}>
            <div className='flex  flex-row items-center hover:bg-gray-100 p-3'>
              <FaHippo className="text-[#FF8A00] text-2xl mr-2" />
              {fold == false && <span className="hidden md:block! bg-gradient-to-r from-[#FFB300] to-[#FF7A00] bg-clip-text text-transparent font-bold "> Rhino </span>}
            </div>
            <div onClick={handlehomeclick} className='flex  flex-row items-center hover:bg-gray-100 p-3'>
              <MdHome className='mr-2 shrink-0'></MdHome>
              {fold == false && <button className='hidden md:block! rounded-xl font-bold'>Home</button>}
            </div>
            <div onClick={onlogout} className='flex flex-row items-center hover:bg-gray-100 p-3'>
              <MdLogout className='mr-2 shrink-0'></MdLogout>
              {fold == false && <button className='hidden md:block! rounded-xl hover:bg-gray-100'>LogOut</button>}
            </div>
            <div onClick={handleChatClick} className='flex flex-row items-center hover:bg-gray-100 p-3 relative'>
              <div className="relative inline-block">
                <HiOutlineChatAlt2 className="mr-2 shrink-0" />

                <div className="absolute -top-3 -right-2 bg-indigo-500 text-white text-[9px] rounded-full min-w-5  flex items-center justify-center">
                  {unreadMessagesCount > 0 ? unreadMessagesCount : ""}
                </div>
              </div>
              {fold == false && <button className='hidden md:block! rounded-xl hover:bg-gray-100'>Chat</button>}

            </div>
            <div onClick={handleNotificationsClick} className='flex flex-row items-center hover:bg-gray-100 p-3'>
              <div className='relative'>
                <IoNotificationsOutline className='mr-2 shrink-0'></IoNotificationsOutline>
                <div className="absolute -top-3 -right-2 bg-indigo-500 text-white text-[9px] rounded-full min-w-5  flex items-center justify-center">
                  {unreadnotificationsCount > 0 ? unreadnotificationsCount : ""}
                </div>
              </div>
              {fold == false && <button className='hidden md:block! rounded-xl hover:bg-gray-100'>Notifications</button>}
            </div>
          </div>)}
        <div className={`${fold ? "col-span-11" : "col-span-6"} h-full  min-h-0 overflow-y-auto  p-1`}>
          {fold == false && location.pathname === "/home" && <CreatePostCard oncreate={fetchpostsfromdb} createpost={handleCreatePost} uploadProgress={uploadProgress}
    isUploading={isUploading}></CreatePostCard>}
          {fold == false && location.pathname === "/home" && posts.map((everypost) => (
            <PostCard key={everypost.id} onProfileClick={handleProfileClick} oncommentclick={handleCommentClick} post={everypost} onClick={() => handleClickPost(everypost.id)} onLikeUpdated={UpdatePostAfterLike}></PostCard>
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