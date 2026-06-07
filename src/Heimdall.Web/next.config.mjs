/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  // Static export: the dashboard is a pure client-side SPA served by the API (wwwroot) and
  // loaded in the desktop WebView2 shell — no Node web server at runtime.
  output: "export",
  images: { unoptimized: true },
};

export default nextConfig;
