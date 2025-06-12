"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";

type OrderDetail = {
  orderId: string;
  customerAddress: string;
  orderStatus: string;
  paymentStatus: string;
  restaurantStatus: string;
  deliveryStatus: string;
  createTime: string;
  lastUpdated: string;
};

const basePath = process.env.NEXT_PUBLIC_BASE_PATH;

export default function OrderDetailPage() {
  const { id } = useParams();
  const [order, setOrder] = useState<OrderDetail | null>(null);

  useEffect(() => {
    if (!id) return;
    fetch(`${basePath}/api/orders/${id}`)
      .then((res) => {
        if (!res.ok) throw new Error("Not found");
        return res.json();
      })
      .then(setOrder)
      .catch(() => alert("Order not found"));
  }, [id]);

  if (!order) return <p>Loading…</p>;

  return (
    <div>
      <h1>Order {order.orderId}</h1>
      <p>
        <strong>Address:</strong> {order.customerAddress}
        <strong>Status:</strong> {order.orderStatus} / {order.paymentStatus} /{" "}
        {order.deliveryStatus}
        <br />
        <strong>Created:</strong> {new Date(order.createTime).toLocaleString()}
      </p>
    </div>
  );
}
