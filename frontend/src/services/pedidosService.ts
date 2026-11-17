import { requisitar } from "./api";

export interface EntregaApi {
  cep: string;
  logradouro: string;
  numero: string;
  complemento: string;
  bairro: string;
  cidade: string;
  estado: string;
}

export interface ItemPedidoApi {
  id: number;
  produtoId: number;
  nomeProduto: string;
  imagemUrl: string;
  precoUnitario: number;
  quantidade: number;
  subtotal: number;
}

export interface PedidoApi {
  id: number;
  codigo: string;
  pagamentoId: number | null;
  compradorId: number;
  vendedorId: number;
  itens: ItemPedidoApi[];
  total: number;
  nomeProduto: string;
  quantidadeTotal: number;
  entrega: EntregaApi;
  formaPagamento: FormaPagamento;
  estado: string;
  statusPagamento: string;
  criadoEm: string;
  pagoEm: string | null;
  concluidoEm: string | null;
  canceladoEm: string | null;
}

export type FormaPagamento = "Pix" | "Boleto" | "CartaoCredito";

/** Compra (checkout) com o pagamento SIMULADO e seus pedidos. */
export interface CompraApi {
  id: number;
  codigo: string;
  forma: FormaPagamento;
  status: string;
  valor: number;
  parcelas: number;
  valorParcela: number;
  criadoEm: string;
  expiraEm: string;
  processadoEm: string | null;
  codigoPix: string;
  linhaDigitavel: string;
  codigoBarras: string;
  cartaoFinal: string;
  cartaoBandeira: string;
  motivo: string;
  simulacao: boolean;
  pedidos: PedidoApi[];
}

export interface CheckoutPayload extends EntregaApi {
  formaPagamento: FormaPagamento;
  chaveIdempotencia: string;
}

export interface CartaoPayload {
  nomeImpresso: string;
  numero: string;
  validade: string;
  cvv: string;
  cpf: string;
  parcelas: number;
}

export const ROTULO_FORMA: Record<string, string> = {
  Pix: "PIX",
  Boleto: "Boleto bancário",
  CartaoCredito: "Cartão de crédito",
};

export const ROTULO_PAGAMENTO: Record<string, string> = {
  Pendente: "Aguardando pagamento",
  Pago: "Pagamento aprovado",
  Recusado: "Pagamento recusado",
  Expirado: "Pagamento expirado",
  Cancelado: "Compra cancelada",
  Estornado: "Valor estornado",
  "": "—",
};

export const ROTULO_PEDIDO: Record<string, string> = {
  Pendente: "Aguardando pagamento",
  Pago: "Pago · preparando envio",
  Enviado: "Enviado",
  Entregue: "Entregue",
  Concluido: "Concluído",
  Cancelado: "Cancelado",
};

/** Pedidos do usuário autenticado (como comprador ou vendedor). */
export async function obterPedidos(): Promise<PedidoApi[]> {
  try {
    return await requisitar<PedidoApi[]>("/pedidos");
  } catch (falha) {
    if ((falha as { status?: number }).status === 401) {
      return [];
    }

    throw falha;
  }
}

export function obterPedido(id: number): Promise<PedidoApi> {
  return requisitar<PedidoApi>(`/pedidos/${id}`);
}

/** Checkout: itens, preços e quantidades vêm do carrinho no servidor. */
export function finalizarCompra(payload: CheckoutPayload): Promise<CompraApi> {
  return requisitar<CompraApi>("/pedidos", { metodo: "POST", corpo: payload });
}

export function obterCompra(id: number): Promise<CompraApi> {
  return requisitar<CompraApi>(`/pagamentos/${id}`);
}

export function simularConfirmacao(id: number): Promise<CompraApi> {
  return requisitar<CompraApi>(`/pagamentos/${id}/simular-confirmacao`, { metodo: "POST" });
}

export function simularExpiracao(id: number): Promise<CompraApi> {
  return requisitar<CompraApi>(`/pagamentos/${id}/simular-expiracao`, { metodo: "POST" });
}

export function pagarComCartao(id: number, cartao: CartaoPayload): Promise<CompraApi> {
  return requisitar<CompraApi>(`/pagamentos/${id}/cartao`, { metodo: "POST", corpo: cartao });
}

export function cancelarCompra(id: number): Promise<CompraApi> {
  return requisitar<CompraApi>(`/pagamentos/${id}/cancelar`, { metodo: "POST" });
}

export async function enviarPedido(id: number): Promise<void> {
  await requisitar(`/pedidos/${id}/enviar`, { metodo: "POST" });
}

export async function entregarPedido(id: number): Promise<void> {
  await requisitar(`/pedidos/${id}/entregar`, { metodo: "POST" });
}

export async function concluirPedido(id: number): Promise<void> {
  await requisitar(`/pedidos/${id}/concluir`, { metodo: "POST" });
}

export function cancelarPedido(id: number): Promise<PedidoApi> {
  return requisitar<PedidoApi>(`/pedidos/${id}/cancelar`, { metodo: "POST" });
}

export async function avaliarItem(
  pedidoId: number,
  produtoId: number,
  nota: number,
  comentario: string,
): Promise<void> {
  await requisitar("/avaliacoes", {
    metodo: "POST",
    corpo: { pedidoId, produtoId, nota, comentario },
  });
}
