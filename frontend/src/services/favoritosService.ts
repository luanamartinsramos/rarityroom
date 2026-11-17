import type { Produto } from "./produtosService";

const API = "http://localhost:5000/api/favoritos";

export async function obterFavoritos(): Promise<Produto[]> {
  const resposta = await fetch(API, {
    method: "GET",
    credentials: "include",
  });

  if (!resposta.ok) {
    throw new Error("Não foi possível carregar seus favoritos.");
  }

  return resposta.json();
}

export async function removerFavorito(produtoId: number): Promise<void> {
  const resposta = await fetch(`${API}/${produtoId}`, {
    method: "DELETE",
    credentials: "include",
  });

  if (!resposta.ok) {
    throw new Error("Não foi possível remover o favorito.");
  }
}

export async function adicionarFavorito(produtoId: number): Promise<void> {
  const resposta = await fetch(`${API}/${produtoId}`, {
    method: "POST",
    credentials: "include",
  });

  if (!resposta.ok) {
    throw new Error("Não foi possível favoritar.");
  }
}
