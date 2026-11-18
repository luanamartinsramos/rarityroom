import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// O frontend chama a API por caminhos relativos (/api, /uploads). Em
// produção o backend serve o frontend na mesma origem; em desenvolvimento
// este proxy encaminha essas chamadas para o backend local. Mesma origem =
// cookie de login sem problemas de CORS/SameSite.
const backend = 'http://localhost:5000'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': backend,
      '/uploads': backend,
    },
  },
})
