import { NextResponse } from "next/server";

export function middleware() {
  const res = NextResponse.next();
  // allow popups to postMessage back
  res.headers.set("Cross-Origin-Opener-Policy", "same-origin-allow-popups");
  res.headers.set("Cross-Origin-Embedder-Policy", "unsafe-none");
  return res;
}

// apply to all paths (including HMR, static, API rewrites, everything)
export const config = {
  matcher: "/:path*",
};
