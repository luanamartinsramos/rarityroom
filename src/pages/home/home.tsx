import { useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router-dom";

import type { Produto } from "../../services/produtosService";
import { obterProdutos } from "../../services/produtosService";

import "./home.css";

interface Usuario {
  id: string;
  nome: string;
  email: string;
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

interface CarrinhoItem extends Produto {
  quantidade: number;
}

interface Pedido {
  id: string;
  produto: string;
  quantidade: number;
  total: number;
  status: string;
  data: string;
}

const categorias = [
  {
    nome: "Games",
    descricao: "Jogos e acessórios",
    icon: "🎮",
    classe: "category-purple",
  },
  {
    nome: "Heróis",
    descricao: "Marvel, DC e muito mais",
    icon: "🦸",
    classe: "category-gold",
  },
  {
    nome: "Fantasia",
    descricao: "Magia e aventuras",
    icon: "🧙",
    classe: "category-blue",
  },
  {
    nome: "Anime",
    descricao: "Itens exclusivos",
    icon: "👾",
    classe: "category-red",
  },
  {
    nome: "Figures",
    descricao: "Bonecos e estátuas",
    icon: "🧸",
    classe: "category-pink",
  },
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

  const [products, setProducts] = useState<Produto[]>([]);
  const [loadingProducts, setLoadingProducts] = useState(true);

  const [search, setSearch] = useState("");
  const [selectedCategory, setSelectedCategory] = useState("Todos");

  const [favorites, setFavorites] = useState<number[]>([]);
  const [cart, setCart] = useState<CarrinhoItem[]>([]);

  const [showCart, setShowCart] = useState(false);
  const [showFavorites, setShowFavorites] = useState(false);
  const [showProfile, setShowProfile] = useState(false);
  const [showOrders, setShowOrders] = useState(false);

  const [selectedProduct, setSelectedProduct] = useState<Produto | null>(null);

  const [showProductForm, setShowProductForm] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Produto | null>(null);

  const [productForm, setProductForm] = useState<ProdutoForm>(emptyForm);

  const [savingProduct, setSavingProduct] = useState(false);

  const [orders, setOrders] = useState<Pedido[]>([
    {
      id: "RR-2026-00124",
      produto: "Pedido em andamento",
      quantidade: 1,
      total: 349.9,
      status: "Em transporte",
      data: "28 de setembro",
    },
  ]);

  useEffect(() => {
    async function loadUser() {
      try {
        const response = await fetch(
          "http://localhost:5000/api/autenticacao/usuário-atual",
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
    try {
      setLoadingProducts(true);

      const data = await obterProdutos();

      setProducts(data);
    } catch {
      toast.error("Não foi possível carregar os produtos.");
    } finally {
      setLoadingProducts(false);
    }
  }

  useEffect(() => {
    loadProducts();
  }, []);

  async function handleLogout() {
    try {
      const response = await fetch(
        "http://localhost:5000/api/autenticacao/sair",
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
    setShowOrders(false);

    document.getElementById("products")?.scrollIntoView({ behavior: "smooth" });
  }

  function scrollToOrders() {
    setShowOrders(true);

    setTimeout(() => {
      document.getElementById("orders")?.scrollIntoView({ behavior: "smooth" });
    }, 50);
  }

  function toggleFavorite(productId: number) {
    setFavorites((current) => {
      if (current.includes(productId)) {
        toast("Removido dos favoritos.");
        return current.filter((id) => id !== productId);
      }

      toast.success("Adicionado aos favoritos! ♡");
      return [...current, productId];
    });
  }

  function addToCart(product: Produto) {
    setCart((current) => {
      const existing = current.find((item) => item.id === product.id);

      if (existing) {
        return current.map((item) =>
          item.id === product.id
            ? {
                ...item,
                quantidade: item.quantidade + 1,
              }
            : item,
        );
      }

      return [
        ...current,
        {
          ...product,
          quantidade: 1,
        },
      ];
    });

    toast.success(`${product.nome} foi para o carrinho! 🛒`);
  }

  function removeFromCart(productId: number) {
    setCart((current) => current.filter((item) => item.id !== productId));
  }

  function changeCartQuantity(productId: number, quantidade: number) {
    if (quantidade <= 0) {
      removeFromCart(productId);
      return;
    }

    setCart((current) =>
      current.map((item) =>
        item.id === productId
          ? {
              ...item,
              quantidade,
            }
          : item,
      ),
    );
  }

  function openCreateProduct() {
    setEditingProduct(null);
    setProductForm(emptyForm);
    setShowProductForm(true);
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
        ? `http://localhost:5000/api/Produtos/${editingProduct.id}`
        : "http://localhost:5000/api/Produtos";

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

        toast.error(error?.mensagem || "Não foi possível salvar o produto.");

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

  async function handleDeleteProduct(productId: number) {
    const confirmed = window.confirm(
      "Tem certeza que deseja excluir este produto?",
    );

    if (!confirmed) {
      return;
    }

    try {
      const response = await fetch(
        `http://localhost:5000/api/Produtos/${productId}`,
        {
          method: "DELETE",
          credentials: "include",
        },
      );

      if (!response.ok) {
        const error = await response.json().catch(() => null);

        toast.error(error?.mensagem || "Não foi possível excluir o produto.");

        return;
      }

      setProducts((current) =>
        current.filter((product) => product.id !== productId),
      );

      setFavorites((current) => current.filter((id) => id !== productId));

      setCart((current) => current.filter((item) => item.id !== productId));

      toast.success("Produto excluído com sucesso.");
    } catch {
      toast.error("Não foi possível conectar ao servidor.");
    }
  }

  const filteredProducts = useMemo(() => {
    return products.filter((product) => {
      const searchValue = search.toLowerCase().trim();

      const matchesSearch =
        !searchValue ||
        product.nome.toLowerCase().includes(searchValue) ||
        product.categoria.toLowerCase().includes(searchValue) ||
        product.marca.toLowerCase().includes(searchValue) ||
        product.colecao.toLowerCase().includes(searchValue);

      const matchesCategory =
        selectedCategory === "Todos" ||
        product.categoria.toLowerCase() === selectedCategory.toLowerCase();

      return matchesSearch && matchesCategory;
    });
  }, [products, search, selectedCategory]);

  const favoriteProducts = products.filter((product) =>
    favorites.includes(product.id),
  );

  const cartTotal = cart.reduce(
    (total, item) => total + item.preco * item.quantidade,
    0,
  );

  const cartQuantity = cart.reduce((total, item) => total + item.quantidade, 0);

  const featuredProduct = products[0];

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

        <div className="home-search">
          <span>⌕</span>

          <input
            type="text"
            placeholder="Buscar itens, personagens, coleções..."
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
        </div>

        <div className="home-nav-actions">
          <button className="nav-button" onClick={() => setShowFavorites(true)}>
            ♡ <span>Favoritos</span>
            {favorites.length > 0 && <strong>{favorites.length}</strong>}
          </button>

          <button className="nav-button" onClick={() => setShowCart(true)}>
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
                    scrollToOrders();
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
                    setShowCart(true);
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

              <button className="secondary-button" onClick={scrollToOrders}>
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
                {
                  orders.filter((order) => order.status === "Em transporte")
                    .length
                }
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

            <button onClick={scrollToOrders}>Ver todos →</button>
          </div>

          {orders.length === 0 ? (
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
              <div className="order-card">
                <div className="order-product">
                  <div className="product-image">
                    {featuredProduct ? (
                      <img
                        src={featuredProduct.imagemUrl}
                        alt={featuredProduct.nome}
                      />
                    ) : (
                      <span>📦</span>
                    )}
                  </div>

                  <div>
                    <span className="order-number">PEDIDO #{orders[0].id}</span>

                    <h3>{featuredProduct?.nome || orders[0].produto}</h3>

                    <p>{orders[0].quantidade} unidade</p>
                  </div>
                </div>

                <div className="order-price">
                  <small>TOTAL</small>

                  <strong>
                    R$ {orders[0].total.toFixed(2).replace(".", ",")}
                  </strong>
                </div>

                <div className="order-status">
                  <span className="status-dot"></span>

                  <div>
                    <strong>{orders[0].status}</strong>

                    <small>Previsão: {orders[0].data}</small>
                  </div>
                </div>
              </div>

              <div className="order-progress">
                <div className="progress-step active">
                  <span>✓</span>
                  <small>Pedido realizado</small>
                </div>

                <div className="progress-line active"></div>

                <div className="progress-step active">
                  <span>✓</span>
                  <small>Preparando</small>
                </div>

                <div className="progress-line active"></div>

                <div className="progress-step active">
                  <span>🚚</span>
                  <small>Em transporte</small>
                </div>

                <div className="progress-line"></div>

                <div className="progress-step">
                  <span>○</span>
                  <small>Entregue</small>
                </div>
              </div>
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
              <span>hora de vender seus itens estagnados</span>
              <h2>Cadastre seus itens no catálogo</h2>
            </div>

            <div className="product-actions">
              <button onClick={openCreateProduct}>+ Cadastrar produto</button>
            </div>
          </div>

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
            {loadingProducts ? (
              <div className="products-loading">Carregando produtos...</div>
            ) : filteredProducts.length === 0 ? (
              <div className="products-empty">
                <span>◇</span>
                <h3>Nenhum produto encontrado</h3>
                <p>Tente outra busca ou cadastre um novo produto.</p>

                <button className="primary-button" onClick={openCreateProduct}>
                  Cadastrar produto
                </button>
              </div>
            ) : (
              filteredProducts.map((product) => (
                <article className="product-card" key={product.id}>
                  <div
                    className="product-cover"
                    onClick={() => setSelectedProduct(product)}
                  >
                    <img src={product.imagemUrl} alt={product.nome} />

                    <button
                      className={`favorite ${
                        favorites.includes(product.id) ? "favorite-active" : ""
                      }`}
                      onClick={(event) => {
                        event.stopPropagation();
                        toggleFavorite(product.id);
                      }}
                    >
                      {favorites.includes(product.id) ? "♥" : "♡"}
                    </button>
                  </div>

                  <div className="product-info">
                    <span>{product.categoria}</span>

                    <h3>{product.nome}</h3>

                    <strong>
                      R$ {product.preco.toFixed(2).replace(".", ",")}
                    </strong>

                    <div className="product-card-actions">
                      <button onClick={() => setSelectedProduct(product)}>
                        Ver detalhes
                      </button>

                      <button onClick={() => addToCart(product)}>
                        Adicionar
                      </button>
                    </div>

                    <div className="product-management">
                      <button onClick={() => openEditProduct(product)}>
                        Editar
                      </button>

                      <button onClick={() => handleDeleteProduct(product.id)}>
                        Excluir
                      </button>
                    </div>
                  </div>
                </article>
              ))
            )}
          </div>
        </section>
      </div>

      {showCart && (
        <div className="modal-overlay" onClick={() => setShowCart(false)}>
          <aside
            className="side-panel"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="panel-header">
              <div>
                <span>MINHA COMPRA</span>
                <h2>Carrinho</h2>
              </div>

              <button onClick={() => setShowCart(false)}>×</button>
            </div>

            {cart.length === 0 ? (
              <div className="panel-empty">
                <span>🛒</span>
                <h3>Seu carrinho está vazio</h3>
                <p>Adicione produtos para começar sua compra.</p>

                <button
                  className="primary-button"
                  onClick={() => {
                    setShowCart(false);
                    scrollToProducts();
                  }}
                >
                  Explorar produtos
                </button>
              </div>
            ) : (
              <>
                <div className="cart-items">
                  {cart.map((item) => (
                    <div className="cart-item" key={item.id}>
                      <img src={item.imagemUrl} alt={item.nome} />

                      <div>
                        <strong>{item.nome}</strong>

                        <span>
                          R$ {item.preco.toFixed(2).replace(".", ",")}
                        </span>

                        <div className="quantity-control">
                          <button
                            onClick={() =>
                              changeCartQuantity(item.id, item.quantidade - 1)
                            }
                          >
                            −
                          </button>

                          <span>{item.quantidade}</span>

                          <button
                            onClick={() =>
                              changeCartQuantity(item.id, item.quantidade + 1)
                            }
                          >
                            +
                          </button>
                        </div>
                      </div>

                      <button
                        className="remove-item"
                        onClick={() => removeFromCart(item.id)}
                      >
                        ×
                      </button>
                    </div>
                  ))}
                </div>

                <div className="cart-footer">
                  <div>
                    <span>Total</span>

                    <strong>R$ {cartTotal.toFixed(2).replace(".", ",")}</strong>
                  </div>

                  <button
                    className="primary-button"
                    onClick={() => {
                      if (cart.length === 0) {
                        return;
                      }

                      const newOrder: Pedido = {
                        id: `RR-2026-${String(orders.length + 125).padStart(
                          5,
                          "0",
                        )}`,
                        produto:
                          cart.length === 1
                            ? cart[0].nome
                            : `${cart.length} produtos`,
                        quantidade: cartQuantity,
                        total: cartTotal,
                        status: "Pedido realizado",
                        data: "Aguardando confirmação",
                      };

                      setOrders((current) => [newOrder, ...current]);

                      setCart([]);
                      setShowCart(false);

                      toast.success("Pedido criado com sucesso! ✨");

                      setTimeout(() => {
                        scrollToOrders();
                      }, 100);
                    }}
                  >
                    Finalizar pedido →
                  </button>
                </div>
              </>
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
                    <img src={product.imagemUrl} alt={product.nome} />

                    <div>
                      <span>{product.categoria}</span>
                      <strong>{product.nome}</strong>

                      <small>
                        R$ {product.preco.toFixed(2).replace(".", ",")}
                      </small>

                      <button onClick={() => addToCart(product)}>
                        Adicionar ao carrinho
                      </button>
                    </div>

                    <button onClick={() => toggleFavorite(product.id)}>
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
              <img src={selectedProduct.imagemUrl} alt={selectedProduct.nome} />
            </div>

            <div className="product-modal-content">
              <span>{selectedProduct.categoria}</span>

              <h2>{selectedProduct.nome}</h2>

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
                <strong>
                  R$ {selectedProduct.preco.toFixed(2).replace(".", ",")}
                </strong>

                <button
                  className="primary-button"
                  onClick={() => {
                    addToCart(selectedProduct);
                    setSelectedProduct(null);
                  }}
                >
                  Adicionar ao carrinho →
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
