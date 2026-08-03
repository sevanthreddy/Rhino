console.log("GlobalProvider rendered");
import { createContext, useContext, useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";

const GlobalContext = createContext();

export function GlobalProvider({ children }) {
        const [user, setUser] = useState(null);
        const [connection, setConnection] = useState(null);
        const [onlineUsers, setOnlineUsers] = useState(new Set());
        const [conversations, setConversations] = useState([]);
        const [unreadMessagesCount, setUnreadMessagesCount] = useState(0);
        const [latestMessage, setLatestMessage] = useState(null);


        const getUnreadMessagesCount = async () => {
                console.log("getUnreadMessagesCount method started");
                const response = await fetch('http://localhost:5040/api/Message/unread', {
                        method: 'GET',
                        headers: {
                                'Authorization': `Bearer ${localStorage.getItem("token")}`
                        }
                });
                if (response.ok) {
                        var data = await response.json();
                        console.log("unread messages count:", data);
                        setUnreadMessagesCount(data);
                }
                console.log("getUnreadMessagesCount method ended");
        };



        useEffect(() => {
                console.log("Token:", localStorage.getItem("token"));
                getUnreadMessagesCount();
        }, [user]);

        useEffect(() => {
                console.log("running global context");
                const token = localStorage.getItem("token");
                if (!token) return;
                console.log("running global context after token line");

                const newConnection = new signalR.HubConnectionBuilder()
                        .withUrl("http://localhost:5040/chatHub", {
                                accessTokenFactory: () => localStorage.getItem("token")
                        })
                        .withAutomaticReconnect()
                        .build();

                const handleUserOnline = (userId) => {
                        setOnlineUsers(prev => {
                                const next = new Set(prev);
                                next.add(Number(userId));
                                return next;
                        });
                };
                const handleOnlineUsers = (users) => {
                        setOnlineUsers(new Set(users));
                };
                const handleUserOffline = (userId) => {
                        setOnlineUsers(prev => {
                                const next = new Set(prev);
                                next.delete(Number(userId));
                                return next;
                        });
                };



                const receiveMessage = async (message) => {
                        console.log("Received message:", message);
                        await getUnreadMessagesCount();

                        setLatestMessage(message);
                        if (message.senderId !== Number(localStorage.getItem("userid"))) {
                                await newConnection.invoke(
                                        "RegisterDeliveredMesssage",
                                        message.messageId,
                                        message.senderId
                                );
                                console.log("Registering read message");
                                
                        }

                };

                async function startConnection() {
                        newConnection.on("AllUsers", handleOnlineUsers);
                        newConnection.on("UserOnline", handleUserOnline);
                        newConnection.on("UserOffline", handleUserOffline);
                        newConnection.on("ReceiveMessage", receiveMessage);

                        await newConnection.start();

                        console.log("Connected");

                        setConnection(newConnection);
                }

                startConnection();

                return () => {
                        newConnection.off("UserOnline", handleUserOnline);
                        newConnection.off("AllUsers", handleOnlineUsers);
                        newConnection.off("UserOffline", handleUserOffline);
                        newConnection.off("ReceiveMessage", receiveMessage);
                        newConnection.stop();
                };
        }, [user]);

        return (
                <GlobalContext.Provider
                        value={{
                                user,
                                setUser,
                                connection,
                                setOnlineUsers,
                                setConnection,
                                onlineUsers,
                                unreadMessagesCount,
                                latestMessage,
                                getUnreadMessagesCount,
                                setUnreadMessagesCount,
                                setLatestMessage
                                
                        }}
                >
                        {children}
                </GlobalContext.Provider>
        );
}

export function useGlobalContext() {
        return useContext(GlobalContext);
}