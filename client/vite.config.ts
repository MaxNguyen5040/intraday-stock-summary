import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

const apiTarget = process.env.API_PROXY_TARGET ?? 'http://localhost:5080'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Same-origin /api in dev, so the browser never needs CORS against the API.
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
    },
  },
})
