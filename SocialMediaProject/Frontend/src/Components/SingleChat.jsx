console.log("SingleChat rendered");
import React, { useEffect, useState, useRef } from "react";
import { useLocation, useParams, useOutletContext, useNavigate } from "react-router-dom";
import * as signalR from "@microsoft/signalr";
import { MdSend } from "react-icons/md";
import { MdArrowBack, MdDone, MdDoneAll } from "react-icons/md";
import { useGlobalContext } from "../context/GlobalContext";
import { apiFetch } from "../api/apiClient";




function SingleChat() {
    const [curuser, setcuruser] = useState(0);
    const [messages, setmessages] = useState([]);
    const { userid } = useParams();
    const messagesEndRef = useRef(null);
    const messagesContainerRef = useRef(null);
    const [text, settext] = useState("");
    const { selecteduser, fetchallchats } = useOutletContext();
    const navigate = useNavigate();
    const { connection, latestMessage, getUnreadMessagesCount } = useGlobalContext();
    const [isTyping, setisTyping] = useState(false);
    var timer = useRef(null);
    const pendingAcknowledgement = useRef(null);
    const loadingOlder = useRef(false);
    const shouldAdjustScroll = useRef(false);
    const prevScrollHeightRef = useRef(0);
    const messagesRef = useRef([]);


    useEffect(() => {
        const processLatestMessage = async () => {
            if (!latestMessage) return;

            if (
                latestMessage.senderId !== Number(userid) &&
                latestMessage.senderId !== Number(localStorage.getItem("userid"))
            ) {
                return;
            }

            // First show the message
            setmessages(prev => [...prev, latestMessage]);

            // Only incoming messages should be marked as read
            if (latestMessage.senderId === Number(userid)) {

                const response = await apiFetch(`/api/Message/updateAll/${userid}`, {
                    method: "POST"
                });

                if (response.ok) {
                    await getUnreadMessagesCount();

                    setmessages(prev =>
                        prev.map(message =>
                            message.senderId === Number(userid)
                                ? { ...message, status: "Read" }
                                : message
                        )
                    );

                    await connection.invoke(
                        "RegisterReadMesssage",
                        [latestMessage.messageId],
                        latestMessage.senderId
                    );
                }
            }
        };

        processLatestMessage();
    }, [latestMessage, userid, connection]);

    useEffect(() => {
        if (!connection) return;
        const updatemessage = async (messageId) => {
            console.log(messageId);
            console.log(messages);
            setmessages(prevMessages =>
                prevMessages.map(message =>
                    message.messageId === messageId
                        ? { ...message, status: "Delivered" }
                        : message
                )
            );
        }
        connection.on("DeliveredMessage", updatemessage);
        connection.on("ReadMessage", (msgId) => {
            setmessages(prevMessages =>
                prevMessages.map(message =>
                    message.messageId === msgId
                        ? { ...message, status: "Read" }
                        : message
                )
            );
        });
        return () => {
            connection.off("DeliveredMessage", updatemessage);
            connection.off("ReadMessage");
        };
    }, [connection]);

    useEffect(() => {
        messagesRef.current = messages;

        if (shouldAdjustScroll.current && messagesContainerRef.current) {
            const container = messagesContainerRef.current;
            const newScrollHeight = container.scrollHeight;
            container.scrollTop = newScrollHeight - prevScrollHeightRef.current;
            shouldAdjustScroll.current = false;
        } else if (!loadingOlder.current) {
            messagesEndRef.current?.scrollIntoView({ block: "end" });
        }
    }, [messages]);

    useEffect(() => {
        if (!connection) return;

        getmessages();
    }, [userid, connection]);
    const getmessages = async () => {
        // fetch conversation here
        console.log("getmessages called");
        const response = await apiFetch(`/api/Message/${userid}`, {
            method: "GET"
        });
        if (response.ok) {
            var data = await response.json();
            const response2 = await apiFetch(`/api/Message/updateAll/${userid}`, {
                method: "POST"
            });
            if (response2.ok) {
                var data2 = await response2.json();
                console.log("data2:", data2);
                console.log("connection inside getmessages:", connection);
                await connection.invoke("RegisterReadMesssage", data2, Number(userid));
                console.log("setting all messages read");
                data = data.map(message =>
                    message.senderId === Number(userid) &&
                        (message.status === "Delivered" || message.status === "Sent")
                        ? { ...message, status: "Read" }
                        : message
                );
            }
        }
        setmessages(data);
        await getUnreadMessagesCount();
        console.log("getmessages finished");
    }

    useEffect(() => {
        const container = messagesContainerRef.current;
        if (!container) return;
        const handleScroll = () => {

            if (container.scrollTop <= 5) {
                loadmoremessages();
            }
        };

        container.addEventListener("scroll", handleScroll);

        return () => {
            container.removeEventListener("scroll", handleScroll);
        };
    }, []);

    const loadmoremessages = async () => {
        if (messagesRef.current.length === 0) return;
        if (loadingOlder.current) return;
        loadingOlder.current = true;

        const container = messagesContainerRef.current;
        if (container) {
            prevScrollHeightRef.current = container.scrollHeight;
            shouldAdjustScroll.current = true;
        }

        const oldestMessageId = messagesRef.current[0].messageId;

        const response = await apiFetch(`/api/Message/${userid}?lastmessageid=${oldestMessageId}`, {
            method: "GET"
        });

        if (response.ok) {
            var data = await response.json();
            if (data.length === 0) {
                loadingOlder.current = false;
                shouldAdjustScroll.current = false;
                return;
            }

            setmessages(prev => [...data, ...prev]);
        }
        loadingOlder.current = false;
        console.log("loadmoremessages finished");

    }


    const handleSendMsg = async () => {
        try {
            console.log("Sending...");
            console.log(connection?.state);
            if (isTyping) {
                setisTyping(false);
                await connection.invoke("SendStopTypingMessage", Number(userid));
            }
            await connection.invoke("SendMessage", Number(userid), text);
            fetchallchats();
            settext("");
            console.log("Sent successfully");
        } catch (err) {
            console.error(err);
        }
    }

    const handlebackbuttonclick = () => {
        navigate("/home/chat");

    }

    const handleOnChangeInTextBox = async (e) => {
        settext(e.target.value);
        if (!isTyping) {
            setisTyping(true);
            await connection.invoke("SendTypingMessage", Number(userid));
            console.log("started send typing invoke");
        }

        clearTimeout(timer.current);

        timer.current = setTimeout(async () => {
            setisTyping(false);
            await connection.invoke("SendStopTypingMessage", Number(userid));
        }, 15000);

    }

    return (
        <div className="flex flex-col h-full">

            <div className="h-10 border-b border-gray-200 flex items-center">
                <button onClick={handlebackbuttonclick} className=" flex items-center justify-center rounded-full hover:bg-gray-200 cursor-pointer p-2">
                    <MdArrowBack size={16} />
                </button>
                {selecteduser}
            </div>

            <div ref={messagesContainerRef} className="flex-1 overflow-y-auto">
                {messages &&

                    messages.map((eachmessage) => (
                        <div key={eachmessage.messageId}
                            className={`flex flex-1 mt-1 mr-3 ${eachmessage.senderId === Number(localStorage.getItem("userid"))
                                ? "justify-end"
                                : "justify-start"
                                }`}>
                            <div className={` p-1 text-sm rounded-xl leading-relaxed  font-bold ${eachmessage.senderId === Number(localStorage.getItem("userid"))
                                ? "bg-blue-500 text-white"
                                : "bg-gray-200 text-black"
                                }`}>
                                {eachmessage.content}
                            </div>
                            {eachmessage.senderId === Number(localStorage.getItem("userid")) && (
                                <>
                                    {eachmessage.status === "Sent" && <MdDone size={14} className="text-lg text-blue-300 mt-auto" />}
                                    {eachmessage.status === "Delivered" && <MdDoneAll size={14} className="text-lg mt-auto"></MdDoneAll>}
                                    {eachmessage.status === "Read" && <MdDoneAll size={14} className="text-lg mt-auto text-blue-500"></MdDoneAll>}
                                </>
                            )}
                        </div>
                    ))}
                <div ref={messagesEndRef}></div>


            </div>

            <div className="w-full rounded-xl  h-10 flex">
                <textarea onKeyDown={(e) => {
                    if (e.key === "Enter" && !e.shiftKey) {
                        e.preventDefault(); // Prevent a new line
                        handleSendMsg();
                    }
                }} value={text} onChange={handleOnChangeInTextBox} className="bg-gray-200 flex-1 rounded-xl h-10 outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500">
                </textarea>
                <button className="p-2 bg-blue-500 rounded-xl text-white cursor-pointer" onClick={handleSendMsg}>
                    <MdSend size={22} />
                </button>
            </div>



        </div>
    );
}

export default SingleChat;