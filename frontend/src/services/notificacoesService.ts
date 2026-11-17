export interface NotificacaoApi {
  id: number;
  usuarioId: number;
  tipo: string;
  mensagem: string;
  lida: boolean;
  criadaEm: string;
}

export interface CentralNotificacoes {
  naoLidas: number;
  itens: NotificacaoApi[];
}

const API = "http://localhost:5000/api/notificacoes";

export async function obterNotificacoes(): Promise<CentralNotificacoes> {
  const resposta = await fetch(API, {
    method: "GET",
    credentials: "include",
  });

  if (resposta.status === 401) {
    return { naoLidas: 0, itens: [] };
  }

  if (!resposta.ok) {
    throw new Error("Não foi possível carregar as notificações.");
  }

  return resposta.json();
}

export async function marcarComoLida(id: number): Promise<void> {
  await fetch(`${API}/${id}/ler`, {
    method: "POST",
    credentials: "include",
  });
}

export async function marcarTodasComoLidas(): Promise<void> {
  await fetch(`${API}/ler-todas`, {
    method: "POST",
    credentials: "include",
  });
}
