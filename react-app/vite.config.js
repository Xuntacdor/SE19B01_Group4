import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    https: false,
    proxy: {
      "/api": {
        // target: "https://webapi-823340438485.asia-southeast1.run.app",
        // changeOrigin: true,
        // secure: true, 
         target: "https://localhost:7264",
        changeOrigin: true,
        secure: false, 
      },
    },
  },
});
