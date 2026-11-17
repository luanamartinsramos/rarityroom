import toast from "react-hot-toast";

import { adicionarAoCarrinho } from "./carrinhoService";

/**
 * Ação única de "Adicionar ao carrinho", usada pelo catálogo, pelo modal de
 * detalhes, pelos favoritos e pela página do produto. Assim todos registram o
 * mesmo item (id do anúncio + quantidade) e recebem a mesma validação do
 * servidor: estoque, anúncio próprio e anúncio indisponível.
 */
export async function adicionarComAviso(
  produto: { id: number; nome: string },
  quantidade = 1,
): Promise<boolean> {
  try {
    await adicionarAoCarrinho(produto.id, quantidade);

    toast.success(
      quantidade > 1
        ? `${quantidade} unidades de ${produto.nome} foram para o carrinho! 🛒`
        : `${produto.nome} foi para o carrinho! 🛒`,
    );

    return true;
  } catch (falha) {
    toast.error(
      falha instanceof Error ? falha.message : "Não foi possível adicionar ao carrinho.",
    );

    return false;
  }
}
