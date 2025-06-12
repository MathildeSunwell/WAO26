import type { NextConfig } from "next";

const rewriteDestination = process.env.REWRITE_DESTINATION;

const nextConfig: NextConfig = {
  basePath: process.env.NEXT_PUBLIC_BASE_PATH,
  output: "standalone",
  async headers() {
    return [
      {
        // apply to all routes under your basePath
        source: `/:path*`,
        headers: [
          {
            key: "Cross-Origin-Opener-Policy",
            value: "unsafe-none",
          },
          {
            key: "Cross-Origin-Embedder-Policy",
            value: "unsafe-none",
          },
        ],
      },
    ];
  },
};

if (rewriteDestination) {
  nextConfig.rewrites = async () => [
    {
      source: "/api/:path*",
      destination: rewriteDestination,
    },
  ];
}

export default nextConfig;
