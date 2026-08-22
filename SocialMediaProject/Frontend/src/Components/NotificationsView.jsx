import React, { useEffect, useState } from 'react';
import { FaHeart, FaRegComment } from "react-icons/fa";
import { useGlobalContext } from "../context/GlobalContext";




function NotificationsView() {
    const [notifications, setallnotifications] = useState([]);
    const { accessToken, apiFetch } = useGlobalContext();

    const fetchallNotifications = async () => {
        console.log("fetchallnotifications method started");
        const response = await apiFetch('/api/Notifications/getall', {
            method: "GET",
        });
        if (response.ok) {
            var data = await response.json();
            console.log("notifications", data);
            const unreadCount = data.filter(notification => !notification.isRead).length;
            setallnotifications(data);
        }
        console.log("fetchallnotifications method ended");

    }

    const MarkNotificationsAsRead = async () => {
        console.log("MarkNotificationsAsRead method started");
        const response = await apiFetch('/api/Notifications', {
            method: "PUT",
        });
        if (response.ok) {
            console.log("Marking all Notifications as Read");
        }
    }



    useEffect(() => {
        if (accessToken) {
            fetchallNotifications();
            MarkNotificationsAsRead();
        }
    }, [accessToken]);

    return (
        <div>
            {notifications.length != 0 && <div className='bg-yellow-100 p-2'>
                <span>Notifications</span>
            </div>}
            {notifications && notifications.map((eachnotification) => {
                return <div key={eachnotification.id} className='border-b border-gray-100 flex flex-row items-center text-sm'>
                    {eachnotification.type == "Like" &&
                        <div className='m-1'>
                            <FaHeart size={10} className='text-red-500'></FaHeart>
                        </div>}
                    {eachnotification.type == "Comment" &&
                        <div className='m-1'>
                            <FaRegComment size={10} className='text-zinc-500'></FaRegComment>
                        </div>}
                    {eachnotification.senderUserName + " " + eachnotification.content}
                </div>

            })}

            {notifications.length == 0 && <div className='h-full w-full flex flex-col items-center justify-center'>
                <h1 className='text-2xl font-bold'>Notifications</h1>
                <p className='text-gray-500'>You have no new notifications.</p>
            </div>}
        </div>

    );
}

export default NotificationsView;