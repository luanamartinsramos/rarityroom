export function classeSelo(status: string): string {
  if (["Pago", "Enviado", "Entregue", "Concluido"].includes(status)) return "ok";
  if (status === "Pendente") return "alerta";
  if (["Recusado", "Expirado", "Cancelado", "Estornado"].includes(status)) return "erro";
  return "";
}

const ETAPAS = ["Pendente", "Pago", "Enviado", "Entregue", "Concluido"];

export const NOMES_ETAPAS = ["Pedido feito", "Pagamento aprovado", "Enviado", "Entregue", "Concluído"];

/** Índice da última etapa alcançada (–1 quando cancelado). */
export function etapaAtual(estado: string): number {
  return estado === "Cancelado" ? -1 : ETAPAS.indexOf(estado);
}

export async function obterMeuId(): Promise<number> {
  const resposta = await fetch("http://localhost:5000/api/autenticacao/usuário-atual", {
    credentials: "include",
  });

  if (!resposta.ok) {
    return 0;
  }

  const dados = await resposta.json();

  return Number(dados.id);
}
