import axios from "axios";

// Declare OneSignal types for TypeScript
declare global {
  interface Window {
    OneSignalDeferred?: Array<(OneSignal: any) => void>;
  }
}

export const login = (returnUrl?: string) => {
  console.log("Return Url:", returnUrl);
  window.location.href = "/login?returnUrl=" + returnUrl;
};

export const logout = () => {
  localStorage.removeItem("userinfor");
  window.OneSignalDeferred = window.OneSignalDeferred || [];
  window.OneSignalDeferred.push(async function (OneSignal) {
    await OneSignal.logout();
  });
};

export const getCurrentUser = () => {
  const userinfor = localStorage.getItem("userinfor");
  return JSON.parse(userinfor!);
};

export const setCurrentUser = (userinfor: any) => {
  localStorage.setItem("userinfor", JSON.stringify(userinfor));
};

export const isAuthenticated = () => {
  const userinfor = localStorage.getItem("userinfor");
  return !!userinfor;
};

export const loadUser = (): Promise<any> => {
  return axios
    .get("/userinfor")
    .then(function (response) {
      setCurrentUser(response.data);
      window.OneSignalDeferred = window.OneSignalDeferred || [];
      window.OneSignalDeferred.push(async function (OneSignal) {
        await OneSignal.login("phongtest");
      });
      return Promise.resolve(getCurrentUser());
    })
    .catch(function () {
      return Promise.resolve(null);
    })
    .finally(function () {});
};
