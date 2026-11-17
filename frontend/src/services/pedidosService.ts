export interface EntregaApi {
  cep: string;
  logradouro: string;
  numero: string;
  complemento: string;
  bairro: string;
  cidade: string;
  estado: string;
}

export interface PedidoApi {
  id: number;
  produtoId: number;
  compradorId: number;
  vendedorId: number;
  nomeProduto: string;
  precoUnitario: number;
  estado: string;
  criadoEm: string;
  formaPagamento: string;
  entrega: EntregaApi;
}

export interface CheckoutPayload extends EntregaApi {
  produtoId: number;
  formaPagamento: string;
}

const API_PEDIDOS = "http://localhost:5000/api/pedidos";

/**
 * Pedidos do usuário autenticado (como comprador ou vendedor). Conta nova
 * recebe lista vazia — nada é inventado no cliente.
 */
export async function obterPedidos(): Promise<PedidoApi[]> {
  const resposta = await fetch(API_PEDIDOS, {
    method: "GET",
    credentials: "include",
  });

  if (resposta.status === 401) {
    return [];
  }

  if (!resposta.ok) {
    throw new Error("Não foi possível carregar seus pedidos.");
  }

  return resposta.json();
}

export async function criarPedido(
  payload: CheckoutPayload,
): Promise<PedidoApi> {
  const resposta = await fetch(API_PEDIDOS, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!resposta.ok) {
    const problema = await resposta.json().catch(() => null);

    throw new Error(
      problema?.detail ?? "Não foi possível concluir o pedido.",
    );
  }

  return resposta.json();
}

/** Pagamento demonstrativo — não há gateway real. */
export async function pagarPedido(id: number, aprovado: boolean): Promise<void> {
  const resposta = await fetch(`${API_PEDIDOS}/${id}/pagar`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ aprovado }),
  });

  if (!resposta.ok) {
    const problema = await resposta.json().catch(() => null);

    throw new Error(problema?.detail ?? "Não foi possível registrar o pagamento.");
  }
}

export async function enviarPedido(id: number): Promise<void> {
  const resposta = await fetch(`${API_PEDIDOS}/${id}/enviar`, {
    method: "POST",
    credentials: "include",
  });

  if (!resposta.ok) {
    throw new Error("Não foi possível marcar o pedido como enviado.");
  }
}

export async function entregarPedido(id: number): Promise<void> {
  const resposta = await fetch(`${API_PEDIDOS}/${id}/entregar`, {
    method: "POST",
    credentials: "include",
  });

  if (!resposta.ok) {
    throw new Error("Não foi possível confirmar a entrega.");
  }
}
