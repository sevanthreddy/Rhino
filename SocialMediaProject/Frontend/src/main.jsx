import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import { GoogleOAuthProvider } from '@react-oauth/google'
import { GlobalProvider } from './context/GlobalContext.jsx'
import { BrowserRouter } from 'react-router-dom';
import { Provider } from "react-redux";
import { store } from "./CentralStore/Store";
import LoginandSignup from "./Components/LoginandSignup";

createRoot(document.getElementById('root')).render(
  <GoogleOAuthProvider clientId="548377775699-jvaoa9mu4cvedtflbd9tb42in3m8icso.apps.googleusercontent.com">
    <BrowserRouter>
      <Provider store={store}>
        <GlobalProvider>
          <App />
        </GlobalProvider>
      </Provider>
    </BrowserRouter>
  </GoogleOAuthProvider>
)
