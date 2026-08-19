import React, { useEffect, useState } from "react";
import { useParams,useNavigate } from "react-router-dom";
import PostCard from "./PostCard";
import CreatePostCard from "./CreatePostCard";
import { apiFetch } from "../api/apiClient";


function SinglePost() {
    const { id } = useParams();

    const [post, setPost] = useState(null);
    const [replies, setreplies] = useState([]);
      const navigate = useNavigate();


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
            <CreatePostCard oncreate={handleCreateReplies} ></CreatePostCard>
            {replies && replies.map(r => <PostCard post={r}></PostCard>)}

        </div>
    );
}

export default SinglePost;