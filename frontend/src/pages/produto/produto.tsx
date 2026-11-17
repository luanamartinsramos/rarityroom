import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate, useParams } from "react-router-dom";

import { adicionarAoCarrinho } from "../../services/carrinhoService";
import { obterProduto, obterVendedor, type VendedorApi } from "../../services/catalogoService";
import { obterProdutos, type Produto } from "../../services/produtosService";
import {
  obterResumoProduto,
  type ResumoAvaliacoes,
} from "../../services/avaliacoesService";

import "../shared/paginas.css";

const API_FAVORITOS = "http://localhost:5000/api/favoritos";

function ProdutoPagina() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [produto, setProduto] = useState<Produto | null>(null);
  const [vendedor, setVendedor] = useState<VendedorApi | null>(null);
  const [relacionados, setRelacionados] = useState<Produto[]>([]);
  const [avaliacoes, setAvaliacoes] = useState<ResumoAvaliacoes | null>(null);
  const [favoritado, setFavoritado] = useState(false);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");

  useEffect(() => {
    const produtoId = Number(id);

    if (!Number.isFinite(produtoId)) {
      setErro("Anúncio inválido.");
      setCarregando(false);
      return;
    }

    setCarregando(true);

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
  }, [id]);

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

  function adicionar() {
    if (!produto) {
      return;
    }

    try {
      adicionarAoCarrinho(produto);
      toast.success("Adicionado ao carrinho! 🛒");
    } catch (falha) {
      toast.error(
        falha instanceof Error ? falha.message : "Não foi possível adicionar.",
      );
    }
  }

  if (carregando) {
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
          <h3>{erro || "Anúncio não encontrado"}</h3>

          <button onClick={() => navigate("/home")}>Voltar ao catálogo</button>
        </div>
      </main>
    );
  }

  return (
    <main className="pagina">
      <div className="pagina-topo">
        <button onClick={() => navigate("/home")}>← Catálogo</button>

        <h1>{produto.nome}</h1>
      </div>

      <div className="produto-detalhe">
        <div className="produto-imagem">
          {produto.imagemUrl ? (
            <img src={produto.imagemUrl} alt={produto.nome} />
          ) : (
            <span>◇</span>
          )}
        </div>

        <div>
          <div className="produto-preco">
            R$ {produto.preco.toFixed(2).replace(".", ",")}
          </div>

          <div className="produto-tags">
            <span className="produto-tag">{produto.categoria}</span>
            <span className="produto-tag">{produto.condicao || "Sem condição"}</span>
            <span className="produto-tag">{produto.raridade}</span>
            {produto.franquia && (
              <span className="produto-tag">{produto.franquia}</span>
            )}
            {produto.edicaoLimitada && (
              <span className="produto-tag">Edição limitada</span>
            )}
            <span className="produto-tag">
              {produto.estoque} em estoque
            </span>

            <span className="produto-tag">
              {avaliacoes && avaliacoes.total > 0
                ? `${avaliacoes.media} ★ (${avaliacoes.total})`
                : "Sem avaliações"}
            </span>
          </div>

          <p className="produto-descricao">
            {produto.descricao || "O vendedor não escreveu uma descrição."}
          </p>

          <div className="produto-acoes">
            <button
              className="primario"
              onClick={adicionar}
              disabled={produto.estoque <= 0}
            >
              {produto.estoque > 0 ? "Adicionar ao carrinho" : "Sem estoque"}
            </button>

            <button onClick={alternarFavorito}>
              {favoritado ? "♥ Favoritado" : "♡ Favoritar"}
            </button>

            <button onClick={() => navigate("/home")}>Ir ao carrinho</button>
          </div>

          {vendedor && (
            <div
              className="vendedor-card"
              onClick={() => navigate(`/vendedor/${vendedor.id}`)}
            >
              <div className="vendedor-avatar">
                {vendedor.nome.charAt(0).toUpperCase()}
              </div>

              <div>
                <strong>{vendedor.nome}</strong>

                <div>
                  {vendedor.totalAvaliacoes === 0
                    ? "Sem avaliações ainda"
                    : `${vendedor.reputacaoMedia} ★ (${vendedor.totalAvaliacoes})`}
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
                    <img src={item.imagemUrl} alt={item.nome} />
                  ) : (
                    <span>◇</span>
                  )}
                </div>

                <div className="cartao-produto-info">
                  <strong>{item.nome}</strong>
                  <span>R$ {item.preco.toFixed(2).replace(".", ",")}</span>
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
