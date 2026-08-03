import React, { useEffect, useState, useRef } from "react";
import { Outlet, useLocation, useNavigate, useParams } from "react-router-dom";
import { MdHome, MdChat, MdLogout } from "react-icons/md";
import { HiOutlineChatAlt2 } from "react-icons/hi";
import { FiX } from "react-icons/fi";
import { useGlobalContext } from "../context/GlobalContext";





function ChatView() {

    const [allchats, setallchats] = useState(null);
    const [defaultview, setdefaultview] = useState(true);
    const navigate = useNavigate();
    const [selecteduser, setselecteduser] = useState("");
    const location = useLocation();
    const [showpeople, setshowpeople] = useState(false);
    const [allusers, setallusers] = useState(null);
    const { onlineUsers, connection } = useGlobalContext();
    const { userid } = useParams();
    const [typingUsers, setTypingUsers] = useState(new Set());

    useEffect(() => {
        if (!connection) return;

        const SetTypingIndicator = (userId) => {
            setTypingUsers(prev => {
                const next = new Set(prev);
                next.add(Number(userId));
                return next;
            });
        };

        const SetStopTypingIndicator = (userId) => {
            setTypingUsers(prev => {
                const next = new Set(prev);
                next.delete(Number(userId));
                return next;
            });
        };

        connection.on("TypingMessage", SetTypingIndicator);
        connection.on("StopTypingMessage", SetStopTypingIndicator);

        return () => {
            connection.off("TypingMessage", SetTypingIndicator);
            connection.off("StopTypingMessage", SetStopTypingIndicator);
        };
    }, [connection]);

    useEffect(() => {
        console.log("ChatView online users:", [...onlineUsers]);
        console.log(userid);
    }, [onlineUsers]);

    const fetchallchats = async () => {
        const response = await fetch("http://localhost:5040/api/Message/chats", {
            method: "GET",
            headers: { "Authorization": `Bearer ${localStorage.getItem("token")}` }
        });
        if (response.ok) {
            var data = await response.json();
            setallchats(data);
            setdefaultview(false);
        }
    }
    useEffect(() => {
        fetchallchats();
    }, []);
    const handleClickAChat = async (userId, username) => {
        navigate(`/home/chat/${userId}`);
        setselecteduser(username);
        setshowpeople(false);
    };


    const handleStartChat = async () => {
        setshowpeople(true);
        const response = await fetch("http://localhost:5040/api/Message/users", {
            method: "GET",
            headers: { "Authorization": `Bearer ${localStorage.getItem("token")}` }
        });
        if (response.ok) {
            var data = await response.json();
            setallusers(data);
        }

    }
    const handleClose = () => {
        setshowpeople(false);
    }



    return (
        <div>
            <div className="h-screen grid grid-cols-12 w-full">
                <div className={`${userid ? "hidden md:block! md:col-span-3 border border-red-500" : "col-span-12 md:col-span-3!  border-red-500"}`}>
                    <div className="md:col-span-3!  col-span-12  flex flex-col  border border-gray-200 h-screen p-2">{/*section for showing  all chats*/}
                        {allchats && allchats.map((eachchat) => (
                            <div key={eachchat.userid} onClick={() => { handleClickAChat(eachchat.userid, eachchat.name) }} className="flex  items-center border-b border-gray-200 h-10 w-full cursor-pointer gap-1 hover:bg-gray-200">
                                <div className="size-4 sm:size-7 md:size-8 bg-gray-400 rounded-3xl relative">
                                    {onlineUsers.has(eachchat.userid) && (
                                        <div className="absolute bottom-0 right-0 size-2 sm:size-2.5 md:size-3 rounded-full bg-green-500 border border-white"></div>
                                    )}                            </div>{/*for profile circle */}
                                <div className="text-sm md:text-lg!">{eachchat.name}</div>
                                {typingUsers.has(eachchat.userid) && <div className="text-sm text-green-500 ml-auto">Typing</div>}
                            </div>
                        ))}
                    </div>
                </div>
                <div className="col-span-12 md:col-span-9! h-full min-h-0">
                    <Outlet context={{ selecteduser, fetchallchats }} />
                    {location.pathname == '/home/chat' && <div className="hidden md:flex! h-full items-center justify-center">
                        <div className="flex flex-col items-center">
                            <HiOutlineChatAlt2 className='mb-4'></HiOutlineChatAlt2>
                            <button onClick={handleStartChat} className="hover:bg-gray-200 p-2 bg-gray-100 cursor-pointer rounded-xl">Start A Chat</button>
                        </div>
                    </div>}
                </div>

            </div>
            {showpeople == true && <div className="bg-black/20 fixed inset-0 z-50 flex items-center justify-center">
                <div className="bg-white rounded-xl shadow-lg max-w-xl min-w-80 overflow-y-auto min-h-80">
                    <div className="relative p-2 flex items-center">{/*header section  */}
                        <FiX onClick={handleClose} className="hover:text-blue-500 cursor-pointer"></FiX>
                        <div className="absolute left-1/2 -translate-x-1/2 text-xs font-semibold">
                            New Message
                        </div>
                    </div>

                    <div className="flex flex-col items-center w-full">{/*list showing all users*/}
                        {allusers && allusers.map((eachuser) => (
                            <div className="p-2 hover:bg-gray-200 cursor-pointer w-full flex justify-center" onClick={() => { handleClickAChat(eachuser.userid, eachuser.name) }}>
                                <div>{eachuser.name}</div>
                            </div>
                        ))}

                    </div>
                </div>
            </div>}


        </div>
    );
}

export default ChatView;