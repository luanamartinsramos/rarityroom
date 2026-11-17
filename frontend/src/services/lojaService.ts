import { API_ORIGIN } from "../config";
export interface LojaApi {
  id: number;
  donoId: number;
  nome: string;
  descricao: string;
  fotoUrl: string;
  criadaEm: string;
}

const API = `${API_ORIGIN}/api/lojas`;

export async function obterMinhaLoja(): Promise<LojaApi | null> {
  const resposta = await fetch(`${API}/minha`, {
    method: "GET",
    credentials: "include",
  });

  if (resposta.status === 404) {
    return null;
  }

  if (!resposta.ok) {
    throw new Error("Não foi possível carregar sua loja.");
  }

  return resposta.json();
}

export async function criarLoja(
  nome: string,
  descricao: string,
): Promise<LojaApi> {
  const resposta = await fetch(API, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ nome, descricao }),
  });

  if (!resposta.ok) {
    const problema = await resposta.json().catch(() => null);

    throw new Error(problema?.detail ?? "Não foi possível criar a loja.");
  }

  return resposta.json();
}

export async function atualizarLoja(
  nome: string,
  descricao: string,
): Promise<LojaApi> {
  const resposta = await fetch(API, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ nome, descricao }),
  });

  if (!resposta.ok) {
    const problema = await resposta.json().catch(() => null);

    throw new Error(problema?.detail ?? "Não foi possível salvar a loja.");
  }

  return resposta.json();
}

export async function enviarFotoLoja(arquivo: File): Promise<LojaApi> {
  const formulario = new FormData();
  formulario.append("arquivo", arquivo);

  const resposta = await fetch(`${API}/foto`, {
    method: "POST",
    credentials: "include",
    body: formulario,
  });

  if (!resposta.ok) {
    const problema = await resposta.json().catch(() => null);

    throw new Error(problema?.detail ?? "Não foi possível enviar a foto.");
  }

  return resposta.json();
}
