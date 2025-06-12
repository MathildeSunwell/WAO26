"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

type Order = {
  orderId: string;
  customerAddress: string;
  totalPrice: number;
  createTime: string;
};

const basePath = process.env.NEXT_PUBLIC_BASE_PATH;

export default function OrdersListPage() {
  const [query, setQuery] = useState("");
  const [orders, setOrders] = useState<Order[]>([]);

  async function fetchOrders() {
    const url = query
      ? `/api/orders?customerAddress=${encodeURIComponent(query)}`
      : "/api/orders";
    const res = await fetch(basePath + url);
    if (!res.ok) return alert("Error fetching orders");
    const data = await res.json();
    setOrders(data);
  }

  useEffect(() => {
    fetchOrders();
  }, []);

  return (
    <div>
      <h1>My Orders</h1>
      <label>
        Search by address:{" "}
        <input value={query} onChange={(e) => setQuery(e.target.value)} />
      </label>
      <button onClick={fetchOrders} style={{ marginLeft: 8 }}>
        Search
      </button>
      <ul>
        {orders.map((o) => (
          <li key={o.orderId}>
            <Link href={`/orders/${o.orderId}`}>
              {o.orderId} — {o.customerAddress} — ${o.totalPrice.toFixed(2)}
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}
