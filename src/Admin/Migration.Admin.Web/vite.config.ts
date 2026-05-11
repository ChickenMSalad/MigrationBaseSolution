import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const target = env.VITE_ADMIN_API_PROXY_TARGET || "https://localhost:55436";

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        "/api": {
          target,
          changeOrigin: true,
          secure: false
        },
        "/health": {
          target,
          changeOrigin: true,
          secure: false
        }
      }
    }
  };
});
