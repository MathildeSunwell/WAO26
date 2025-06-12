import "./globals.css";
import Link from "next/link";

export const metadata = { title: "Order Frontend" };

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <head>
        <script
          src="https://accounts.google.com/gsi/client"
          async
          defer
        ></script>
        <link
          href="https://fonts.googleapis.com/css2?family=Roboto:wght@400;500;700&display=swap"
          rel="stylesheet"
        />
      </head>
      <body>
        <body></body>
        <nav style={{ padding: 16, borderBottom: "1px solid #ddd" }}>
          <Link href="/">Home</Link> |{" "}
          <Link href="/orders/new">Create Order</Link> |{" "}
          <Link href="/orders">My Orders</Link>
        </nav>
        <main style={{ padding: 16 }}>{children}</main>
      </body>
    </html>
  );
}
