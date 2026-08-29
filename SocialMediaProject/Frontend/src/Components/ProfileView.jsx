import React, { useEffect, useState } from 'react';
import { useParams,useNavigate } from 'react-router-dom';
import EditProfileView from './EditProfileView';
import PostCard from './PostCard';
import { useOutletContext } from "react-router-dom";
import { MdArrowBack } from "react-icons/md";
import { getAssetUrl } from "../config";
import { useGlobalContext } from "../context/GlobalContext";

function ProfileView() {
    const { handleCommentClick, handleProfileClick, handleClickPost } = useOutletContext();
    const { username } = useParams();
    const [mode, setmode] = useState("Follow");
    const [profile, setprofile] = useState(null);
    const totalposts = profile?.totalposts;
    const followercount = profile?.followercount;
    const followingcount = profile?.followingcount;
    const followinfo = profile?.followinfo;
    const About = profile?.about;
    const imageurl = profile?.profileImage;
    const [showeditpf, setshoweditpf] = useState(false);
    const [clickedTab, setclickedTab] = useState("Posts");
    const [posts, setposts] = useState(null);
    const navigate=useNavigate();
    const { apiFetch } = useGlobalContext();


    useEffect(() => {
        getUserFollowInfo()
    }, []);

    useEffect(() => {
        getAllPosts();

    }, [clickedTab]);

    const handleFollowClick = async () => {
        if (username == localStorage.getItem("username")) {
            setmode("Edit Profile");
            setshoweditpf(true);
            return;
        }
        const response = await apiFetch(`/api/Follow/${username}`, {
            method: "POST"
        });
        if (response.ok) {
            console.log("response is 200")
            if (mode === "Follow") {
                setmode("Unfollow");

            } else {
                setmode("Follow");
            }

        } else {
            console.log("follow unsuccessfull");

        }
    }

    const getUserFollowInfo = async () => {

        var response = await apiFetch(`/api/Follow/${username}`, {
            method: "GET"
        });
        if (response.ok) {
            var data = await response.json();
            setprofile(data);
            console.log(localStorage.getItem("username"), data);
            if (username == localStorage.getItem("username")) {
                setmode("Edit Profile");
                return;
            }
            if (data.followinfo) {
                setmode("Unfollow")
            } else {
                setmode("Follow");
            }


        } else {
            setmode("Follow");
        }
    }

    const getAllPosts = async () => {
        const response = await apiFetch(`/api/Follow/${username}/${clickedTab}`, {
            method: "GET"
        });
        if (response.ok) {
            var data = await response.json();
            setposts(data);

        }
    }

    const handlepostclick = (post) => {
        handleClickPost(post.id);

    }

    const handlebackbuttonclick = ()=>{
        navigate(-1);

    }

    return (
        <div>
            <div className='w-full '>
                <div className='flex flex-row sticky top-0 bg-white/70 z-10 backdrop-blur-md'>{/*Top header Section showing username,numberofposts, */}

                    <div className='flex items-center rounded-xl mr-4'>
                        <button onClick={handlebackbuttonclick} className=" flex items-center justify-center rounded-full hover:bg-gray-200 cursor-pointer p-2">
                            <MdArrowBack size={16} />
                        </button>
                    </div>
                    <div className='text-sm  flex flex-col mb-1'>
                        <div className="text-sm ">{username}</div>
                        <div className="text-[10px] text-gray-500 ">{totalposts} posts</div>
                    </div>
                </div>
                <div className='h-32 '>{/*Main Image in Profile view*/}
                    {imageurl && <img src={getAssetUrl(`/uploads/${imageurl}`)} className='w-full h-32 object-cover overflow-hidden'></img>}

                </div>
                <div className='flex mt-4'>{/*bar for having follow button and other stuff*/}
                    {mode && <button onClick={handleFollowClick} className='rounded-xl bg-black text-white text-sm p-1 ml-auto cursor-pointer'>{mode}</button>}

                </div>
                <div>{/*About Section*/}
                    {About}

                </div>
                <div className="flex mt-1 mb-1">{/*bar showing follower count and following count*/}
                    <div className="text-sm mr-1">
                        <span>{followingcount + " "}</span>
                        <span className="text-gray-500">Following</span>
                    </div>
                    <div className="text-sm">
                        <span>{followercount + " "}</span>
                        <span className="text-gray-500">Followers</span>
                    </div>
                </div>
                <div className="flex justify-around mt-4 text-gray-500">
                    <button className={`hover:bg-gray-100 w-full p-2 border-b-2 cursor-pointer ${clickedTab === "Posts" ? "border-blue-500" : "border-transparent"}`} onClick={() => setclickedTab("Posts")} > Posts </button>
                    <button className={`hover:bg-gray-100 w-full p-2 border-b-2 cursor-pointer ${clickedTab === "Replies" ? "border-blue-500" : "border-transparent"}`} onClick={() => setclickedTab("Replies")} > Replies </button>
                    <button className={`hover:bg-gray-100 w-full p-2 border-b-2 cursor-pointer ${clickedTab === "Media" ? "border-blue-500" : "border-transparent"}`} onClick={() => setclickedTab("Media")} > Media </button>
                </div>
                <div>{/*dynamic section for showing either posts or replies or media based on user selection.*/}
                    {posts &&
                        posts.map((eachpost) => {
                            return <PostCard post={eachpost} oncommentclick={handleCommentClick} onProfileClick={handleProfileClick} onClick={() => handlepostclick(eachpost)}></PostCard>
                        })
                    }

                </div>
                {showeditpf && <EditProfileView onclose={() => { setshoweditpf(false); }}></EditProfileView>}
            </div>
        </div>
    );

}

export default ProfileView;