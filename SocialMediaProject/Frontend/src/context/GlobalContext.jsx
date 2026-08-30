
import {
    createContext,
    useContext,
    useEffect,
    useRef,
    useState
} from "react";

import * as signalR from "@microsoft/signalr";
import { jwtDecode } from "jwt-decode";
import { getApiUrl } from "../config";

const GlobalContext = createContext();

export function GlobalProvider({ children }) {
    console.log("Globalcontext rendered");

    // =========================
    // STATE
    // =========================

    const [user, setUser] = useState(null);

    const [connection, setConnection] = useState(null);

    const [onlineUsers, setOnlineUsers] = useState(new Set());

    const [conversations, setConversations] = useState([]);

    const [unreadMessagesCount, setUnreadMessagesCount] = useState(0);

    const [latestMessage, setLatestMessage] = useState(null);

    const [listofNewMessageSenders, setListofNewMessageSenders] = useState([]);

    const [unreadnotificationsCount, setunreadnotificationsCount] = useState([]);

    const [notifications,setnotifications]=useState([]);

    const [accessToken, setaccessToken] = useState(null);

    const [userId, setUserId] = useState(null);

    // VERY IMPORTANT
    // Application should wait until we finish checking
    // whether a refresh session exists.
    const [authLoading, setAuthLoading] = useState(true);


    // =========================
    // REFRESH LOCK
    // =========================

    // If multiple API calls receive 401 at the same time,
    // they will all wait for this SAME promise.
    const refreshPromiseRef = useRef(null);


    // =========================
    // REFRESH SESSION
    // =========================

    const refreshSession = async () => {

        // If a refresh request is already running,
        // don't start another one.
        if (refreshPromiseRef.current) {

            console.log(
                "⏳ Refresh already running. Waiting for existing refresh."
            );

            return refreshPromiseRef.current;
        }


        refreshPromiseRef.current = (async () => {

            try {

                console.log("🔥 REFRESH SESSION STARTED");

                const response = await fetch(
                    getApiUrl("/api/RegisterandLogin/refresh"),
                    {
                        method: "POST",
                        credentials: "include"
                    }
                );

                console.log(
                    "🔥 REFRESH RESPONSE:",
                    response.status
                );


                if (!response.ok) {

                    console.log(
                        "❌ No valid refresh session"
                    );

                    setaccessToken(null);
                    setUserId(null);
                    setUser(null);

                    return null;
                }


                const data = await response.json();

                console.log(
                    "🔥 REFRESH DATA:",
                    data
                );


                const newAccessToken = data.newAccessToken;


                if (!newAccessToken) {

                    console.log(
                        "❌ Refresh response did not contain access token"
                    );

                    setaccessToken(null);
                    setUserId(null);
                    setUser(null);

                    return null;
                }


                console.log(
                    "✅ New access token received"
                );


                // Store new access token in React state
                setaccessToken(newAccessToken);
                setUserIdFromToken(newAccessToken);


                return newAccessToken;

            }
            catch (error) {

                console.error(
                    "❌ Refresh request failed:",
                    error
                );

                setaccessToken(null);
                setUserId(null);
                setUser(null);

                return null;
            }
            finally {

                // Allow another refresh in the future
                refreshPromiseRef.current = null;
            }

        })();


        return refreshPromiseRef.current;
    };


    // =========================
    // API FETCH
    // =========================

    const apiFetch = async (path, options = {}) => {

        console.log( "apiFetch token:", accessToken );

        console.log(
            "api path:",
            path
        );


        let headers = {
            ...(options.headers || {})
        };


        // Add access token if we have one
        if (accessToken) {

            headers.Authorization =
                `Bearer ${accessToken}`;
        }


        // First request
        let response;

        try {

            response = await fetch(
                getApiUrl(path),
                {
                    ...options,
                    headers
                }
            );

        }
        catch (error) {

            console.error(
                "❌ API request failed:",
                error
            );

            throw error;
        }


        // =========================
        // NOT UNAUTHORIZED
        // =========================

        if (response.status !== 401) {

            return response;
        }


        // =========================
        // 401
        // =========================

        console.log(
            "🔥 API returned 401"
        );


        // Try to refresh
        const newAccessToken =
            await refreshSession();


        // Refresh failed
        if (!newAccessToken) {

            console.log(
                "❌ Could not refresh access token"
            );

            return response;
        }


        // =========================
        // RETRY ORIGINAL REQUEST
        // =========================

        console.log(
            "🔄 Retrying original API request"
        );


        headers.Authorization =
            `Bearer ${newAccessToken}`;


        response = await fetch(
            getApiUrl(path),
            {
                ...options,
                headers
            }
        );


        return response;
    };


    // =========================
    // LOGIN TOKEN SETTER
    // =========================

    const setLoginToken = (token) => {

        console.log(
            "🔥 Setting login access token"
        );

        setaccessToken(token);
    };


    const setUserIdFromToken = (token) => {

        try {
            const decodedToken = jwtDecode(token);
            setUserId(decodedToken.userId);
        }
        catch (error) {
            console.error("Unable to decode access token:", error);
            setUserId(null);
        }
    };


    // =========================
    // LOGOUT
    // =========================

    const logout = async () => {

        try {

            console.log(
                "🔥 Logout started"
            );


            // Tell backend to revoke refresh token
            await fetch(
                getApiUrl("/api/RegisterandLogin/logout"),
                {
                    method: "POST",
                    credentials: "include"
                }
            );

        }
        catch (error) {

            console.error(
                "Logout API failed:",
                error
            );

        }
        finally {

            // Clear frontend authentication
            setaccessToken(null);

            setUserId(null);

            setUser(null);

            setOnlineUsers(new Set());

            setConnection(null);

            console.log(
                "✅ Logged out"
            );
        }
    };


    // =========================
    // UNREAD MESSAGES
    // =========================

    const getUnreadMessagesCount = async () => {

        console.log(
            "getUnreadMessagesCount method started"
        );


        const response = await apiFetch(
            "/api/Message/unread",
            {
                method: "GET"
            }
        );


        if (response.ok) {

            const data =
                await response.json();

            setUnreadMessagesCount(
                data.length
            );

            setListofNewMessageSenders(
                data
            );
        }


        console.log(
            "getUnreadMessagesCount method ended"
        );
    };


    // =========================
    // NOTIFICATIONS
    // =========================

    const handleNotification = async () => {

        console.log(
            "handleNotification api started"
        );


        const response = await apiFetch(
            "/api/Notifications/getall",
            {
                method: "GET"
            }
        );


        if (response.ok) {

            const data =
                await response.json();

            setnotifications(data);
        }


        console.log(
            "handleNotification api ended"
        );
    };


    const handlenewNotification = (notification) => {

        setnotifications(prev => [
            notification,
            ...prev
        ]);
    };


    // =========================
    // RESTORE SESSION
    // =========================

    useEffect(() => {

        console.log(
            "🔥 RESTORE SESSION EFFECT RUNNING"
        );
        


        const restoreSession = async () => {

            console.log(
                "🔥 RESTORE SESSION FUNCTION RUNNING"
            );


            const newToken =
                await refreshSession();


            if (newToken) {

                console.log(
                    "✅ Session restored"
                );

            }
            else {

                console.log(
                    "❌ No valid session"
                );

                setaccessToken(null);
                setUserId(null);
                setUser(null);
            }


            // VERY IMPORTANT
            // The application can now render.
            setAuthLoading(false);

        };


        restoreSession();

    }, []);


    // =========================
    // LOAD USER DATA
    // =========================

    useEffect(() => {

        if (!accessToken) {
            return;
        }


        console.log(
            "🔥 Access token exists - loading user data"
        );


        getUnreadMessagesCount();

        handleNotification();

    }, [accessToken]);


    // =========================
    // SIGNALR
    // =========================

    useEffect(() => {

        if (!accessToken) {

            console.log(
                "❌ No access token. SignalR not started."
            );

            return;
        }


        console.log(
            "🔥 Running SignalR after token"
        );


        const newConnection =
            new signalR.HubConnectionBuilder()
                .withUrl(
                    getApiUrl("/chatHub"),
                    {
                        accessTokenFactory: () =>
                            accessToken
                    }
                )
                .withAutomaticReconnect()
                .build();


        const handleUserOnline = (userId) => {

            setOnlineUsers(prev => {

                const next =
                    new Set(prev);

                next.add(
                    Number(userId)
                );

                return next;
            });
        };


        const handleOnlineUsers = (users) => {

            setOnlineUsers(
                new Set(users)
            );
        };


        const handleUserOffline = (userId) => {

            setOnlineUsers(prev => {

                const next =
                    new Set(prev);

                next.delete(
                    Number(userId)
                );

                return next;
            });
        };


        const receiveMessage = async (message) => {

            console.log(
                "Received message:",
                message
            );


            await getUnreadMessagesCount();


            setLatestMessage(message);


            if (
                message.senderId !==
                Number(userId)
            ) {

                try {

                    await newConnection.invoke(
                        "RegisterDeliveredMesssage",
                        message.messageId,
                        message.senderId
                    );

                    console.log(
                        "Registering delivered message"
                    );

                }
                catch (error) {

                    console.error(
                        "SignalR invoke failed:",
                        error
                    );
                }
            }
        };


        const startConnection = async () => {

            try {

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


                console.log(
                    "✅ SignalR Connected"
                );


                setConnection(
                    newConnection
                );

            }
            catch (error) {

                console.error(
                    "❌ SignalR connection failed:",
                    error
                );
            }
        };


        startConnection();


        return () => {

            console.log("🧹 Cleaning SignalR connection");
            newConnection.off("UserOnline", handleUserOnline);
            newConnection.off("AllUsers", handleOnlineUsers);
            newConnection.off("UserOffline", handleUserOffline);
            newConnection.off("ReceiveMessage", receiveMessage);
            newConnection.off("ReceiveNotification", handlenewNotification);
            newConnection.stop();
        };


    }, [accessToken]);


    // =========================
    // CONTEXT
    // =========================

    return (

        <GlobalContext.Provider
            value={{

                // Authentication
                user,
                setUser,

                userId,
                setUserId,

                accessToken,
                setaccessToken,
                setLoginToken,

                authLoading,

                refreshSession,

                logout,

                apiFetch,


                // SignalR
                connection,
                setConnection,

                onlineUsers,
                setOnlineUsers,


                // Messages
                conversations,
                setConversations,

                unreadMessagesCount,
                setUnreadMessagesCount,

                latestMessage,
                setLatestMessage,

                listofNewMessageSenders,

                getUnreadMessagesCount,


                // Notifications
                unreadnotificationsCount,

                setunreadnotificationsCount,

                notifications,
                setnotifications


            }}
        >

            
            {children}
        

        </GlobalContext.Provider>
    );
}


export function useGlobalContext() {

    return useContext(
        GlobalContext
    );
}