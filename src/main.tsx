import { createRoot } from "react-dom/client";
import { Toaster } from "react-hot-toast";
import { BrowserRouter } from "react-router-dom";
import "./global.css";
import AppRoutes from "./routes/AppRoutes.tsx";

createRoot(document.getElementById("root")!).render(
  <BrowserRouter>
    <Toaster
      position="top-right"
      toastOptions={{
        duration: 4000,
        style: {
          background: "#151020",
          color: "#ffffff",
          border: "1px solid #7b4bc4",
          borderRadius: "14px",
          padding: "16px 18px",
          fontFamily: "DM Sans, sans-serif",
          boxShadow: "0 12px 40px rgba(0, 0, 0, 0.45)",
        },
      }}
    />

    <AppRoutes />
  </BrowserRouter>,
);
