import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router-dom";

import { products } from "../../data/products";

import "./home.css";

interface Usuario {
  id: string;
  nome: string;
  email: string;
}

function Home() {
  const navigate = useNavigate();

  const [user, setUser] = useState<Usuario | null>(null);
  const [loadingUser, setLoadingUser] = useState(true);

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
        <div className="home-logo">
          <span>✦</span>
          RARITYROOM
        </div>

        <div className="home-search">
          <span>⌕</span>

          <input
            type="text"
            placeholder="Buscar itens, personagens, coleções..."
          />
        </div>

        <div className="home-nav-actions">
          <button className="nav-button">
            ♡ <span>Favoritos</span>
          </button>

          <button className="nav-button">
            🛒 <span>Carrinho</span>
          </button>

          <button className="profile-button">
            <span className="profile-avatar">{avatarLetter}</span>

            <span>{firstName}</span>
          </button>

          <button className="logout-button" onClick={handleLogout}>
            Sair
          </button>
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
              Seu universo geek está esperando por você. Explore novos itens e
              acompanhe seus pedidos.
            </p>

            <div className="hero-actions">
              <button className="primary-button">Explorar coleção →</button>

              <button className="secondary-button">Ver meus pedidos</button>
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
              <strong>4</strong>
            </div>
          </div>

          <div className="stat-card">
            <span className="stat-icon">🚚</span>

            <div>
              <small>A CAMINHO</small>
              <strong>1</strong>
            </div>
          </div>

          <div className="stat-card">
            <span className="stat-icon">♡</span>

            <div>
              <small>FAVORITOS</small>
              <strong>8</strong>
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

        <section className="featured-order">
          <div className="section-heading">
            <div>
              <span>ACOMPANHE SUA COMPRA</span>
              <h2>Pedido em andamento</h2>
            </div>

            <button>Ver todos →</button>
          </div>

          <div className="order-card">
            <div className="order-product">
              <div className="product-image">
                <img src={products[1]?.image} alt={products[1]?.name} />
              </div>

              <div>
                <span className="order-number">PEDIDO #RR-2026-00124</span>

                <h3>{products[1]?.name || "Katana Demon Slayer"}</h3>

                <p>Edição especial • 1 unidade</p>
              </div>
            </div>

            <div className="order-price">
              <small>TOTAL</small>
              <strong>R$ 349,90</strong>
            </div>

            <div className="order-status">
              <span className="status-dot"></span>

              <div>
                <strong>Em transporte</strong>
                <small>Previsão: 28 de setembro</small>
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
        </section>

        <section className="categories-section">
          <div className="section-heading">
            <div>
              <span>EXPLORE</span>
              <h2>Encontre seu próximo item</h2>
            </div>
          </div>

          <div className="categories-grid">
            <div className="category-card category-purple">
              <span>🎮</span>
              <h3>Games</h3>
              <p>Jogos e acessórios</p>
            </div>

            <div className="category-card category-gold">
              <span>🦸</span>
              <h3>Heróis</h3>
              <p>Marvel, DC e muito mais</p>
            </div>

            <div className="category-card category-blue">
              <span>🧙</span>
              <h3>Fantasia</h3>
              <p>Magia e aventuras</p>
            </div>

            <div className="category-card category-red">
              <span>👾</span>
              <h3>Anime</h3>
              <p>Itens exclusivos</p>
            </div>

            <div className="category-card category-pink">
              <span>🧸</span>
              <h3>Figures</h3>
              <p>Bonecos e estátuas</p>
            </div>
          </div>
        </section>

        <section className="recommended-section">
          <div className="section-heading">
            <div>
              <span>PARA VOCÊ</span>
              <h2>Itens que podem entrar na sua coleção</h2>
            </div>

            <button>Ver todos →</button>
          </div>

          <div className="products-grid">
            {products.map((product) => (
              <article className="product-card" key={product.id}>
                <div className="product-cover">
                  <img src={product.image} alt={product.name} />

                  <button className="favorite">♡</button>
                </div>

                <div className="product-info">
                  <span>{product.category}</span>

                  <h3>{product.name}</h3>

                  <strong>
                    R$ {product.price.toFixed(2).replace(".", ",")}
                  </strong>
                </div>
              </article>
            ))}
          </div>
        </section>
      </div>
    </main>
  );
}

export default Home;
