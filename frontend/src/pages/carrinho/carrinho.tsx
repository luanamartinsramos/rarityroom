import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router-dom";

import {
  alterarQuantidade,
  carrinhoVazio,
  limparCarrinho,
  obterCarrinho,
  removerDoCarrinho,
  type Carrinho,
  type ItemCarrinho,
} from "../../services/carrinhoService";
import { moeda, urlImagem } from "../../utils/formato";

import "../shared/paginas.css";
import "../shared/compra.css";

function CarrinhoPagina() {
  const navigate = useNavigate();

  const [carrinho, setCarrinho] = useState<Carrinho>(carrinhoVazio);
  const [carregando, setCarregando] = useState(true);
  const [ocupado, setOcupado] = useState<number | null>(null);

  useEffect(() => {
    obterCarrinho()
      .then(setCarrinho)
      .catch((falha: Error) => toast.error(falha.message))
      .finally(() => setCarregando(false));
  }, []);

  async function executar(produtoId: number, acao: () => Promise<Carrinho>) {
    setOcupado(produtoId);

    try {
      setCarrinho(await acao());
    } catch (falha) {
      toast.error(falha instanceof Error ? falha.message : "Falha ao atualizar o carrinho.");

      // Mostra o estado real (ex.: estoque mudou enquanto a página estava aberta).
      setCarrinho(await obterCarrinho().catch(() => carrinho));
    } finally {
      setOcupado(null);
    }
  }

  function mudarQuantidade(item: ItemCarrinho, quantidade: number) {
    if (quantidade < 1) {
      return;
    }

    void executar(item.produtoId, () => alterarQuantidade(item.produtoId, quantidade));
  }

  function remover(item: ItemCarrinho) {
    void executar(item.produtoId, async () => {
      const atualizado = await removerDoCarrinho(item.produtoId);
      toast(`${item.nome} saiu do carrinho.`);
      return atualizado;
    });
  }

  async function esvaziar() {
    if (!window.confirm("Remover todos os itens do carrinho?")) {
      return;
    }

    await executar(-1, limparCarrinho);
  }

  if (carregando) {
    return (
      <main className="pagina">
        <div className="pagina-carregando">Carregando seu carrinho...</div>
      </main>
    );
  }

  return (
    <main className="pagina">
      <div className="pagina-topo">
        <button onClick={() => navigate("/home")}>← Continuar comprando</button>
        <h1>Carrinho</h1>
      </div>

      {carrinho.itens.length === 0 ? (
        <div className="pagina-vazio">
          <span style={{ fontSize: 40 }}>🛒</span>
          <h3>Seu carrinho está vazio</h3>
          <p>Adicione produtos do catálogo para começar sua compra.</p>
          <button className="botao-primario" onClick={() => navigate("/home")}>
            Explorar produtos
          </button>
        </div>
      ) : (
        <div className="compra-layout">
          <section className="compra-bloco">
            <h2>
              {carrinho.quantidadeItens} {carrinho.quantidadeItens === 1 ? "unidade" : "unidades"}
              {" · "}
              {carrinho.itens.length} {carrinho.itens.length === 1 ? "produto" : "produtos"}
            </h2>

            {carrinho.itens.map((item) => (
              <div className="item-compra" key={item.produtoId}>
                <div
                  className="item-compra-imagem"
                  onClick={() => navigate(`/produto/${item.produtoId}`)}
                >
                  {item.imagemUrl ? <img src={urlImagem(item.imagemUrl)} alt={item.nome} /> : <span>◇</span>}
                </div>

                <div className="item-compra-info">
                  <strong onClick={() => navigate(`/produto/${item.produtoId}`)}>{item.nome}</strong>
                  <small>
                    Vendido por{" "}
                    <button className="link-texto" onClick={() => navigate(`/vendedor/${item.vendedorId}`)}>
                      {item.vendedorNome}
                    </button>
                  </small>
                  <small>
                    {moeda(item.precoUnitario)} cada
                    {item.precoDeProposta && " · preço da sua proposta aceita"}
                    {" · "}
                    {item.estoqueDisponivel} disponível(is)
                  </small>
                  {item.aviso && <span className="item-aviso">⚠ {item.aviso}</span>}

                  <div className="acoes-linha" style={{ marginTop: 8 }}>
                    <div className="seletor-quantidade">
                      <button
                        aria-label="Diminuir quantidade"
                        disabled={ocupado !== null || item.quantidade <= 1}
                        onClick={() => mudarQuantidade(item, item.quantidade - 1)}
                      >
                        −
                      </button>
                      <span>{item.quantidade}</span>
                      <button
                        aria-label="Aumentar quantidade"
                        disabled={ocupado !== null || item.quantidade >= item.estoqueDisponivel}
                        onClick={() => mudarQuantidade(item, item.quantidade + 1)}
                      >
                        +
                      </button>
                    </div>

                    {item.quantidade > item.estoqueDisponivel && item.estoqueDisponivel > 0 && (
                      <button
                        className="link-texto"
                        onClick={() => mudarQuantidade(item, item.estoqueDisponivel)}
                      >
                        Ajustar para {item.estoqueDisponivel}
                      </button>
                    )}

                    <button
                      className="link-texto"
                      disabled={ocupado !== null}
                      onClick={() => remover(item)}
                    >
                      Remover
                    </button>
                  </div>
                </div>

                <div className="item-compra-valor">
                  <small style={{ color: "#9b8fb0" }}>Subtotal</small>
                  <strong>{moeda(item.subtotal)}</strong>
                </div>
              </div>
            ))}

            <div className="acoes-linha">
              <button className="botao-secundario" onClick={esvaziar} disabled={ocupado !== null}>
                Esvaziar carrinho
              </button>
            </div>
          </section>

          <aside className="compra-bloco compra-resumo">
            <h2>Resumo</h2>

            <div className="compra-linha">
              <span>Produtos ({carrinho.quantidadeItens})</span>
              <span>{moeda(carrinho.total)}</span>
            </div>

            <div className="compra-linha">
              <span>Frete</span>
              <span>A combinar com o vendedor</span>
            </div>

            <div className="compra-linha total">
              <span>Total</span>
              <strong>{moeda(carrinho.total)}</strong>
            </div>

            {!carrinho.podeFinalizar && (
              <p className="item-aviso">
                Corrija os itens com aviso (quantidade ou disponibilidade) para finalizar.
              </p>
            )}

            <button
              className="botao-primario botao-largo"
              disabled={!carrinho.podeFinalizar || ocupado !== null}
              onClick={() => navigate("/checkout")}
            >
              Finalizar compra →
            </button>
          </aside>
        </div>
      )}
    </main>
  );
}

export default CarrinhoPagina;
