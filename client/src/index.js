// index.js
import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import axios from 'axios';
import './index.css';
import { v4 as uuidv4 } from 'uuid';
import dayjs from 'dayjs';
import '@ant-design/v5-patch-for-react-19';
import 'antd/dist/reset.css'; // ✅ Ant Design styles

let userId = localStorage.getItem('userId');
if (!userId) {
    userId = `${uuidv4().slice(0, 24)}${dayjs().format('1DDHHmmssSSS')}`;
    localStorage.setItem('userId', userId);
}

// Set Axios default header
axios.defaults.headers.common['X-User-Id'] = userId;

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(
    <App />
);
