console.log("GlobalProvider rendered");

import {
    createContext,
    useContext,
    useEffect,
    useState
} from "react";

import * as signalR from "@microsoft/signalr";

import { getApiUrl } from "../config";

import {
    apiFetch,
    setToken,
    setTokenUpdater
} from "../api/apiClient";


const GlobalContext = createContext();


export function GlobalProvider({ children }) {

    const [user, setUser] = useState(null);

    const [connection, setConnection] = useState(null);

    const [onlineUsers, setOnlineUsers] = useState(new Set());

    const [conversations, setConversations] = useState([]);

    const [unreadMessagesCount, setUnreadMessagesCount] = useState(0);

    const [latestMessage, setLatestMessage] = useState(null);

    const [listofNewMessageSenders, setListofNewMessageSenders] = useState([]);

    const [notifications, setnotifications] = useState([]);

    const [accessToken, setaccessToken] = useState(null);


    // ---------------------------------------------------
    // UNREAD MESSAGES
    // ---------------------------------------------------

    const getUnreadMessagesCount = async () => {

        console.log("getUnreadMessagesCount method started");

        const response = await apiFetch(
            '/api/Message/unread',
            {
                method: 'GET'
            }
        );

        if (response.ok) {

            const data = await response.json();

            setUnreadMessagesCount(data.length);

            setListofNewMessageSenders(data);
        }

        console.log("getUnreadMessagesCount method ended");
    };


    // ---------------------------------------------------
    // NOTIFICATIONS
    // ---------------------------------------------------

    const handleNotification = async () => {

        console.log("handleNotification api started");

        const response = await apiFetch(
            '/api/Notifications/getall',
            {
                method: "GET"
            }
        );

        if (response.ok) {

            const data = await response.json();

            setnotifications(data);
        }

        console.log("handleNotification api ended");
    };


    const handlenewNotification = (notification) => {

        setnotifications(prev => [
            notification,
            ...prev
        ]);
    };


    // ---------------------------------------------------
    // CONNECT apiClient TO GLOBAL CONTEXT
    // ---------------------------------------------------

    useEffect(() => {

        console.log("🔥 Registering token updater");

        setTokenUpdater(setaccessToken);

    }, []);


    // ---------------------------------------------------
    // RESTORE SESSION
    // ---------------------------------------------------

    useEffect(() => {

        console.log("🔥 RESTORE SESSION EFFECT RUNNING");


        const restoreSession = async () => {

            console.log(
                "🔥 RESTORE SESSION FUNCTION RUNNING"
            );


            const response = await fetch(
                getApiUrl(
                    "/api/RegisterandLogin/refresh"
                ),
                {
                    method: "POST",
                    credentials: "include"
                }
            );


            console.log(
                "🔥 REFRESH RESPONSE:",
                response
            );


            if (response.ok) {

                const data = await response.json();

                console.log(
                    "🔥 REFRESH DATA:",
                    data
                );


                const newToken =
                    data.newAccessToken;


                // Update GlobalContext
                setaccessToken(newToken);


                // Update apiClient
                setToken(newToken);


                console.log(
                    "🔥 Token restored successfully"
                );

            } else {

                console.log(
                    "❌ No valid refresh session"
                );

            }

        };


        restoreSession();

    }, []);


    // ---------------------------------------------------
    // WHEN ACCESS TOKEN EXISTS
    // ---------------------------------------------------

    useEffect(() => {

        if (accessToken != null) {

            console.log(
                "🔥 Access token exists - loading user data"
            );

            getUnreadMessagesCount();

            handleNotification();
        }

    }, [accessToken]);


    // ---------------------------------------------------
    // SIGNALR CONNECTION
    // ---------------------------------------------------

    useEffect(() => {

        if (!accessToken) {
            return;
        }


        console.log(
            "🔥 Running SignalR after token"
        );


        const newConnection =
            new signalR.HubConnectionBuilder()

                .withUrl(
                    getApiUrl('/chatHub'),
                    {
                        accessTokenFactory:
                            () => accessToken
                    }
                )

                .withAutomaticReconnect()

                .build();


        // ---------------------------------------------------
        // USER ONLINE
        // ---------------------------------------------------

        const handleUserOnline = (userId) => {

            setOnlineUsers(prev => {

                const next = new Set(prev);

                next.add(Number(userId));

                return next;
            });
        };


        // ---------------------------------------------------
        // ALL ONLINE USERS
        // ---------------------------------------------------

        const handleOnlineUsers = (users) => {

            setOnlineUsers(
                new Set(users)
            );
        };


        // ---------------------------------------------------
        // USER OFFLINE
        // ---------------------------------------------------

        const handleUserOffline = (userId) => {

            setOnlineUsers(prev => {

                const next = new Set(prev);

                next.delete(Number(userId));

                return next;
            });
        };


        // ---------------------------------------------------
        // RECEIVE MESSAGE
        // ---------------------------------------------------

        const receiveMessage = async (message) => {

            console.log(
                "Received message:",
                message
            );


            await getUnreadMessagesCount();


            setLatestMessage(message);


            if (
                message.senderId !==
                Number(
                    localStorage.getItem("userid")
                )
            ) {

                await newConnection.invoke(
                    "RegisterDeliveredMesssage",
                    message.messageId,
                    message.senderId
                );


                console.log(
                    "Registering read message"
                );
            }
        };


        // ---------------------------------------------------
        // START SIGNALR
        // ---------------------------------------------------

        async function startConnection() {

            newConnection.on(
                "AllUsers",
                handleOnlineUsers
            );

            newConnection.on(
                "UserOnline",
                handleUserOnline
            );

            newConnection.on(
                "UserOffline",
                handleUserOffline
            );

            newConnection.on(
                "ReceiveMessage",
                receiveMessage
            );

            newConnection.on(
                "ReceiveNotification",
                handlenewNotification
            );


            await newConnection.start();


            console.log("Connected");


            setConnection(newConnection);
        }


        startConnection();


        // ---------------------------------------------------
        // CLEANUP
        // ---------------------------------------------------

        return () => {

            newConnection.off(
                "UserOnline",
                handleUserOnline
            );

            newConnection.off(
                "AllUsers",
                handleOnlineUsers
            );

            newConnection.off(
                "UserOffline",
                handleUserOffline
            );

            newConnection.off(
                "ReceiveMessage",
                receiveMessage
            );

            newConnection.off(
                "ReceiveNotification",
                handlenewNotification
            );


            newConnection.stop();
        };


    }, [accessToken]);


    // ---------------------------------------------------
    // CONTEXT
    // ---------------------------------------------------

    return (

        <GlobalContext.Provider
            value={{

                user,
                setUser,

                connection,
                setConnection,

                setOnlineUsers,
                onlineUsers,

                unreadMessagesCount,
                setUnreadMessagesCount,

                latestMessage,
                setLatestMessage,

                getUnreadMessagesCount,

                listofNewMessageSenders,

                notifications,

                accessToken,
                setaccessToken

            }}
        >

            {children}

        </GlobalContext.Provider>
    );
}


export function useGlobalContext() {

    return useContext(GlobalContext);
}