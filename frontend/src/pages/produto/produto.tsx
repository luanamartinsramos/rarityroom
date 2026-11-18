import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate, useParams } from "react-router-dom";

import { adicionarComAviso } from "../../services/carrinhoAcoes";
import { moeda, urlImagem, rotulo } from "../../utils/formato";
import { obterMeuId } from "../pedidos/rotulos";
import { obterProduto, obterVendedor, type VendedorApi } from "../../services/catalogoService";
import { obterProdutos, type Produto } from "../../services/produtosService";
import {
  obterResumoProduto,
  type ResumoAvaliacoes,
} from "../../services/avaliacoesService";

import "../shared/paginas.css";
import "../shared/compra.css";

const API_FAVORITOS = "/api/favoritos";

/**
 * A chave pelo id faz a página recomeçar do zero ao navegar de um produto
 * para outro (ex.: produtos relacionados), sem resetar estado num efeito.
 */
function ProdutoPagina() {
  const { id } = useParams();

  return <ProdutoConteudo key={id} id={id} />;
}

function ProdutoConteudo({ id }: { id: string | undefined }) {
  const navigate = useNavigate();

  const [produto, setProduto] = useState<Produto | null>(null);
  const [vendedor, setVendedor] = useState<VendedorApi | null>(null);
  const [relacionados, setRelacionados] = useState<Produto[]>([]);
  const [avaliacoes, setAvaliacoes] = useState<ResumoAvaliacoes | null>(null);
  const [favoritado, setFavoritado] = useState(false);
  const [meuId, setMeuId] = useState(0);
  const [quantidade, setQuantidade] = useState(1);
  const [adicionando, setAdicionando] = useState(false);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");

  const produtoId = Number(id);
  const idValido = Number.isFinite(produtoId);

  useEffect(() => {
    if (!idValido) {
      return;
    }

    obterMeuId().then(setMeuId).catch(() => setMeuId(0));

    obterProduto(produtoId)
      .then(async (dados) => {
        setProduto(dados);

        const [dadosVendedor, mesmaCategoria, favoritos, resumo] = await Promise.all([
          obterVendedor(dados.usuarioId).catch(() => null),
          obterProdutos({ categoria: dados.categoria }).catch(() => []),
          fetch(API_FAVORITOS, { credentials: "include" })
            .then((resposta) => (resposta.ok ? resposta.json() : []))
            .catch(() => []),
          obterResumoProduto(dados.id).catch(() => null),
        ]);

        setAvaliacoes(resumo);

        setVendedor(dadosVendedor);
        setRelacionados(
          mesmaCategoria.filter((item) => item.id !== dados.id).slice(0, 4),
        );
        setFavoritado(
          Array.isArray(favoritos)
            && favoritos.some((item: Produto) => item.id === dados.id),
        );
      })
      .catch(() => setErro("Não foi possível carregar este anúncio."))
      .finally(() => setCarregando(false));
  }, [produtoId, idValido]);

  async function alternarFavorito() {
    if (!produto) {
      return;
    }

    const metodo = favoritado ? "DELETE" : "POST";

    const resposta = await fetch(`${API_FAVORITOS}/${produto.id}`, {
      method: metodo,
      credentials: "include",
    });

    if (!resposta.ok) {
      toast.error("Não foi possível atualizar o favorito.");
      return;
    }

    setFavoritado(!favoritado);

    toast.success(
      favoritado ? "Removido dos favoritos." : "Adicionado aos favoritos! ♡",
    );
  }

  /** Mesma ação do catálogo; o servidor valida estoque e anúncio próprio. */
  async function adicionar(irPara?: string) {
    if (!produto || adicionando) {
      return;
    }

    setAdicionando(true);

    const ok = await adicionarComAviso(produto, quantidade);

    setAdicionando(false);

    if (ok && irPara) {
      navigate(irPara);
    }
  }

  if (idValido && carregando) {
    return (
      <main className="pagina">
        <div className="pagina-carregando">Carregando anúncio...</div>
      </main>
    );
  }

  if (erro || !produto) {
    return (
      <main className="pagina">
        <div className="pagina-vazio">
          <h3>{!idValido ? "Anúncio inválido." : erro || "Anúncio não encontrado"}</h3>

          <button onClick={() => navigate("/home")}>Voltar ao catálogo</button>
        </div>
      </main>
    );
  }

  return (
    <main className="pagina">
      <div className="pagina-topo">
        <button onClick={() => navigate(-1)}>← Voltar</button>

        <h1>{produto.nome}</h1>
      </div>

      <div className="produto-detalhe">
        <div className="produto-imagem">
          {produto.imagemUrl ? (
            <img src={urlImagem(produto.imagemUrl)} alt={produto.nome} />
          ) : (
            <span>◇</span>
          )}
        </div>

        <div>
          <div className="produto-preco">{moeda(produto.preco)}</div>

          <div className="produto-tags">
            <span className="produto-tag">{produto.categoria}</span>
            <span className="produto-tag">{produto.condicao || "Sem condição"}</span>
            <span className="produto-tag">{rotulo(produto.raridade)}</span>
            {produto.franquia && (
              <span className="produto-tag">{produto.franquia}</span>
            )}
            {produto.edicaoLimitada && (
              <span className="produto-tag">Edição limitada</span>
            )}
            {produto.marca && <span className="produto-tag">{produto.marca}</span>}
            {produto.colecao && <span className="produto-tag">{produto.colecao}</span>}

            <span className="produto-tag">
              {avaliacoes && avaliacoes.total > 0
                ? `${avaliacoes.media} ★ (${avaliacoes.total})`
                : "Sem avaliações"}
            </span>

            {(produto.vendas ?? 0) > 0 && (
              <span className="produto-tag">{produto.vendas} vendido(s)</span>
            )}
          </div>

          <p className="produto-descricao">
            {produto.descricao || "O vendedor não escreveu uma descrição."}
          </p>

          <p style={{ color: produto.disponivel ? "#7fe3a6" : "#ff9c9c", margin: "14px 0 0" }}>
            {produto.disponivel
              ? `Estoque disponível: ${produto.estoque} unidade(s)`
              : produto.estado === "Reservado"
                ? "Reservado por proposta aceita"
                : produto.estado === "Pausado"
                  ? "Anúncio pausado pelo vendedor"
                  : "Esgotado"}
          </p>

          {produto.usuarioId === meuId ? (
            <div className="produto-acoes">
              <span className="produto-tag">Este anúncio é seu — gerencie em "Vender".</span>
              <button onClick={alternarFavorito}>
                {favoritado ? "♥ Favoritado" : "♡ Favoritar"}
              </button>
            </div>
          ) : (
            <>
              {(produto.disponivel || produto.estado === "Reservado") && (
                <div className="acoes-linha" style={{ alignItems: "center" }}>
                  <span style={{ color: "#c7bdda", fontSize: 14 }}>Quantidade</span>
                  <div className="seletor-quantidade">
                    <button
                      aria-label="Diminuir quantidade"
                      disabled={quantidade <= 1}
                      onClick={() => setQuantidade((atual) => Math.max(1, atual - 1))}
                    >
                      −
                    </button>
                    <span>{quantidade}</span>
                    <button
                      aria-label="Aumentar quantidade"
                      disabled={quantidade >= produto.estoque}
                      onClick={() =>
                        setQuantidade((atual) => Math.min(produto.estoque, atual + 1))
                      }
                    >
                      +
                    </button>
                  </div>
                  <small style={{ color: "#9b8fb0" }}>
                    Total: {moeda(produto.preco * quantidade)}
                  </small>
                </div>
              )}

              <div className="produto-acoes">
                <button
                  className="primario"
                  onClick={() => void adicionar("/checkout")}
                  disabled={adicionando || !(produto.disponivel || produto.estado === "Reservado")}
                >
                  Comprar agora
                </button>

                <button
                  onClick={() => void adicionar()}
                  disabled={adicionando || !(produto.disponivel || produto.estado === "Reservado")}
                >
                  {produto.disponivel || produto.estado === "Reservado"
                    ? "Adicionar ao carrinho"
                    : "Indisponível"}
                </button>

                <button onClick={alternarFavorito}>
                  {favoritado ? "♥ Favoritado" : "♡ Favoritar"}
                </button>

                <button onClick={() => navigate("/carrinho")}>Ir ao carrinho</button>
              </div>
            </>
          )}

          {vendedor && (
            <div
              className="vendedor-card"
              onClick={() => navigate(`/vendedor/${vendedor.id}`)}
            >
              <div className="vendedor-avatar">
                {vendedor.nome.charAt(0).toUpperCase()}
              </div>

              <div>
                <small style={{ color: "#9b8fb0" }}>Vendido por</small>
                <br />
                <strong>{vendedor.loja?.nome ?? vendedor.nome}</strong>

                <div>
                  {vendedor.totalAvaliacoes === 0
                    ? "Sem avaliações ainda"
                    : `${vendedor.reputacaoMedia} ★ (${vendedor.totalAvaliacoes})`}
                  {" · "}
                  {vendedor.produtosVendidos} anúncio(s) vendido(s)
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      <h2 style={{ marginTop: 40 }}>Avaliações do produto</h2>

      {!avaliacoes || avaliacoes.total === 0 ? (
        <div className="pagina-vazio">
          <p>Ainda não há avaliações deste anúncio.</p>
        </div>
      ) : (
        <div className="avaliacoes">
          {avaliacoes.avaliacoes.map((avaliacao) => (
            <div className="avaliacao" key={avaliacao.id}>
              <strong>{"★".repeat(avaliacao.nota)}</strong>

              <p>{avaliacao.comentario || "Sem comentário."}</p>

              <small>
                {new Date(avaliacao.criadaEm).toLocaleDateString("pt-BR")}
              </small>
            </div>
          ))}
        </div>
      )}

      {relacionados.length > 0 && (
        <>
          <h2 style={{ marginTop: 40 }}>Produtos relacionados</h2>

          <div className="grade-produtos">
            {relacionados.map((item) => (
              <article
                className="cartao-produto"
                key={item.id}
                onClick={() => navigate(`/produto/${item.id}`)}
              >
                <div className="cartao-produto-imagem">
                  {item.imagemUrl ? (
                    <img src={urlImagem(item.imagemUrl)} alt={item.nome} />
                  ) : (
                    <span>◇</span>
                  )}
                </div>

                <div className="cartao-produto-info">
                  <strong>{item.nome}</strong>
                  <span>{moeda(item.preco)}</span>
                </div>
              </article>
            ))}
          </div>
        </>
      )}
    </main>
  );
}

export default ProdutoPagina;
