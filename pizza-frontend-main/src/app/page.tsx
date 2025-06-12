"use client";

import { useEffect, useState } from "react";
import LoginButton from "./LoginButton";

type User = { name: string };

const basePath = process.env.NEXT_PUBLIC_BASE_PATH;

export default function HomePage() {
  const [user, setUser] = useState<User | null>(null);

  useEffect(() => {
    fetch(`${basePath}/api/auth/me`, { credentials: "include" })
      .then((r) => (r.ok ? r.json() : Promise.reject()))
      .then((json) => setUser(json.user))
      .catch(() => setUser(null));
  }, []);

  if (!user) return <LoginButton />;

  return (
    <div>
      <h1>Welcome, {user.name}!</h1>
      <p>
        Use the links above to create a new order or view your existing orders.
      </p>
    </div>
  );
}
