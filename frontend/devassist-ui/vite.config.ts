import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// SpaProxy (Visual Studio style) runs Vite on 5173; the API on 5147 proxies the UI in Development.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': 'http://localhost:5147',
      '/health': 'http://localhost:5147',
    },
  },
})
