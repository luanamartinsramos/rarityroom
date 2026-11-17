import type { AvaliacaoApi } from "./avaliacoesService";
import type { LojaApi } from "./lojaService";
import type { Produto } from "./produtosService";

export interface VendedorApi {
  id: number;
  nome: string;
  tipoConta: string;
  desde: string;
  reputacaoMedia: number;
  totalAvaliacoes: number;
  anunciosAtivos: number;
  produtosVendidos: number;
  avaliacoes: AvaliacaoApi[];
  anuncios: Produto[];
  loja?: LojaApi | null;
}

const API = "http://localhost:5000/api";

export async function obterProduto(id: number): Promise<Produto> {
  const resposta = await fetch(`${API}/produtos/${id}`, {
    method: "GET",
    credentials: "include",
  });

  if (!resposta.ok) {
    throw new Error("Anúncio não encontrado.");
  }

  return resposta.json();
}

export async function obterVendedor(id: number): Promise<VendedorApi> {
  const resposta = await fetch(`${API}/vendedores/${id}`, {
    method: "GET",
    credentials: "include",
  });

  if (!resposta.ok) {
    throw new Error("Vendedor não encontrado.");
  }

  return resposta.json();
}
