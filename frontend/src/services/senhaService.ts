const API = "/api/autenticacao";

/**
 * Pede a redefinição. O token nunca volta na resposta: ele é "enviado por
 * e-mail" (no projeto, aparece no terminal do backend).
 */
export async function solicitarRecuperacao(email: string): Promise<string> {
  const resposta = await fetch(`${API}/recuperar-senha`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email }),
  });

  if (resposta.status === 429) {
    throw new Error("Muitas tentativas. Aguarde um minuto e tente novamente.");
  }

  if (!resposta.ok) {
    throw new Error("Não foi possível solicitar a recuperação.");
  }

  const dados = await resposta.json();

  return dados.mensagem ?? "Se este e-mail estiver cadastrado, enviamos as instruções.";
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
