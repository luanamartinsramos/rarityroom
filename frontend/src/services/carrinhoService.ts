import { requisitar } from "./api";

/** Linha do carrinho calculada pelo servidor com os dados atuais do anúncio. */
export interface ItemCarrinho {
  produtoId: number;
  nome: string;
  imagemUrl: string;
  categoria: string;
  precoUnitario: number;
  quantidade: number;
  subtotal: number;
  estoqueDisponivel: number;
  vendedorId: number;
  vendedorNome: string;
  disponivel: boolean;
  precoDeProposta: boolean;
  aviso: string | null;
}

export interface Carrinho {
  itens: ItemCarrinho[];
  quantidadeItens: number;
  total: number;
  podeFinalizar: boolean;
}

const EVENTO = "rarityroom:carrinho";

export const carrinhoVazio: Carrinho = {
  itens: [],
  quantidadeItens: 0,
  total: 0,
  podeFinalizar: false,
};

/**
 * Carrinho ÚNICO da aplicação: fica no banco, vinculado ao usuário logado.
 * Catálogo, página do produto, favoritos e a página do carrinho usam estas
 * mesmas funções — a validação de estoque e de anúncio próprio é do servidor.
 */
function avisar(carrinho: Carrinho): Carrinho {
  window.dispatchEvent(new CustomEvent<Carrinho>(EVENTO, { detail: carrinho }));

  return carrinho;
}

export async function obterCarrinho(): Promise<Carrinho> {
  return avisar(await requisitar<Carrinho>("/carrinho"));
}

export async function adicionarAoCarrinho(
  produtoId: number,
  quantidade = 1,
): Promise<Carrinho> {
  return avisar(
    await requisitar<Carrinho>("/carrinho/itens", {
      metodo: "POST",
      corpo: { produtoId, quantidade },
    }),
  );
}

export async function alterarQuantidade(
  produtoId: number,
  quantidade: number,
): Promise<Carrinho> {
  return avisar(
    await requisitar<Carrinho>(`/carrinho/itens/${produtoId}`, {
      metodo: "PUT",
      corpo: { quantidade },
    }),
  );
}

export async function removerDoCarrinho(produtoId: number): Promise<Carrinho> {
  return avisar(
    await requisitar<Carrinho>(`/carrinho/itens/${produtoId}`, {
      metodo: "DELETE",
    }),
  );
}

export async function limparCarrinho(): Promise<Carrinho> {
  return avisar(await requisitar<Carrinho>("/carrinho", { metodo: "DELETE" }));
}

/** Avisa quando o carrinho muda em qualquer componente da página. */
export function ouvirCarrinho(handler: (carrinho: Carrinho) => void): () => void {
  const ouvinte = (evento: Event) => handler((evento as CustomEvent<Carrinho>).detail);

  window.addEventListener(EVENTO, ouvinte);

  return () => window.removeEventListener(EVENTO, ouvinte);
}
