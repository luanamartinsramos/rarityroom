export interface PropostaApi {
  id: number;
  produtoId: number;
  proponenteId: number;
  valor: number;
  mensagem: string;
  estado: string;
  criadaEm: string;
}

const API = "http://localhost:5000/api/propostas";

export async function obterPropostas(): Promise<PropostaApi[]> {
  const resposta = await fetch(API, {
    method: "GET",
    credentials: "include",
  });

  if (!resposta.ok) {
    throw new Error("Não foi possível carregar suas propostas.");
  }

  return resposta.json();
}

async function agir(id: number, acao: string): Promise<void> {
  const resposta = await fetch(`${API}/${id}/${acao}`, {
    method: "POST",
    credentials: "include",
  });

  if (!resposta.ok) {
    const problema = await resposta.json().catch(() => null);

    throw new Error(
      problema?.detail ?? "Não foi possível concluir a operação.",
    );
  }
}

export const aceitarProposta = (id: number) => agir(id, "aceitar");

export const recusarProposta = (id: number) => agir(id, "recusar");

export const cancelarProposta = (id: number) => agir(id, "cancelar");
