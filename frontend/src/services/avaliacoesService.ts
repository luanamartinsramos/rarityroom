import { API_ORIGIN } from "../config";
export interface AvaliacaoApi {
  id: number;
  pedidoId: number;
  autorId: number;
  vendedorId: number;
  nota: number;
  comentario: string;
  criadaEm: string;
}

export interface ResumoAvaliacoes {
  produtoId: number;
  media: number;
  total: number;
  avaliacoes: AvaliacaoApi[];
}

export async function obterResumoProduto(
  produtoId: number,
): Promise<ResumoAvaliacoes> {
  const resposta = await fetch(
    `${API_ORIGIN}/api/avaliacoes/produto/${produtoId}`,
    { method: "GET", credentials: "include" },
  );

  if (!resposta.ok) {
    throw new Error("Não foi possível carregar as avaliações do produto.");
  }

  return resposta.json();
}
