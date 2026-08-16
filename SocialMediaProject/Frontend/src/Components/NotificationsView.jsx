import React, { useEffect, useState } from 'react';
import { FaHeart } from "react-icons/fa";
import { getApiUrl } from "../config";



function NotificationsView() {
    const [notifications,setallnotifications]=useState([]);

    const fetchallNotifications = async () => {
        console.log("fetchallnotifications method started");
        const token = localStorage.getItem("token");
        const response = await fetch(getApiUrl('/api/Notifications/getall'), {
            method: "GET",
            headers: { "Authorization": `Bearer ${token}` }
        });
        if(response.ok){
            var data=await response.json();
            console.log("notifications",data);
            setallnotifications(data);
        }
        console.log("fetchallnotifications method ended");

    }

    useEffect(()=>{fetchallNotifications();}, [])

    return (
        <div>
            {notifications.length!=0 && <div className='bg-yellow-100 p-2'>
                <span>Notifications</span>
            </div>}
            {notifications && notifications.map((eachnotification)=>{
               return  <div key={eachnotification.id} className='border-b border-gray-100 flex flex-row items-center text-sm'>
                {eachnotification.type=="Like" && 
                <div className='m-1'>
                <FaHeart size={10} className='text-red-500'></FaHeart>
                </div>}
                {eachnotification.senderUserName+" "+eachnotification.content}
                </div>

            })}

            {notifications.length==0 && <div className='h-full w-full flex flex-col items-center justify-center'>
                <h1 className='text-2xl font-bold'>Notifications</h1>
                <p className='text-gray-500'>You have no new notifications.</p>
            </div>}
        </div>

    );
}

export default NotificationsView;