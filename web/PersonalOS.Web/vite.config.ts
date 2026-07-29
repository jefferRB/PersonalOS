import { defineConfig } from 'vitest/config';
import plugin from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    server: {
        port: 61960,
        proxy: {
            '/api': {
                target: 'https://localhost:7268',
                changeOrigin: true,
                secure: false,
            },
            '/health': {
                target: 'https://localhost:7268',
                changeOrigin: true,
                secure: false,
            },
        },
    },
    test: {
        environment: 'jsdom',
        globals: true,
        setupFiles: './src/test/setup.ts',
        css: true,
    },
})
