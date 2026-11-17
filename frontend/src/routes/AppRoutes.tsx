import { Navigate, Route, Routes } from "react-router-dom";

import Home from "../pages/home/home";
import Login from "../pages/login/login";
import Register from "../pages/register/register";
import ProdutoPagina from "../pages/produto/produto";
import VendedorPagina from "../pages/vendedor/vendedor";
import NotificacoesPagina from "../pages/notificacoes/notificacoes";
import AdminPagina from "../pages/admin/admin";
import ContaPagina from "../pages/conta/conta";
import PerfilPagina from "../pages/perfil/perfil";
import RecuperarPagina from "../pages/recuperar/recuperar";
import NovoAnuncioPagina from "../pages/anuncio/novo";
import CarrinhoPagina from "../pages/carrinho/carrinho";
import CheckoutPagina from "../pages/checkout/checkout";
import PagamentoPagina from "../pages/pagamento/pagamento";
import PedidosPagina from "../pages/pedidos/pedidos";
import PedidoDetalhePagina from "../pages/pedidos/pedidoDetalhe";

import ProtectedRoute from "./ProtectedRoute";

function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/login" replace />} />

      <Route path="/login" element={<Login />} />

      <Route path="/register" element={<Register />} />

      <Route path="/recuperar-senha" element={<RecuperarPagina />} />

      <Route
        path="/home"
        element={
          <ProtectedRoute>
            <Home />
          </ProtectedRoute>
        }
      />

      <Route
        path="/produto/:id"
        element={
          <ProtectedRoute>
            <ProdutoPagina />
          </ProtectedRoute>
        }
      />

      <Route
        path="/vendedor/:id"
        element={
          <ProtectedRoute>
            <VendedorPagina />
          </ProtectedRoute>
        }
      />

      <Route
        path="/notificacoes"
        element={
          <ProtectedRoute>
            <NotificacoesPagina />
          </ProtectedRoute>
        }
      />

      <Route
        path="/conta"
        element={
          <ProtectedRoute>
            <ContaPagina />
          </ProtectedRoute>
        }
      />

      <Route
        path="/anuncios/novo"
        element={
          <ProtectedRoute>
            <NovoAnuncioPagina />
          </ProtectedRoute>
        }
      />

      <Route
        path="/perfil"
        element={
          <ProtectedRoute>
            <PerfilPagina />
          </ProtectedRoute>
        }
      />

      <Route
        path="/admin"
        element={
          <ProtectedRoute>
            <AdminPagina />
          </ProtectedRoute>
        }
      />
      <Route
        path="/carrinho"
        element={
          <ProtectedRoute>
            <CarrinhoPagina />
          </ProtectedRoute>
        }
      />

      <Route
        path="/checkout"
        element={
          <ProtectedRoute>
            <CheckoutPagina />
          </ProtectedRoute>
        }
      />

      <Route
        path="/pagamento/:id"
        element={
          <ProtectedRoute>
            <PagamentoPagina />
          </ProtectedRoute>
        }
      />

      <Route
        path="/pedidos"
        element={
          <ProtectedRoute>
            <PedidosPagina />
          </ProtectedRoute>
        }
      />

      <Route
        path="/pedidos/:id"
        element={
          <ProtectedRoute>
            <PedidoDetalhePagina />
          </ProtectedRoute>
        }
      />
    </Routes>
  );
}

export default AppRoutes;
