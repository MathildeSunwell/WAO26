"use client";

import { useEffect } from "react";
import { usePathname } from "next/navigation";

//
// 1) Define the shape of the Google response so `response.credential` is typed
//
interface CredentialResponse {
  credential: string;
  clientId?: string;
  select_by?: string;
}

//
// 2) Tell TypeScript about `window.google` so it won’t complain.
//
declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (opts: {
            client_id: string;
            callback: (res: CredentialResponse) => void;
            auto_select?: boolean;
            cancel_on_tap_outside?: boolean;
          }) => void;
          renderButton: (
            container: HTMLElement,
            options: { theme?: "outline" | string; size?: "large" | string }
          ) => void;
          prompt?: () => void;
        };
      };
    };
  }
}

export default function LoginButton() {
  const pathname = usePathname() || "/";
  const basePath = process.env.NEXT_PUBLIC_BASE_PATH!;
  const returnTo = encodeURIComponent(basePath + pathname);
  const loginUrl = `${basePath}/api/login/google?returnTo=${returnTo}`;

  useEffect(() => {
    const clientId = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID!;
    if (!window.google?.accounts?.id) {
      console.error("Google Identity Services not loaded");
      return;
    }

    // Initialize the one‐tap / button flow
    window.google.accounts.id.initialize({
      client_id: clientId,
      callback: async (response: CredentialResponse) => {
        if (!response.credential) {
          alert("No credential returned");
          return;
        }

        // 3) POST the JWT to your gateway (include cookies so set-cookie is respected)
        const res = await fetch(loginUrl, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include", // <–– send & receive cookies
          redirect: "manual", // <–– don’t auto-follow the redirect
          body: JSON.stringify({ credential: response.credential }),
        });

        console.log("got status", res.status, "headers:", {
          location: res.headers.get("Location"),
        });

        // 4) Grab the gateway’s 302 “Location” header and nav there
        if (res.status === 302) {
          const location = res.headers.get("Location");
          if (location) window.location.href = location;
        } else {
          const text = await res.text();
          alert("Login failed: " + text);
        }
      },
    });

    // Render Google’s branded button into our div
    window.google.accounts.id.renderButton(
      document.getElementById("g_id_signin")!,
      { theme: "outline", size: "large" }
    );
  }, [returnTo, loginUrl]);

  return <div id="g_id_signin" />;
}
