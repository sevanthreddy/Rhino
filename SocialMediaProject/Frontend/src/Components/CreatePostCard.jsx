import React, { use, useState, useEffect } from "react";
import { useLocation } from "react-router-dom";
import { FiImage } from "react-icons/fi";
import { useOutletContext } from "react-router-dom";


const CreatePostCard = ({ oncreate, createpost,uploadProgress,
    isUploading }) => {
    //const  { handleCreatePost } = useOutletContext();

    const [content, setContent] = useState("");
    const [selectedFiles, setSelectedFiles] = useState([]);
    const [previewUrls, setPreviewUrls] = useState([]);
    const [mode, setmode] = useState("Post");
    const location = useLocation();

    useEffect(() => {
        if (location.pathname.startsWith("/home/post/")) {
            setmode("Reply");
        } else {
            setmode("Post");
        }
    }, [location.pathname]);

    useEffect(() => {
        const urls = selectedFiles.map(file => ({
            file,
            url: URL.createObjectURL(file)
        }));

        setPreviewUrls(urls);

        return () => {
            urls.forEach(item => URL.revokeObjectURL(item.url));
        };
    }, [selectedFiles]);

    const CreatePost = async (e) => {

        await createpost(e, mode, content, selectedFiles);
        await oncreate();
        setContent("");
        setSelectedFiles([]);

    }

    return (
        <div className="flex border border-gray-200 p-2 rounded-xl m-1 bg-white">
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

                {/* Image and video previews */}
                {previewUrls.length > 0 && (
                    <div className="grid grid-cols-2 gap-2 mb-2">
                        {previewUrls.map(({ file, url }, index) => (
                            file.type.startsWith("image/") ? (
                                <img
                                    key={index}
                                    src={url}
                                    alt="Preview"
                                    className="rounded-lg w-full object-cover"
                                />
                            ) : file.type.startsWith("video/") ? (
                                <video
                                    key={index}
                                    src={url}
                                    controls
                                    preload="metadata"
                                    className="rounded-lg w-full object-cover"
                                />
                            ) : null
                        ))}
                    </div>
                )}

                {/* Upload progress */}
{isUploading && (
    <div className="mb-3">
        <div className="flex justify-between text-sm text-gray-500 mb-1">
            <span>Uploading video...</span>
            <span>{uploadProgress}%</span>
        </div>

        <div className="w-full h-2 bg-gray-200 rounded-full overflow-hidden">
            <div
                className="h-full bg-blue-500 transition-all duration-200"
                style={{ width: `${uploadProgress}%` }}
            />
        </div>
    </div>
)}

                <div className="flex items-center">

                    <input
                        id="image"
                        type="file"
                        accept="image/*,video/*"
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