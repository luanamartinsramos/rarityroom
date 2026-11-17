import { API_ORIGIN } from "../config";
export interface EnderecoApi {
  id: number;
  usuarioId: number;
  cep: string;
  logradouro: string;
  numero: string;
  complemento: string;
  bairro: string;
  cidade: string;
  estado: string;
  principal: boolean;
  criadoEm: string;
}

export interface EnderecoForm {
  cep: string;
  logradouro: string;
  numero: string;
  complemento: string;
  bairro: string;
  cidade: string;
  estado: string;
}

const API = `${API_ORIGIN}/api/enderecos`;

export async function obterEnderecos(): Promise<EnderecoApi[]> {
  const resposta = await fetch(API, { credentials: "include" });

  if (!resposta.ok) {
    throw new Error("Não foi possível carregar seus endereços.");
  }

  return resposta.json();
}

export async function criarEndereco(
  endereco: EnderecoForm,
): Promise<EnderecoApi> {
  const resposta = await fetch(API, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(endereco),
  });

  if (!resposta.ok) {
    const problema = await resposta.json().catch(() => null);

    throw new Error(problema?.detail ?? "Não foi possível salvar o endereço.");
  }

  return resposta.json();
}

export async function removerEndereco(id: number): Promise<void> {
  const resposta = await fetch(`${API}/${id}`, {
    method: "DELETE",
    credentials: "include",
  });

  if (!resposta.ok) {
    throw new Error("Não foi possível remover o endereço.");
  }
}

export async function definirPrincipal(id: number): Promise<void> {
  const resposta = await fetch(`${API}/${id}/principal`, {
    method: "POST",
    credentials: "include",
  });

  if (!resposta.ok) {
    throw new Error("Não foi possível definir o endereço principal.");
  }
}
