import type { Produto } from "./produtosService";

export interface ItemCarrinho extends Produto {
  quantidade: number;
}

const CHAVE = "rarityroom.carrinho";
const EVENTO = "rarityroom:carrinho";

/** Carrinho persistido no navegador, compartilhado entre as páginas. */
export function lerCarrinho(): ItemCarrinho[] {
  try {
    const bruto = localStorage.getItem(CHAVE);

    if (!bruto) {
      return [];
    }

    const dados: unknown = JSON.parse(bruto);

    return Array.isArray(dados) ? (dados as ItemCarrinho[]) : [];
  } catch {
    return [];
  }
}

export function salvarCarrinho(itens: ItemCarrinho[]): void {
  localStorage.setItem(CHAVE, JSON.stringify(itens));
  window.dispatchEvent(new Event(EVENTO));
}

export function adicionarAoCarrinho(produto: Produto): ItemCarrinho[] {
  const itens = lerCarrinho();
  const existente = itens.find((item) => item.id === produto.id);
  const limite = produto.estoque;

  if (existente) {
    if (existente.quantidade >= limite) {
      throw new Error(
        `Só há ${limite} unidade(s) disponível(is) deste item.`,
      );
    }

    existente.quantidade += 1;
  } else {
    if (limite <= 0) {
      throw new Error("Este anúncio está sem estoque.");
    }

    itens.push({ ...produto, quantidade: 1 });
  }

  salvarCarrinho(itens);

  return itens;
}

export function removerDoCarrinho(id: number): ItemCarrinho[] {
  const itens = lerCarrinho().filter((item) => item.id !== id);

  salvarCarrinho(itens);

  return itens;
}

export function alterarQuantidade(
  id: number,
  quantidade: number,
): ItemCarrinho[] {
  const itens = lerCarrinho();
  const item = itens.find((atual) => atual.id === id);

  if (!item) {
    return itens;
  }

  const limite = Math.min(quantidade, item.estoque);

  if (limite <= 0) {
    return removerDoCarrinho(id);
  }

  item.quantidade = limite;
  salvarCarrinho(itens);

  return itens;
}

export function limparCarrinho(): void {
  salvarCarrinho([]);
}

/** Avisa a página quando o carrinho muda em outra aba ou componente. */
export function ouvirCarrinho(handler: () => void): () => void {
  window.addEventListener(EVENTO, handler);
  window.addEventListener("storage", handler);

  return () => {
    window.removeEventListener(EVENTO, handler);
    window.removeEventListener("storage", handler);
  };
}
