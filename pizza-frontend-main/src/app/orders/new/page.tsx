"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";

export default function NewOrderPage() {
  const router = useRouter();
  const [address, setAddress] = useState("");
  const [product, setProduct] = useState("");
  const [size, setSize] = useState("");
  const [quantity, setQuantity] = useState(1);
  const [price, setPrice] = useState(0);

  const basePath = process.env.NEXT_PUBLIC_BASE_PATH;

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    const totalPrice = price * quantity; // auto-calc; let me know if you’d rather enter manually
    const body = {
      customerAddress: address,
      totalPrice,
      orderItems: [{ productName: product, size, quantity, price }],
    };
    const res = await fetch(`${basePath}/orders`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      alert("Error creating order");
      return;
    }
    const text = await res.text(); // "Order created with OrderID: xxx"
    console.log("jfkdlsæfjasf: " + text);
    const id = text.split(":").pop()?.trim();
    if (id) router.push(`${id}`);
  }

  return (
    <form onSubmit={handleSubmit} style={{ maxWidth: 400 }}>
      <h1>Create Order</h1>
      <label>
        Address
        <br />
        <input
          value={address}
          onChange={(e) => setAddress(e.target.value)}
          required
        />
      </label>
      <hr />
      <label>
        Product Name
        <br />
        <input
          value={product}
          onChange={(e) => setProduct(e.target.value)}
          required
        />
      </label>
      <label>
        Size
        <br />
        <input
          value={size}
          onChange={(e) => setSize(e.target.value)}
          required
        />
      </label>
      <label>
        Quantity
        <br />
        <input
          type="number"
          value={quantity}
          onChange={(e) => setQuantity(+e.target.value)}
          min={1}
          required
        />
      </label>
      <label>
        Price per Item
        <br />
        <input
          type="number"
          step="0.01"
          value={price}
          onChange={(e) => setPrice(+e.target.value)}
          required
        />
      </label>

      <button type="submit" style={{ marginTop: 16 }}>
        Submit
      </button>
    </form>
  );
}
