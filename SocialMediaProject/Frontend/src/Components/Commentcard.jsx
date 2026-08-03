import React, { useState } from "react";
import { FiX } from "react-icons/fi";
import { FiImage } from "react-icons/fi";




function CommentCard({ showoriginalpost, onclose, oncreatecomment }) {
    const [reply, setreply] = useState("");
    const [selectedFiles, setSelectedFiles] = useState([]);



    const handleCloseClick = (e) => {
        e.stopPropagation();
        onclose();
    }

    const handleReply = (e) => {
        oncreatecomment(e, "Reply", reply, selectedFiles);
        onclose();
    }

    return (
        <div className="fixed inset-0 z-50  flex items-start justify-center pt-10 bg-black/40">
            <div className="bg-white w-[90%] max-w-lg rounded-xl  flex flex-col ml-20 p-4">
                <div className="rounded-xl">{/*header top section*/}
                    <FiX onClick={handleCloseClick} className="hover:text-blue-500 mb-4 ml-1.5"></FiX>
                </div>
                <div className="flex  rounded-xl items-center">{/*Second section for showing post which we are replying to */}
                    <div className='flex'>
                        <div className={`w-8 h-8 rounded-full  flex items-center justify-center text-black  font-bold bg-blue-500`}>PE</div>
                    </div>
                    <div className="flex-1 ml-1">{showoriginalpost}</div>

                </div>
                <div className="flex rounded-xl mt-8 mb-1">{/*third section for replying area */}
                    <div className='flex'>
                        <div className={`w-8 h-8 rounded-full  flex items-center justify-center text-black  font-bold bg-blue-500`}>PE</div>
                    </div>
                    <div className="flex flex-col w-full">
                        <textarea value={reply} placeholder="Reply to the post" className=" min-h-24 w-full flex-1 ml-1 outline-none caret-blue-500 focus:border-blue-500 border-slate-800" onChange={(e) => { setreply(e.target.value) }}></textarea>
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

                    </div>

                </div>
                <div className="flex space-between  rounded-xl">{/*footer section for addding media and reply button*/}
                    <input id="commentimage" type="file" accept="image/*" multiple className="hidden" onChange={(e) => { setSelectedFiles(prev => [...prev, ...Array.from(e.target.files)]); }} />
                    <label htmlFor="commentimage" className="cursor-pointer p-2 rounded-full hover:bg-blue-100" >
                        <FiImage className="text-2xl text-blue-500" />
                    </label>
                    <button onClick={handleReply} className="ml-auto px-4 py-2 rounded-full bg-blue-500 text-white hover:bg-blue-600">Reply</button>

                </div>

            </div>


        </div>
    );
}

export default CommentCard;