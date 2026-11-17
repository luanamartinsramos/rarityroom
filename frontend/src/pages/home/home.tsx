import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate, useSearchParams } from "react-router-dom";

import {
  obterCategorias,
  obterMeusProdutos,
  obterPaginaProdutos,
  ORDENS,
  type Produto,
  type FiltroProdutos,
} from "../../services/produtosService";
import {
  obterPedidos,
  ROTULO_PEDIDO,
  type PedidoApi,
} from "../../services/pedidosService";
import { obterNotificacoes } from "../../services/notificacoesService";
import { obterCarrinho, ouvirCarrinho } from "../../services/carrinhoService";
import { requisitar } from "../../services/api";
import { adicionarComAviso } from "../../services/carrinhoAcoes";
import {
  adicionarFavorito,
  obterFavoritos,
  removerFavorito,
} from "../../services/favoritosService";
import { estrelas, data, moeda, urlImagem } from "../../utils/formato";
import { etapaAtual, NOMES_ETAPAS } from "../pedidos/rotulos";

import "./home.css";
import "../shared/compra.css";
import { API_ORIGIN } from "../../config";

const TAMANHO_PAGINA = 12;

interface Usuario {
  id: string;
  nome: string;
  email: string;
  tipoConta?: string;
  administrador?: boolean;
}

interface ProdutoForm {
  nome: string;
  descricao: string;
  preco: string;
  estoque: string;
  categoria: string;
  marca: string;
  colecao: string;
  condicao: string;
  imagemUrl: string;
}

// Paleta visual aplicada às categorias vindas do catálogo do backend.
const paletaCategorias = [
  { descricao: "Jogos e acessórios", icon: "🎮", classe: "category-purple" },
  { descricao: "Heróis e quadrinhos", icon: "🦸", classe: "category-gold" },
  { descricao: "Itens colecionáveis", icon: "✨", classe: "category-blue" },
  { descricao: "Animes e mangás", icon: "👾", classe: "category-red" },
  { descricao: "Bonecos e estátuas", icon: "🧸", classe: "category-pink" },
];

const emptyForm: ProdutoForm = {
  nome: "",
  descricao: "",
  preco: "",
  estoque: "",
  categoria: "",
  marca: "",
  colecao: "",
  condicao: "",
  imagemUrl: "",
};

