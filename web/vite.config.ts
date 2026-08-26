import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';

// Dev server proxies the API routes straight through to the ASP.NET
// backend (DashBoard/Properties/launchSettings.json's "http" profile,
// port 5276) so the SPA can be developed against the real API with no
// CORS policy needed on the backend. DashboardController has no
// "/api" prefix, so each route it exposes is listed explicitly here.
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
    // H: is a DFS-mapped network drive here; fs.realpath resolves through
    // the reparse point to a UNC-style path that Vite/esbuild then mix
    // with the original drive-letter path, producing invalid combined
    // paths. Skipping realpath resolution avoids that entirely.
    preserveSymlinks: true,
  },
  server: {
    port: 5173,
    proxy: {
      '/tickets': 'http://localhost:5276',
      '/total': 'http://localhost:5276',
      '/sync': 'http://localhost:5276',
    },
  },
});
