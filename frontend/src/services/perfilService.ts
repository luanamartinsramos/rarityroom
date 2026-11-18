export interface PerfilApi {
  id: number;
  nome: string;
  email: string;
  tipoConta: string;
  administrador: boolean;
  fotoUrl: string;
  desde: string;
}

const API = "/api/perfil";

export async function obterPerfil(): Promise<PerfilApi> {
  const resposta = await fetch(API, { credentials: "include" });

  if (!resposta.ok) {
    throw new Error("Não foi possível carregar seu perfil.");
  }

  return resposta.json();
}

export async function alterarNome(nome: string): Promise<PerfilApi> {
  const resposta = await fetch(`${API}/nome`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ nome }),
  });

  if (!resposta.ok) {
    const problema = await resposta.json().catch(() => null);

    throw new Error(problema?.detail ?? "Não foi possível salvar o nome.");
  }

  return resposta.json();
}

/** Trocar o e-mail (login) exige a senha atual. */
export async function alterarEmail(
  email: string,
  senhaAtual: string,
): Promise<PerfilApi> {
  const resposta = await fetch(`${API}/email`, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, senhaAtual }),
  });

  if (!resposta.ok) {
    const problema = await resposta.json().catch(() => null);

    throw new Error(problema?.detail ?? "Não foi possível salvar o e-mail.");
  }

  return resposta.json();
}

export async function enviarFotoPerfil(arquivo: File): Promise<PerfilApi> {
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