function Home() {
  const navigate = useNavigate();

  const [user, setUser] = useState<Usuario | null>(null);
  const [loadingUser, setLoadingUser] = useState(true);

  const [meusProdutos, setMeusProdutos] = useState<Produto[]>([]);
  const [loadingProducts, setLoadingProducts] = useState(true);

  // Critérios da pesquisa ficam na URL: voltar da página de um produto
  // (ou recarregar) mantém termo, categoria, ordenação, filtros e página.
  const [parametros, setParametros] = useSearchParams();

  const termoUrl = parametros.get("q") ?? "";
  const selectedCategory = parametros.get("categoria") ?? "Todos";
  const ordem = parametros.get("ordem") ?? "Relevancia";
  const pagina = Math.max(1, Number(parametros.get("pagina") ?? "1") || 1);
  const precoMinUrl = parametros.get("min") ?? "";
  const precoMaxUrl = parametros.get("max") ?? "";
  const somenteDestaque = parametros.get("destaque") === "1";

  // Campos digitados: viram parâmetros da URL após uma pausa curta.
  const [search, setSearch] = useState(termoUrl);
  const [precoMin, setPrecoMin] = useState(precoMinUrl);
  const [precoMax, setPrecoMax] = useState(precoMaxUrl);

  const [categorias, setCategorias] = useState<
    { nome: string; descricao: string; icon: string; classe: string }[]
  >([]);

  const [searchResults, setSearchResults] = useState<Produto[]>([]);
  const [totalResultados, setTotalResultados] = useState(0);
  const [searching, setSearching] = useState(false);
  const [recarga, setRecarga] = useState(0);

  const [favoriteProducts, setFavoriteProducts] = useState<Produto[]>([]);
  const favorites = favoriteProducts.map((produto) => produto.id);
  const [cartQuantity, setCartQuantity] = useState(0);

  const [showFavorites, setShowFavorites] = useState(false);
  const [showProfile, setShowProfile] = useState(false);

  const [selectedProduct, setSelectedProduct] = useState<Produto | null>(null);

  const [showProductForm, setShowProductForm] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Produto | null>(null);

  const [productForm, setProductForm] = useState<ProdutoForm>(emptyForm);

  const [savingProduct, setSavingProduct] = useState(false);

  const [orders, setOrders] = useState<PedidoApi[]>([]);

  const [showSeller, setShowSeller] = useState(false);

  const [naoLidas, setNaoLidas] = useState(0);

  function atualizarParametros(
    mudancas: Record<string, string | null>,
    manterPagina = false,
  ) {
    setParametros(
      (atuais) => {
        const novos = new URLSearchParams(atuais);

        for (const [chave, valor] of Object.entries(mudancas)) {
          if (valor === null || valor === "") {
            novos.delete(chave);
          } else {
            novos.set(chave, valor);
          }
        }

        if (!manterPagina) {
          novos.delete("pagina");
        }

        return novos;
      },
      { replace: true },
    );
  }

  function setSelectedCategory(categoria: string) {
    atualizarParametros({ categoria: categoria === "Todos" ? null : categoria });
  }

  function setOrdem(valor: string) {
    atualizarParametros({ ordem: valor === "Relevancia" ? null : valor });
  }

  function irParaPagina(numero: number) {
    atualizarParametros({ pagina: numero > 1 ? String(numero) : null }, true);
    scrollToProducts();
  }

  function limparPesquisa() {
    setSearch("");
    setPrecoMin("");
    setPrecoMax("");
    setParametros(new URLSearchParams(), { replace: true });
  }

  function limparSoOTermo() {
    setSearch("");
    atualizarParametros({ q: null });
  }

  const meuId = Number(user?.id ?? 0);

  function ehMeu(produto: Produto) {
    return produto.usuarioId === meuId;
  }

  useEffect(() => {
    async function loadUser() {
      try {
        const response = await fetch(
          `${API_ORIGIN}/api/autenticacao/usuário-atual`,
          {
            method: "GET",
            credentials: "include",
          },
        );

        if (!response.ok) {
          navigate("/");
          return;
        }

        const data: Usuario = await response.json();
        setUser(data);
      } catch {
        toast.error("Não foi possível verificar sua sessão.");
        navigate("/");
      } finally {
        setLoadingUser(false);
      }
    }

    loadUser();
  }, [navigate]);

  async function loadProducts() {
    setRecarga((atual) => atual + 1);

    obterMeusProdutos()
      .then(setMeusProdutos)
      .catch(() => setMeusProdutos([]));
  }

  // Favoritos e contador do carrinho vêm do servidor.
  useEffect(() => {
    if (!user) {
      return;
    }

    obterFavoritos().then(setFavoriteProducts).catch(() => setFavoriteProducts([]));
    obterMeusProdutos().then(setMeusProdutos).catch(() => setMeusProdutos([]));
    obterCarrinho()
      .then((carrinho) => setCartQuantity(carrinho.quantidadeItens))
      .catch(() => setCartQuantity(0));

    return ouvirCarrinho((carrinho) => setCartQuantity(carrinho.quantidadeItens));
  }, [user]);

  async function handleLogout() {
    try {
      const response = await fetch(
        `${API_ORIGIN}/api/autenticacao/sair`,
        {
          method: "POST",
          credentials: "include",
        },
      );

      if (!response.ok) {
        toast.error("Não foi possível sair da sua conta.");
        return;
      }

      toast.success("Você saiu da sua conta. Até logo! ✨");
      navigate("/");
    } catch {
      toast.error("Não foi possível conectar ao servidor.");
    }
  }

  function scrollToProducts() {

    document.getElementById("products")?.scrollIntoView({ behavior: "smooth" });
  }

  async function toggleFavorite(product: Produto) {
    const favoritado = favorites.includes(product.id);

    try {
      if (favoritado) {
        await removerFavorito(product.id);
        setFavoriteProducts((atual) => atual.filter((item) => item.id !== product.id));
        toast("Removido dos favoritos.");
      } else {
        await adicionarFavorito(product.id);
        setFavoriteProducts((atual) => [...atual, product]);
        toast.success("Adicionado aos favoritos! ♡");
      }
    } catch (falha) {
      toast.error(falha instanceof Error ? falha.message : "Não foi possível atualizar o favorito.");
    }
  }

  /** Mesma ação usada na página do produto (ver carrinhoAcoes). */
  function addToCart(product: Produto) {
    void adicionarComAviso(product, 1);
  }

  function openEditProduct(product: Produto) {
    setEditingProduct(product);

    setProductForm({
      nome: product.nome,
      descricao: product.descricao,
      preco: String(product.preco),
      estoque: String(product.estoque),
      categoria: product.categoria,
      marca: product.marca,
      colecao: product.colecao,
      condicao: product.condicao,
      imagemUrl: product.imagemUrl,
    });

    setShowProductForm(true);
  }

  async function handleSaveProduct(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!productForm.nome.trim()) {
      toast.error("Digite o nome do produto.");
      return;
    }

    if (!productForm.preco || Number(productForm.preco) <= 0) {
      toast.error("Digite um preço válido.");
      return;
    }

    if (!productForm.estoque || Number(productForm.estoque) < 0) {
      toast.error("Digite um estoque válido.");
      return;
    }

    if (!productForm.categoria.trim()) {
      toast.error("Informe a categoria.");
      return;
    }

    setSavingProduct(true);

    try {
      const payload = {
        nome: productForm.nome.trim(),
        descricao: productForm.descricao.trim(),
        preco: Number(productForm.preco),
        estoque: Number(productForm.estoque),
        categoria: productForm.categoria.trim(),
        marca: productForm.marca.trim(),
        colecao: productForm.colecao.trim(),
        condicao: productForm.condicao.trim(),
        imagemUrl: productForm.imagemUrl.trim(),
      };

      const url = editingProduct
        ? `${API_ORIGIN}/api/Produtos/${editingProduct.id}`
        : `${API_ORIGIN}/api/Produtos`;

      const response = await fetch(url, {
        method: editingProduct ? "PUT" : "POST",
        headers: {
          "Content-Type": "application/json",
        },
        credentials: "include",
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        const error = await response.json().catch(() => null);

        toast.error(error?.detail || error?.mensagem || "Não foi possível salvar o produto.");

        return;
      }

      toast.success(
        editingProduct
          ? "Produto atualizado com sucesso! ✨"
          : "Produto cadastrado com sucesso! ✨",
      );

      setShowProductForm(false);
      setEditingProduct(null);
      setProductForm(emptyForm);

      await loadProducts();
    } catch {
      toast.error("Não foi possível conectar ao servidor.");
    } finally {
      setSavingProduct(false);
    }
  }

  /** Repor estoque (RN-REAB): o servidor valida dono, estado e quantidade. */
  async function reabastecer(produto: Produto) {
    const resposta = window.prompt(
      `Quantas unidades de "${produto.nome}" você quer repor? (1 a 1000)`,
      "1",
    );

    if (resposta === null) {
      return;
    }

    try {
      await requisitar(`/produtos/${produto.id}/reabastecer`, {
        metodo: "POST",
        corpo: { quantidade: Number(resposta) },
      });

      toast.success("Estoque reposto! ✨");
      await loadProducts();
    } catch (falha) {
      toast.error(falha instanceof Error ? falha.message : "Não foi possível repor o estoque.");
    }
  }

  async function handleDeleteProduct(productId: number) {
    const confirmed = window.confirm(
      "Tem certeza que deseja excluir este produto?",
    );

    if (!confirmed) {
      return;
    }

    try {
      const response = await fetch(
        `${API_ORIGIN}/api/Produtos/${productId}`,
        {
          method: "DELETE",
          credentials: "include",
        },
      );

      if (!response.ok) {
        const error = await response.json().catch(() => null);

        toast.error(error?.detail || error?.mensagem || "Não foi possível excluir o produto.");

        return;
      }

      setFavoriteProducts((current) => current.filter((item) => item.id !== productId));

      await loadProducts();

      toast.success("Produto excluído com sucesso.");
    } catch {
      toast.error("Não foi possível conectar ao servidor.");
    }
  }

  // Texto digitado → URL, com espera curta para não pesquisar a cada tecla.
  useEffect(() => {
    const temporizador = setTimeout(() => {
      const mudancas: Record<string, string | null> = {};

      if (search.trim() !== termoUrl) mudancas.q = search.trim() || null;
      if (precoMin.trim() !== precoMinUrl) mudancas.min = precoMin.trim() || null;
      if (precoMax.trim() !== precoMaxUrl) mudancas.max = precoMax.trim() || null;

      if (Object.keys(mudancas).length > 0) {
        atualizarParametros(mudancas);
      }
    }, 350);

    return () => clearTimeout(temporizador);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search, precoMin, precoMax]);

  // Pesquisa no servidor sempre que um critério da URL muda.
  useEffect(() => {
    let cancelado = false;

    setSearching(true);

    const filtro: FiltroProdutos = {
      termo: termoUrl,
      ordem,
      pagina,
      tamanhoPagina: TAMANHO_PAGINA,
    };

    if (selectedCategory !== "Todos") {
      filtro.categoria = selectedCategory;
    }

    const minimo = Number(precoMinUrl.replace(",", "."));
    const maximo = Number(precoMaxUrl.replace(",", "."));

    if (precoMinUrl && Number.isFinite(minimo)) filtro.precoMin = minimo;
    if (precoMaxUrl && Number.isFinite(maximo)) filtro.precoMax = maximo;
    if (somenteDestaque) filtro.destaque = true;

    obterPaginaProdutos(filtro)
      .then((resultado) => {
        if (cancelado) return;
        setSearchResults(resultado.itens);
        setTotalResultados(resultado.total);
      })
      .catch(() => !cancelado && toast.error("Não foi possível pesquisar."))
      .finally(() => {
        if (!cancelado) {
          setSearching(false);
          setLoadingProducts(false);
        }
      });

    return () => {
      cancelado = true;
    };
  }, [termoUrl, selectedCategory, ordem, pagina, precoMinUrl, precoMaxUrl, somenteDestaque, recarga]);

  // Categorias vêm do catálogo do backend, não de uma lista fixa.
  useEffect(() => {
    obterCategorias()
      .then((lista) =>
        setCategorias(
          lista.map((categoria, indice) => ({
            nome: categoria.nome,
            ...paletaCategorias[indice % paletaCategorias.length],
          })),
        ),
      )
      .catch(() => setCategorias([]));
  }, []);

  // Contador de notificações não lidas no cabeçalho.
  useEffect(() => {
    if (!user) {
      setNaoLidas(0);
      return;
    }

    obterNotificacoes()
      .then((central) => setNaoLidas(central.naoLidas))
      .catch(() => setNaoLidas(0));
  }, [user]);

  // Pedidos vêm do servidor: conta nova não tem nenhum.
  useEffect(() => {
    if (!user) {
      setOrders([]);
      return;
    }

    obterPedidos()
      .then((lista) =>
        setOrders(lista.filter((pedido) => String(pedido.compradorId) === String(user.id))),
      )
      .catch(() => setOrders([]));
  }, [user]);

  const totalPaginas = Math.max(1, Math.ceil(totalResultados / TAMANHO_PAGINA));
  const ultimoPedido = orders[0];
  const etapaUltimo = ultimoPedido ? etapaAtual(ultimoPedido.estado) : -1;
  const filtrando =
    termoUrl !== "" || selectedCategory !== "Todos" || precoMinUrl !== "" || precoMaxUrl !== "" || somenteDestaque;

  if (loadingUser) {
    return (
      <main className="home-page">
        <div className="home-loading">
          <span>CARREGANDO RARITYROOM...</span>
        </div>
      </main>
    );
  }

  if (!user) {
    return null;
  }

  const firstName = user.nome.split(" ")[0];
  const avatarLetter = user.nome.charAt(0).toUpperCase();

  return (
    <main className="home-page">
      <nav className="home-navbar">
        <button
          className="home-logo"
          onClick={() => window.scrollTo({ top: 0, behavior: "smooth" })}
        >
          <span>✦</span>
          RARITYROOM
        </button>

        <div className="home-search-area">
          <div className="home-search">
          <span>⌕</span>

          <input
            type="text"
            aria-label="Pesquisar produtos"
            placeholder="Buscar itens, personagens, coleções..."
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                atualizarParametros({ q: search.trim() || null });
                scrollToProducts();
              }
            }}
          />

          {search && (
            <button
              type="button"
              className="link-texto"
              aria-label="Limpar pesquisa"
              onClick={limparSoOTermo}
            >
              ✕
            </button>
          )}
          </div>

          <div className="home-search-filtros">
            <select
              className="toolbar-select"
              aria-label="Ordenar por"
              value={ordem}
              onChange={(event) => setOrdem(event.target.value)}
            >
              {ORDENS.map((opcao) => (
                <option key={opcao.valor} value={opcao.valor}>
                  {opcao.rotulo}
                </option>
              ))}
            </select>

            <input
              className="toolbar-preco"
              placeholder="Preço mín."
              value={precoMin}
              onChange={(event) => setPrecoMin(event.target.value)}
            />

            <input
              className="toolbar-preco"
              placeholder="Preço máx."
              value={precoMax}
              onChange={(event) => setPrecoMax(event.target.value)}
            />

            <button
              className={somenteDestaque ? "active" : ""}
              onClick={() => atualizarParametros({ destaque: somenteDestaque ? null : "1" })}
            >
              ★ Destaques
            </button>
          </div>
        </div>

        <div className="home-nav-actions">
          {user?.administrador && (
            <button
              className="nav-button"
              onClick={() => navigate("/admin")}
            >
              ⚙️ <span>Admin</span>
            </button>
          )}

          <button className="nav-button" onClick={() => navigate("/conta")}>
            👤 <span>Minha conta</span>
          </button>

          <button
            className="nav-button"
            onClick={() => navigate("/notificacoes")}
          >
            🔔 <span>Notificações</span>
            {naoLidas > 0 && <strong>{naoLidas}</strong>}
          </button>

          <button className="nav-button" onClick={() => setShowSeller(true)}>
            🏪 <span>Vender</span>
          </button>

          <button className="nav-button" onClick={() => setShowFavorites(true)}>
            ♡ <span>Favoritos</span>
            {favorites.length > 0 && <strong>{favorites.length}</strong>}
          </button>

          <button className="nav-button" onClick={() => navigate("/carrinho")}>
            🛒 <span>Carrinho</span>
            {cartQuantity > 0 && <strong>{cartQuantity}</strong>}
          </button>

          <div className="profile-wrapper">
            <button
              className="profile-button"
              onClick={() => setShowProfile((current) => !current)}
            >
              <span className="profile-avatar">{avatarLetter}</span>

              <span>{firstName}</span>
            </button>

            {showProfile && (
              <div className="profile-menu">
                <div className="profile-menu-user">
                  <strong>{user.nome}</strong>
                  <span>{user.email}</span>
                </div>

                <button
                  onClick={() => {
                    setShowProfile(false);
                    navigate("/pedidos");
                  }}
                >
                  📦 Meus pedidos
                </button>

                <button
                  onClick={() => {
                    setShowProfile(false);
                    setShowFavorites(true);
                  }}
                >
                  ♡ Meus favoritos
                </button>

                <button
                  onClick={() => {
                    setShowProfile(false);
                    navigate("/carrinho");
                  }}
                >
                  🛒 Meu carrinho
                </button>

                <button className="profile-logout" onClick={handleLogout}>
                  Sair da conta
                </button>
              </div>
            )}
          </div>
        </div>
      </nav>

      <div className="home-content">
        <section className="welcome-section">
          <div>
            <span className="home-eyebrow">✦ ÁREA DO COLECIONADOR</span>

            <h1>
              Olá, <span>{firstName}</span>.
            </h1>

            <p>
              Seu universo geek está esperando por você. Encontre itens raros,
              monte sua coleção e acompanhe seus pedidos.
            </p>

            <div className="hero-actions">
              <button className="primary-button" onClick={scrollToProducts}>
                Meu catálogo →
              </button>

              <button className="secondary-button" onClick={() => navigate("/pedidos")}>
                Ver meus pedidos
              </button>
            </div>
          </div>

          <div className="hero-symbol">
            <div className="symbol-ring ring-one"></div>
            <div className="symbol-ring ring-two"></div>
            <span>✦</span>
          </div>
        </section>

        <section className="stats-section">
          <div className="stat-card">
            <span className="stat-icon">📦</span>

            <div>
              <small>TOTAL DE PEDIDOS</small>
              <strong>{orders.length}</strong>
            </div>
          </div>

          <div className="stat-card">
            <span className="stat-icon">🚚</span>

            <div>
              <small>A CAMINHO</small>
              <strong>
                {orders.filter((order) => order.estado === "Enviado").length}
              </strong>
            </div>
          </div>

          <div className="stat-card">
            <span className="stat-icon">♡</span>

            <div>
              <small>FAVORITOS</small>
              <strong>{favorites.length}</strong>
            </div>
          </div>

          <div className="stat-card">
            <span className="stat-icon">✦</span>

            <div>
              <small>COLECIONADOR DESDE</small>
              <strong>2026</strong>
            </div>
          </div>
        </section>

        <section className="featured-order" id="orders">
          <div className="section-heading">
            <div>
              <span>ACOMPANHE SUA COMPRA</span>
              <h2>Meus pedidos</h2>
            </div>

            <button onClick={() => navigate("/pedidos")}>Ver todos →</button>
          </div>

          {!ultimoPedido ? (
            <div className="order-empty">
              <span>📦</span>
              <h3>Você ainda não possui pedidos</h3>
              <p>
                Explore nossa coleção e encontre algo especial para sua coleção.
              </p>

              <button className="primary-button" onClick={scrollToProducts}>
                Explorar produtos
              </button>
            </div>
          ) : (
            <>
              <div
                className="order-card"
                style={{ cursor: "pointer" }}
                onClick={() => navigate(`/pedidos/${ultimoPedido.id}`)}
              >
                <div className="order-product">
                  <div className="product-image">
                    {ultimoPedido.itens[0]?.imagemUrl ? (
                      <img
                        src={urlImagem(ultimoPedido.itens[0].imagemUrl)}
                        alt={ultimoPedido.nomeProduto}
                      />
                    ) : (
                      <span>📦</span>
                    )}
                  </div>

                  <div>
                    <span className="order-number">PEDIDO {ultimoPedido.codigo}</span>

                    <h3>{ultimoPedido.nomeProduto}</h3>

                    <p>
                      {ultimoPedido.quantidadeTotal}{" "}
                      {ultimoPedido.quantidadeTotal === 1 ? "unidade" : "unidades"}
                    </p>
                  </div>
                </div>

                <div className="order-price">
                  <small>TOTAL</small>

                  <strong>{moeda(ultimoPedido.total)}</strong>
                </div>

                <div className="order-status">
                  <span className="status-dot"></span>

                  <div>
                    <strong>{ROTULO_PEDIDO[ultimoPedido.estado] ?? ultimoPedido.estado}</strong>

                    <small>Feito em {data(ultimoPedido.criadoEm)}</small>
                  </div>
                </div>
              </div>

              {etapaUltimo >= 0 && (
                <div className="order-progress">
                  {NOMES_ETAPAS.slice(0, 4).map((nome, indice) => (
                    <div key={nome} style={{ display: "contents" }}>
                      {indice > 0 && (
                        <div className={`progress-line ${indice <= etapaUltimo ? "active" : ""}`}></div>
                      )}
                      <div className={`progress-step ${indice <= etapaUltimo ? "active" : ""}`}>
                        <span>{indice <= etapaUltimo ? "✓" : "○"}</span>
                        <small>{nome}</small>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </>
          )}
        </section>

        <section className="categories-section">
          <div className="section-heading">
            <div>
              <span>EXPLORE</span>
              <h2>Encontre seu próximo item</h2>
            </div>

            <button
              onClick={() => {
                setSelectedCategory("Todos");
                scrollToProducts();
              }}
            >
              Ver todos →
            </button>
          </div>

          <div className="categories-grid">
            {categorias.map((category) => (
              <button
                className={`category-card ${category.classe}`}
                key={category.nome}
                onClick={() => {
                  setSelectedCategory(category.nome);
                  scrollToProducts();
                }}
              >
                <span>{category.icon}</span>
                <h3>{category.nome}</h3>
                <p>{category.descricao}</p>
              </button>
            ))}
          </div>
        </section>

        <section className="recommended-section" id="products">
          <div className="section-heading">
            <div>
              <span>{termoUrl ? "RESULTADOS DA PESQUISA" : "CATÁLOGO"}</span>
              <h2>
                {termoUrl ? `Resultados para "${termoUrl}"` : "Itens à venda"}
              </h2>
            </div>

            {filtrando && (
              <button onClick={limparPesquisa}>Limpar pesquisa e filtros ✕</button>
            )}
          </div>

          {!searching && (
            <p style={{ color: "#9b8fb0", margin: "0 0 12px" }}>
              {totalResultados} {totalResultados === 1 ? "produto encontrado" : "produtos encontrados"}
              {" · ordenado por "}
              {ORDENS.find((opcao) => opcao.valor === ordem)?.rotulo ?? "Relevância"}
              {totalPaginas > 1 && ` · página ${pagina} de ${totalPaginas}`}
            </p>
          )}

          <div className="product-toolbar">

            <button
              className={selectedCategory === "Todos" ? "active" : ""}
              onClick={() => setSelectedCategory("Todos")}
            >
              Todos
            </button>

            {categorias.map((category) => (
              <button
                key={category.nome}
                className={selectedCategory === category.nome ? "active" : ""}
                onClick={() => setSelectedCategory(category.nome)}
              >
                {category.nome}
              </button>
            ))}
          </div>

          <div className="products-grid">
            {loadingProducts || searching ? (
              <div className="products-loading">Carregando produtos...</div>
            ) : searchResults.length === 0 ? (
              <div className="products-empty">
                <span>◇</span>
                <h3>Nenhum produto encontrado</h3>
                <p>
                  {termoUrl
                    ? `Nenhum anúncio corresponde a "${termoUrl}". Confira a ortografia ou use menos palavras.`
                    : "Nenhum anúncio corresponde aos filtros escolhidos."}
                </p>

                {filtrando ? (
                  <button className="primary-button" onClick={limparPesquisa}>
                    Limpar pesquisa e voltar ao catálogo
                  </button>
                ) : (
                  <button
                    className="primary-button"
                    onClick={() => navigate("/anuncios/novo")}
                  >
                    Cadastrar produto
                  </button>
                )}
              </div>
            ) : (
              searchResults.map((product) => (
                <article className="product-card" key={product.id}>
                  <div
                    className="product-cover"
                    onClick={() => setSelectedProduct(product)}
                  >
                    {product.imagemUrl ? (
                      <img src={urlImagem(product.imagemUrl)} alt={product.nome} />
                    ) : (
                      <span style={{ fontSize: 48, color: "#3a2c50", display: "grid", placeItems: "center", height: "100%" }}>◇</span>
                    )}

                    <button
                      className={`favorite ${
                        favorites.includes(product.id) ? "favorite-active" : ""
                      }`}
                      onClick={(event) => {
                        event.stopPropagation();
                        void toggleFavorite(product);
                      }}
                    >
                      {favorites.includes(product.id) ? "♥" : "♡"}
                    </button>
                  </div>

                  <div className="product-info">
                    <span>{product.categoria}</span>

                    <h3>{product.nome}</h3>

                    <strong>{moeda(product.preco)}</strong>

                    <small style={{ color: "#9b8fb0", display: "block", marginTop: 4 }}>
                      {product.vendedorNome ? `por ${product.vendedorNome} · ` : ""}
                      {(product.totalAvaliacoes ?? 0) > 0
                        ? `${estrelas(product.mediaAvaliacoes ?? 0)} (${product.totalAvaliacoes})`
                        : "Sem avaliações"}
                      {(product.vendas ?? 0) > 0 && ` · ${product.vendas} vendido(s)`}
                    </small>

                    <small style={{ color: product.estoque <= 2 ? "#f1d58a" : "#9b8fb0", display: "block" }}>
                      {product.estoque === 1 ? "Última unidade!" : `${product.estoque} em estoque`}
                    </small>

                    <div className="product-card-actions">
                      <button onClick={() => navigate(`/produto/${product.id}`)}>
                        Ver detalhes
                      </button>

                      <button
                        onClick={() => addToCart(product)}
                        disabled={ehMeu(product)}
                        title={ehMeu(product) ? "Este anúncio é seu" : undefined}
                      >
                        {ehMeu(product) ? "Seu anúncio" : "Adicionar"}
                      </button>
                    </div>

                    {ehMeu(product) && (
                      <div className="product-management">
                        <button onClick={() => openEditProduct(product)}>
                          Editar
                        </button>

                        <button onClick={() => handleDeleteProduct(product.id)}>
                          Excluir
                        </button>
                      </div>
                    )}
                  </div>
                </article>
              ))
            )}
          </div>

          {!searching && totalPaginas > 1 && (
            <nav className="paginacao" aria-label="Páginas de resultados">
              <button disabled={pagina <= 1} onClick={() => irParaPagina(pagina - 1)}>
                ‹
              </button>

              {Array.from({ length: totalPaginas }, (_, indice) => indice + 1)
                .filter((numero) => Math.abs(numero - pagina) <= 2 || numero === 1 || numero === totalPaginas)
                .map((numero) => (
                  <button
                    key={numero}
                    className={numero === pagina ? "ativa" : ""}
                    onClick={() => irParaPagina(numero)}
                  >
                    {numero}
                  </button>
                ))}

              <button disabled={pagina >= totalPaginas} onClick={() => irParaPagina(pagina + 1)}>
                ›
              </button>
            </nav>
          )}
        </section>
      </div>

      {showSeller && (
        <div className="modal-overlay" onClick={() => setShowSeller(false)}>
          <aside
            className="side-panel"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="panel-header">
              <div>
                <span>ÁREA DO VENDEDOR</span>
                <h2>Meus anúncios</h2>
              </div>

              <button onClick={() => setShowSeller(false)}>×</button>
            </div>

            <div className="seller-actions">
              <button
                className="primary-button"
                onClick={() => navigate("/anuncios/novo")}
              >
                + Cadastrar produto
              </button>
            </div>

            {meusProdutos.length === 0 ? (
              <div className="panel-empty">
                <span>◇</span>
                <h3>Você ainda não publicou nada</h3>
                <p>Cadastre seu primeiro item para começar a vender.</p>
              </div>
            ) : (
              <div className="seller-list">
                {meusProdutos.map((produto) => (
                  <div className="seller-item" key={produto.id}>
                    <div>
                      <strong>{produto.nome}</strong>

                      <span>
                        R$ {produto.preco.toFixed(2).replace(".", ",")} ·{" "}
                        {produto.estado}
                      </span>

                      <span>Estoque: {produto.estoque}</span>
                    </div>

                    <div className="seller-item-actions">
                      {produto.estado !== "Vendido" && (
                        <button onClick={() => openEditProduct(produto)}>
                          Editar
                        </button>
                      )}

                      <button onClick={() => void reabastecer(produto)}>
                        Repor estoque
                      </button>

                      <button onClick={() => handleDeleteProduct(produto.id)}>
                        Excluir
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </aside>
        </div>
      )}

      {showFavorites && (
        <div className="modal-overlay" onClick={() => setShowFavorites(false)}>
          <aside
            className="side-panel"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="panel-header">
              <div>
                <span>MINHA COLEÇÃO</span>
                <h2>Favoritos</h2>
              </div>

              <button onClick={() => setShowFavorites(false)}>×</button>
            </div>

            {favoriteProducts.length === 0 ? (
              <div className="panel-empty">
                <span>♡</span>
                <h3>Nenhum favorito ainda</h3>
                <p>Clique no coração dos produtos que você deseja guardar.</p>

                <button
                  className="primary-button"
                  onClick={() => {
                    setShowFavorites(false);
                    scrollToProducts();
                  }}
                >
                  Explorar produtos
                </button>
              </div>
            ) : (
              <div className="favorite-list">
                {favoriteProducts.map((product) => (
                  <div className="favorite-item" key={product.id}>
                    {product.imagemUrl ? (
                      <img src={urlImagem(product.imagemUrl)} alt={product.nome} />
                    ) : (
                      <span style={{ fontSize: 48, color: "#3a2c50", display: "grid", placeItems: "center", height: "100%" }}>◇</span>
                    )}

                    <div>
                      <span>{product.categoria}</span>
                      <strong>{product.nome}</strong>

                      <small>
                        R$ {product.preco.toFixed(2).replace(".", ",")}
                      </small>

                      <button onClick={() => addToCart(product)} disabled={ehMeu(product) || !product.disponivel}>
                        {ehMeu(product) ? "Seu anúncio" : product.disponivel ? "Adicionar ao carrinho" : "Indisponível"}
                      </button>
                    </div>

                    <button onClick={() => void toggleFavorite(product)}>
                      ♥
                    </button>
                  </div>
                ))}
              </div>
            )}
          </aside>
        </div>
      )}

      {selectedProduct && (
        <div className="modal-overlay" onClick={() => setSelectedProduct(null)}>
          <div
            className="product-modal"
            onClick={(event) => event.stopPropagation()}
          >
            <button
              className="modal-close"
              onClick={() => setSelectedProduct(null)}
            >
              ×
            </button>

            <div className="product-modal-image">
              {selectedProduct.imagemUrl ? (
                <img src={urlImagem(selectedProduct.imagemUrl)} alt={selectedProduct.nome} />
              ) : (
                <span style={{ fontSize: 64, color: "#3a2c50", display: "grid", placeItems: "center", height: "100%" }}>◇</span>
              )}
            </div>

            <div className="product-modal-content">
              <span>{selectedProduct.categoria}</span>

              <h2>{selectedProduct.nome}</h2>

              <button
                className="primary-button"
                style={{ marginBottom: 12 }}
                onClick={() => {
                  const produtoId = selectedProduct.id;
                  setSelectedProduct(null);
                  navigate(`/produto/${produtoId}`);
                }}
              >
                Ver página do produto
              </button>

              <p>
                {selectedProduct.descricao ||
                  "Produto especial para colecionadores."}
              </p>

              <div className="product-modal-details">
                <div>
                  <small>MARCA</small>
                  <strong>{selectedProduct.marca || "Não informado"}</strong>
                </div>

                <div>
                  <small>COLEÇÃO</small>
                  <strong>{selectedProduct.colecao || "Não informado"}</strong>
                </div>

                <div>
                  <small>CONDIÇÃO</small>
                  <strong>{selectedProduct.condicao || "Não informado"}</strong>
                </div>

                <div>
                  <small>ESTOQUE</small>
                  <strong>{selectedProduct.estoque} unidades</strong>
                </div>
              </div>

              <div className="product-modal-footer">
                <strong>{moeda(selectedProduct.preco)}</strong>

                <button
                  className="primary-button"
                  disabled={ehMeu(selectedProduct)}
                  onClick={() => {
                    addToCart(selectedProduct);
                    setSelectedProduct(null);
                  }}
                >
                  {ehMeu(selectedProduct) ? "Este anúncio é seu" : "Adicionar ao carrinho →"}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {showProductForm && (
        <div
          className="modal-overlay"
          onClick={() => !savingProduct && setShowProductForm(false)}
        >
          <div
            className="product-form-modal"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="panel-header">
              <div>
                <span>{editingProduct ? "CATÁLOGO" : "NOVO ITEM"}</span>

                <h2>
                  {editingProduct ? "Editar produto" : "Cadastrar produto"}
                </h2>
              </div>

              <button
                onClick={() => !savingProduct && setShowProductForm(false)}
              >
                ×
              </button>
            </div>

            <form onSubmit={handleSaveProduct}>
              <div className="form-grid">
                <label>
                  Nome
                  <input
                    value={productForm.nome}
                    onChange={(event) =>
                      setProductForm({
                        ...productForm,
                        nome: event.target.value,
                      })
                    }
                    placeholder="Nome do produto"
                  />
                </label>

                <label>
                  Categoria
                  <input
                    value={productForm.categoria}
                    onChange={(event) =>
                      setProductForm({
                        ...productForm,
                        categoria: event.target.value,
                      })
                    }
                    placeholder="Anime, Games..."
                  />
                </label>

                <label>
                  Preço
                  <input
                    type="number"
                    step="0.01"
                    min="0"
                    value={productForm.preco}
                    onChange={(event) =>
                      setProductForm({
                        ...productForm,
                        preco: event.target.value,
                      })
                    }
                    placeholder="0,00"
                  />
                </label>

                <label>
                  Estoque
                  <input
                    type="number"
                    min="0"
                    value={productForm.estoque}
                    onChange={(event) =>
                      setProductForm({
                        ...productForm,
                        estoque: event.target.value,
                      })
                    }
                    placeholder="0"
                  />
                </label>

                <label>
                  Marca
                  <input
                    value={productForm.marca}
                    onChange={(event) =>
                      setProductForm({
                        ...productForm,
                        marca: event.target.value,
                      })
                    }
                    placeholder="Marca"
                  />
                </label>

                <label>
                  Coleção
                  <input
                    value={productForm.colecao}
                    onChange={(event) =>
                      setProductForm({
                        ...productForm,
                        colecao: event.target.value,
                      })
                    }
                    placeholder="Coleção"
                  />
                </label>

                <label>
                  Condição
                  <input
                    value={productForm.condicao}
                    onChange={(event) =>
                      setProductForm({
                        ...productForm,
                        condicao: event.target.value,
                      })
                    }
                    placeholder="Novo, usado..."
                  />
                </label>

                <label>
                  URL da imagem
                  <input
                    value={productForm.imagemUrl}
                    onChange={(event) =>
                      setProductForm({
                        ...productForm,
                        imagemUrl: event.target.value,
                      })
                    }
                    placeholder="https://..."
                  />
                </label>

                <label className="form-full">
                  Descrição
                  <textarea
                    value={productForm.descricao}
                    onChange={(event) =>
                      setProductForm({
                        ...productForm,
                        descricao: event.target.value,
                      })
                    }
                    placeholder="Descrição do produto"
                    rows={4}
                  />
                </label>
              </div>

              <div className="form-actions">
                <button
                  type="button"
                  onClick={() => setShowProductForm(false)}
                  disabled={savingProduct}
                >
                  Cancelar
                </button>

                <button
                  type="submit"
                  className="primary-button"
                  disabled={savingProduct}
                >
                  {savingProduct
                    ? "SALVANDO..."
                    : editingProduct
                      ? "SALVAR ALTERAÇÕES"
                      : "CADASTRAR PRODUTO"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </main>
  );
}

export default Home;
