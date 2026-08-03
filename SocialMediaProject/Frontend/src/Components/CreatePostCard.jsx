import React, { use, useState, useEffect } from "react";
import { useLocation } from "react-router-dom";
import { FiImage } from "react-icons/fi";
import { useOutletContext } from "react-router-dom";


const CreatePostCard = ({ oncreate }) => {
    const  handleCreatePost  = useOutletContext();

    const [content, setContent] = useState("");
    const [selectedFiles, setSelectedFiles] = useState([]);
    const [mode, setmode] = useState("Post");
    const location = useLocation();

    useEffect(() => {
        if (location.pathname.startsWith("/home/post/")) {
            setmode("Reply");
        } else {
            setmode("Post");
        }
    }, [location.pathname]);

    const CreatePost=async (e)=>{
        await handleCreatePost(e,mode,content,selectedFiles);
        await oncreate();
        setContent("");
    }

    return (
        <div className="flex border border-gray-200 p-2">
            <div className="h-8 w-8 rounded-full bg-blue-500 flex items-center justify-center font-bold text-white">
                {localStorage.getItem("initials")}
            </div>

            <div className="ml-2 flex-1">

                <textarea
                    value={content}
                    onChange={(e) => setContent(e.target.value)}
                    placeholder={mode === "Post" ? "Create a Post" : "Create a Comment"}
                    className="min-h-24 w-full outline-none caret-blue-500"
                />

                {/* Image previews */}
                {selectedFiles.length > 0 && (
                    <div className="grid grid-cols-2 gap-2 mb-2">
                        {selectedFiles.map((file, index) => (
                            <img
                                key={index}
                                src={URL.createObjectURL(file)}
                                alt="Preview"
                                className="rounded-lg w-full object-cover"
                            />
                        ))}
                    </div>
                )}

                <div className="flex items-center">

                    <input
                        id="image"
                        type="file"
                        accept="image/*"
                        multiple
                        className="hidden"
                        onChange={(e) => {
                            setSelectedFiles(prev => [
                                ...prev,
                                ...Array.from(e.target.files)
                            ]);
                        }}
                    />

                    <label htmlFor="image" className="cursor-pointer p-2 rounded-full hover:bg-blue-100" >
                         <FiImage className="text-2xl text-blue-500" /> 
                    </label>

                    <button onClick={CreatePost} className="ml-auto px-4 py-2 rounded-full bg-blue-500 text-white hover:bg-blue-600" > 
                        {mode === "Post" ? "Post" : "Reply"}
                    </button>

                </div>
            </div>
        </div>
    );
};

export default CreatePostCard;