import React, { useEffect, useState } from "react";
import { useParams,useNavigate } from "react-router-dom";
import PostCard from "./PostCard";
import CreatePostCard from "./CreatePostCard";
import { useGlobalContext } from "../context/GlobalContext";
import { useOutletContext } from "react-router-dom";


function SinglePost() {
    const { id } = useParams();

    const [post, setPost] = useState(null);
    const [replies, setreplies] = useState([]);
      const navigate = useNavigate();
      const {
          apiFetch
        } = useGlobalContext();
        const { handleCreatePost } = useOutletContext();


    useEffect(() => {
        const fetchPost = async () => {
            const response = await apiFetch(`/api/Posts/post/${id}`, {
                method: "GET",
            });

            if (response.ok) {
                const data = await response.json();
                setPost(data);
            }
        };

        fetchPost();
    }, [id]);

    const getallreplies = async () => {
        const response = await apiFetch(`/api/ReplyTo/Replies/${id}`, {
            
        });

        if (response.ok) {
            const data = await response.json();
            setreplies(data);
        }
    };

    useEffect(() => {
        getallreplies();
    }, [id]);

    const handleCreateReplies = () => {
        getallreplies();
    };




    return (
        <div>
            {post && <PostCard post={post} />}
            <CreatePostCard createpost={handleCreatePost} oncreate={handleCreateReplies} ></CreatePostCard>
            {replies && replies.map(r => <PostCard post={r}></PostCard>)}

        </div>
    );
}

export default SinglePost;