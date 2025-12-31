import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";

const API_TARGET = "http://localhost:5015";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      "/login": {
        target: API_TARGET,
        changeOrigin: false,
        secure: false,
        rewrite: (path) => {
          var newPath = "/api" + path;
          console.log(`Rewriting path: ${path}` + ` to ${newPath}`);
          return newPath;
        },
      },
      "/logout": {
        target: API_TARGET,
        changeOrigin: false,
        secure: false,
        rewrite: (path) => {
          var newPath = "/api" + path;
          console.log(`Rewriting path: ${path}` + ` to ${newPath}`);
          return newPath;
        },
      },
      "/userinfor": {
        target: API_TARGET,
        changeOrigin: false,
        secure: false,
        rewrite: (path) => {
          var newPath = "/api" + path;
          console.log(`Rewriting path: ${path}` + ` to ${newPath}`);
          return newPath;
        },
      },
      "/signin-oidc": {
        target: API_TARGET,
        changeOrigin: false,
        secure: false,
        rewrite: (path) => {
          var newPath = "/api" + path;
          console.log(`Rewriting path: ${path}` + ` to ${newPath}`);
          return newPath;
        },
      },
      "/api": {
        target: API_TARGET,
        changeOrigin: false,
        secure: false,
      },
    },
  },
});
