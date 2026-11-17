export interface Produto {
  id: number;
  usuarioId: number;
  nome: string;
  descricao: string;
  preco: number;
  estoque: number;
  categoria: string;
  marca: string;
  colecao: string;
  condicao: string;
  imagemUrl: string;
  franquia: string;
  raridade: string;
  edicaoLimitada: boolean;
  destaque: boolean;
  estado: string;
  modalidade: string;
  disponivel: boolean;
}

export interface Categoria {
  id: number;
  nome: string;
  slug: string;
}

export interface FiltroProdutos {
  termo?: string;
  categoria?: string;
  estado?: string;
  franquia?: string;
  condicao?: string;
  precoMin?: number;
  precoMax?: number;
  ordem?: string;
  destaque?: boolean;
}

const API_PRODUTOS = "http://localhost:5000/api/produtos";
const API_CATEGORIAS = "http://localhost:5000/api/categorias";

/**
 * A pesquisa acontece no servidor: o termo é comparado sem diferenciar acento
 * nem caixa, e a categoria vem do catálogo real do backend.
 */
export async function obterProdutos(
  filtro: FiltroProdutos = {},
): Promise<Produto[]> {
  const parametros = new URLSearchParams();

  if (filtro.termo && filtro.termo.trim().length > 0) {
    parametros.set("termo", filtro.termo.trim());
  }

  if (filtro.categoria) {
    parametros.set("categoria", filtro.categoria);
  }

  if (filtro.estado) {
    parametros.set("estado", filtro.estado);
  }

  if (filtro.franquia) {
    parametros.set("franquia", filtro.franquia);
  }

  if (filtro.condicao) {
    parametros.set("condicao", filtro.condicao);
  }

  if (filtro.precoMin !== undefined) {
    parametros.set("precoMin", String(filtro.precoMin));
  }

  if (filtro.precoMax !== undefined) {
    parametros.set("precoMax", String(filtro.precoMax));
  }

  if (filtro.ordem) {
    parametros.set("ordem", filtro.ordem);
  }

  if (filtro.destaque) {
    parametros.set("destaque", "true");
  }

  const consulta = parametros.toString();

  const response = await fetch(
    consulta.length > 0 ? `${API_PRODUTOS}?${consulta}` : API_PRODUTOS,
    {
      method: "GET",
      credentials: "include",
    },
  );

  if (!response.ok) {
    throw new Error("Não foi possível carregar os produtos.");
  }

  return response.json();
}

export async function obterCategorias(): Promise<Categoria[]> {
  const response = await fetch(API_CATEGORIAS, {
    method: "GET",
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error("Não foi possível carregar as categorias.");
  }

  return response.json();
}
