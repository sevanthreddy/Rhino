import React, { useState } from "react";
import { FiX } from "react-icons/fi";
import { HiCamera } from "react-icons/hi";
import { useRef } from "react";
import { useGlobalContext } from "../context/GlobalContext";


function EditProfileView({ onclose }) {
    const fileinputrefhandler = useRef(null);
    const [selectedfile, setselectedfile] = useState(null);
    const [prevwimg, setprevwimg] = useState(null);
    const [profileData, setProfileData] = useState({
        name: "",
        bio: "",
        profileImage: null
    });

    const handleImageUploadClick = () => {
        fileinputrefhandler.current.click();

    }
    const handleimagechange = (e) => {
        const file = e.target.files[0];
        setselectedfile(file);
        const x = URL.createObjectURL(file);
        setprevwimg(x);
    }

    const handleClose = (e) => {
        e.stopPropagation();
        onclose();

    }

    const handleSaveProfile = async () => {
        const formdata = new FormData();
        formdata.append("Name", profileData.name);
        formdata.append("Bio", profileData.bio);
        formdata.append("profileimage", selectedfile);
        const response = await apiFetch('/api/Profile/Save', {
            method: "POST",
            body: formdata
        });
    }


    return (
        <div className="fixed inset-0 z-50 pt-10 bg-black/40 flex justify-center w-full border">
            <div className="bg-white rounded-xl shadow-lg max-w-xl  max-h-80 min-w-120 overflow-y-auto">
                <div className="flex items-center mb-1 p-1">{/*header section having save button,Edit Profile heading */}
                    <FiX onClick={handleClose} className="hover:text-blue-500 cursor-pointer"></FiX>
                    <h1 className="ml-2">Edit Profile</h1>
                    <button onClick={handleSaveProfile} className="bg-black text-white rounded-xl text-sm ml-auto p-1 cursor-pointer">Save</button>
                </div>
                <div className="h-50 border bg-gray-300 w-full flex items-center justify-center relative">{/*profile image box where we can upload the picture */}
                    <HiCamera onClick={handleImageUploadClick} size={32} className="text-lg cursor-pointer hover:text-gray-500 absolute text-red-500"></HiCamera>
                    <input type="file" hidden accept="image/*" ref={fileinputrefhandler} onChange={handleimagechange}></input>
                    {selectedfile && <img src={prevwimg} className="h-50 w-full"></img>}

                </div>
                <div className="border mt-1">
                    <span className="text-xs">Name</span>
                    <input onChange={(e) => { setProfileData({ ...profileData, name: e.target.value }) }} autoCorrect="on" maxLength='50' className=" w-full rounded outline-none focus:border-blue-500"></input>
                </div>
                <div className="border mt-1">
                    <span className="text-xs">Bio</span>
                    <input onChange={(e) => { setProfileData({ ...profileData, bio: e.target.value }) }} maxLength='50' className=" w-full rounded outline-none focus:border-blue-500"></input>
                </div>
            </div>
        </div>
    );


}

export default EditProfileView; 