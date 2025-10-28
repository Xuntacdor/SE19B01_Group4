import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    https: false,
    proxy: {
      "/api": {
        target: "https://localhost:7264",
        changeOrigin: true,
        secure: false, 
      },
    },
  },
});
