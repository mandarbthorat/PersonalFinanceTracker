import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE, // e.g., https://finance-api-<unique>.azurewebsites.net
  withCredentials: false,
});

export function setAuthToken(token?: string) {
  if (token) api.defaults.headers.common.Authorization = `Bearer ${token}`;
  else delete api.defaults.headers.common.Authorization;
}

export default api;
