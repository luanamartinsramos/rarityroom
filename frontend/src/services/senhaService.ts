import { API_ORIGIN } from "../config";
const API = `${API_ORIGIN}/api/autenticacao`;

export async function solicitarRecuperacao(
  email: string,
): Promise<string | null> {
  const resposta = await fetch(`${API}/recuperar-senha`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email }),
  });

  if (!resposta.ok) {
    throw new Error("Não foi possível solicitar a recuperação.");
  }

  const dados = await resposta.json();

  return dados.token ?? null;
}

export async function redefinirSenha(
  email: string,
  token: string,
  novaSenha: string,
): Promise<void> {
  const resposta = await fetch(`${API}/redefinir-senha`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, token, novaSenha }),
  });

  if (!resposta.ok) {
    const problema = await resposta.json().catch(() => null);

    throw new Error(problema?.detail ?? "Não foi possível redefinir a senha.");
  }
}
