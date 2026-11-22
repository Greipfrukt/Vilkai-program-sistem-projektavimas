import { defineConfig, loadEnv, type ProxyOptions } from 'vite'
import react from '@vitejs/plugin-react'

// VITE_API_TARGET=mock    → Prism mock (:4010, 1 savaitę)
// VITE_API_TARGET=backend → gateway per docker compose (:8080)
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const useMock = env.VITE_API_TARGET !== 'backend'

  const proxy: Record<string, ProxyOptions> = useMock
    ? {
        // Prism tarnauja keliais be /api/v1 prefikso
        '/api/v1': {
          target: 'http://localhost:4010',
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/api\/v1/, ''),
        },
      }
    : {
        '/api': { target: 'http://localhost:8080', changeOrigin: false, ws: true },
      }

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy,
    },
  }
})
