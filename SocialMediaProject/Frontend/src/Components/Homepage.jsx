import { useState, useEffect } from 'react';
import PostCard from './PostCard';
import CreatePostCard from './CreatePostCard';
import '../Styles/Homepage.css';
import { Outlet, useLocation, useNavigate, useParams } from 'react-router-dom';
import SinglePost from './SinglePost';


// 🔌 Homepage accepts 'onLogout' as a prop from App.jsx
function Homepage() {
  const [posts, setposts] = useState([]);
  const navigate = useNavigate();
  const location=useLocation();


  const fetchpostsfromdb = async () => {
    const token = localStorage.getItem("token");
    const response = await fetch("http://localhost:5040/api/Posts", {
      method: "GET",
      headers: { "Authorization": `Bearer ${token}` }
    });

    if (response.ok) {
      var data = await response.json();
      setposts(data);
    } else {
      console.log("issue in fetching posts");
    }
  }

  const handleClickPost = (id) => {
    navigate(`/home/post/${id}`);
  }

  const onlogout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("initials");
    navigate("/login");
  }
  
  const handlehomeclick = () => {
    navigate("/home");
  }
  
  useEffect(() => {
    fetchpostsfromdb();
  }, []);
  return (
    <div className='min-h-screen w-full '>
      <div className='grid grid-cols-12 gap-6 p-6'>
        <div className='col-span-3  flex flex-col sticky top-0 h-screen'>
          <button onClick={handlehomeclick} className='rounded-xl hover:bg-gray-100 p-3 font-bold'>Home</button>
          <button onClick={onlogout} className='rounded-xl hover:bg-gray-100 p-3 '>LogOut</button>
        </div>
        <div className='col-span-6'>
          {location.pathname.startsWith("/home/post/") === false && <CreatePostCard oncreate={fetchpostsfromdb}></CreatePostCard>}
          {location.pathname === "/home" &&  posts.map((everypost) => (
            <PostCard key={everypost.id} post={everypost} onClick={() => handleClickPost(everypost.id)}></PostCard>
          ))}
          <Outlet></Outlet>
        </div>
        <div className='col-span-3'></div>
      </div>
    </div>
  );

}
export default Homepage;