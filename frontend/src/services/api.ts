export const API_BASE = "/api";

/** Erro da API com a mensagem de negócio enviada pelo servidor. */
export class ErroApi extends Error {
  status: number;

  constructor(mensagem: string, status: number) {
    super(mensagem);
    this.status = status;
  }
}

/**
 * Requisição autenticada por cookie. Em caso de erro, usa o "detail" do
 * ProblemDetails do backend (ex.: "Estoque insuficiente...").
 */
export async function requisitar<T>(
  caminho: string,
  opcoes: { metodo?: string; corpo?: unknown } = {},
): Promise<T> {
  let resposta: Response;

  try {
    resposta = await fetch(`${API_BASE}${caminho}`, {
      method: opcoes.metodo ?? "GET",
      credentials: "include",
      headers:
        opcoes.corpo !== undefined
          ? { "Content-Type": "application/json" }
          : undefined,
      body: opcoes.corpo !== undefined ? JSON.stringify(opcoes.corpo) : undefined,
    });
  } catch {
    throw new ErroApi("Não foi possível conectar ao servidor.", 0);
  }

  if (!resposta.ok) {
    const problema = await resposta.json().catch(() => null);

    const mensagem =
      resposta.status === 401
        ? "Entre na sua conta para continuar."
        : (problema?.detail ?? problema?.mensagem ?? "Não foi possível concluir a operação.");

    throw new ErroApi(mensagem, resposta.status);
  }

  if (resposta.status === 204) {
    return undefined as T;
  }

  return resposta.json() as Promise<T>;
}
