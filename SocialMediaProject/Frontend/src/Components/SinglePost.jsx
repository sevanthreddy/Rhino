import React, { useEffect, useState } from "react";
import { useParams,useNavigate } from "react-router-dom";
import PostCard from "./PostCard";
import CreatePostCard from "./CreatePostCard";


function SinglePost() {
    const { id } = useParams();

    const [post, setPost] = useState(null);
    const [replies, setreplies] = useState([]);
      const navigate = useNavigate();


    useEffect(() => {
        const fetchPost = async () => {
            var token = localStorage.getItem("token");
            const response = await fetch(`http://localhost:5040/api/Posts/post/${id}`, {
                method: "GET",
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (response.ok) {
                const data = await response.json();
                setPost(data);
            }
        };

        fetchPost();
    }, [id]);

    const getallreplies = async () => {
        var token = localStorage.getItem("token");

        const response = await fetch(
            `http://localhost:5040/api/ReplyTo/Replies/${id}`,
            {
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        );

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