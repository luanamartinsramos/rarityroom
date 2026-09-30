export interface Produto {
  id: number;
  nome: string;
  descricao: string;
  preco: number;
  estoque: number;
  categoria: string;
  marca: string;
  colecao: string;
  condicao: string;
  imagemUrl: string;
}

const API_URL = "http://localhost:5000/api/Produtos";

export async function obterProdutos(): Promise<Produto[]> {
  const response = await fetch(API_URL, {
    method: "GET",
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error("Não foi possível carregar os produtos.");
  }

  return response.json();
}
