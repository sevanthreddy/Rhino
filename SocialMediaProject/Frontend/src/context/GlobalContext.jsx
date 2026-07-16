import { createContext, useContext, useState } from "react";

const GlobalContext = createContext();

export function GlobalProvider({ children }) {
        const [user, setUser] = useState(null);

        return (
                <GlobalContext.Provider value={{ user, setUser }}>
                        {children}
                </GlobalContext.Provider>
        );
}

export function useGlobalContext() {
        return useContext(GlobalContext);
}