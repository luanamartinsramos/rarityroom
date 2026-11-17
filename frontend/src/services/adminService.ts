import type { PedidoApi } from "./pedidosService";
import type { Produto } from "./produtosService";

export interface ResumoAdmin {
  usuarios: number;
  usuariosAtivos: number;
  anunciosAtivos: number;
  anunciosVendidos: number;
  pedidos: number;
  totalVendido: number;
}

export interface UsuarioAdmin {
  id: number;
  nome: string;
  email: string;
  tipoConta: string;
  ativo: boolean;
  administrador: boolean;
  criadoEm: string;
}

const API = "http://localhost:5000/api/admin";

async function pedir<T>(caminho: string, init?: RequestInit): Promise<T> {
  const resposta = await fetch(`${API}${caminho}`, {
    credentials: "include",
    ...init,
  });

  if (!resposta.ok) {
    throw new Error(
      resposta.status === 403
        ? "Área restrita a administradores."
        : "Não foi possível concluir a operação.",
    );
  }

  return resposta.json();
}

export function obterResumo(): Promise<ResumoAdmin> {
  return pedir<ResumoAdmin>("/resumo");
}

export function listarUsuarios(): Promise<UsuarioAdmin[]> {
  return pedir<UsuarioAdmin[]>("/usuarios");
}

export function suspenderUsuario(id: number): Promise<unknown> {
  return pedir(`/usuarios/${id}/suspender`, { method: "POST" });
}

export function reativarUsuario(id: number): Promise<unknown> {
  return pedir(`/usuarios/${id}/reativar`, { method: "POST" });
}

export function listarAnuncios(): Promise<Produto[]> {
  return pedir<Produto[]>("/anuncios");
}

export function moderarAnuncio(id: number, acao: string): Promise<unknown> {
  return pedir(`/anuncios/${id}/moderar?acao=${encodeURIComponent(acao)}`, {
    method: "POST",
  });
}

export function listarPedidos(): Promise<PedidoApi[]> {
  return pedir<PedidoApi[]>("/pedidos");
}
