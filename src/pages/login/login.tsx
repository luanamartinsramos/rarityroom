import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import "./login.css";

interface Usuario {
  id: number;
  nome: string;
  email: string;
}

interface LoginResponse {
  mensagem: string;
  usuario: Usuario;
}

function Login() {
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleLogin(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setError("");
    setLoading(true);

    try {
      const response = await fetch(
        "http://localhost:5000/api/autenticacao/entrar",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          credentials: "include",
          body: JSON.stringify({
            email,
            senha: password,
          }),
        },
      );

      const data: LoginResponse | { mensagem: string } = await response.json();

      if (!response.ok) {
        setError(data.mensagem || "E-mail ou senha inválidos.");
        return;
      }

      const loginData = data as LoginResponse;

      localStorage.setItem("user", JSON.stringify(loginData.usuario));

      navigate("/home");
    } catch {
      setError(
        "Não foi possível conectar ao servidor. Verifique se o backend está rodando.",
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="login-page">
      <div className="background-glow glow-one"></div>
      <div className="background-glow glow-two"></div>

      <section className="login-container">
        <div className="login-showcase">
          <div className="collector-showcase">
            <div className="collector-row row-one">
              🎮 🧙 ⚔️ 🎲 🕹️ 🪄 🦸 💎 🎮 🧙 ⚔️ 🎲 🕹️ 🪄 🦸
            </div>

            <div className="collector-row row-two">
              🧸 🪙 🃏 👾 🗡️ 🛡️ 🧙‍♀️ 🎮 💎 🪄 👾 🧸 🪙 🃏
            </div>

            <div className="collector-row row-three">
              🃏 🎮 💎 ⚔️ 👾 🧙 🪄 🎲 🛡️ 🕹️ 🃏 🎮 💎 ⚔️
            </div>
          </div>

          <div className="brand">
            <span className="brand-symbol">✦</span>
            <span>RARITYROOM</span>
          </div>

          <div className="showcase-content">
            <span className="eyebrow">COLECIONE. ENCONTRE. CONQUISTE.</span>

            <h1>
              Seu universo
              <br />
              <span>geek</span> começa
              <br />
              aqui.
            </h1>

            <p>
              Encontre itens raros, edições limitadas e colecionáveis que
              merecem um lugar especial na sua coleção.
            </p>

            <div className="collection-cards">
              <div className="mini-card">
                <span className="mini-icon">◈</span>

                <div>
                  <strong>Itens raros</strong>
                  <small>Peças exclusivas</small>
                </div>
              </div>

              <div className="mini-card">
                <span className="mini-icon">✦</span>

                <div>
                  <strong>Coleções</strong>
                  <small>Seu acervo em um só lugar</small>
                </div>
              </div>
            </div>
          </div>

          <div className="showcase-footer">
            <span>EST. 2026</span>
            <span className="footer-line"></span>
            <span>FOR COLLECTORS</span>
          </div>
        </div>

        <div className="login-box">
          <div className="login-header">
            <div className="login-icon">♜</div>

            <h2>Bem-vindo de volta</h2>

            <p>
              Entre na sua conta e continue
              <br />
              sua jornada de colecionador.
            </p>
          </div>

          <form className="login-form" onSubmit={handleLogin}>
            <div className="input-group">
              <label htmlFor="email">E-MAIL</label>

              <div className="input-wrapper">
                <span className="input-icon">@</span>

                <input
                  id="email"
                  type="email"
                  placeholder="seu@email.com"
                  autoComplete="email"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  required
                />
              </div>
            </div>

            <div className="input-group">
              <div className="password-label">
                <label htmlFor="password">SENHA</label>
                <a href="#">Esqueceu a senha?</a>
              </div>

              <div className="input-wrapper">
                <span className="input-icon">◉</span>

                <input
                  id="password"
                  type="password"
                  placeholder="••••••••"
                  autoComplete="current-password"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  required
                />
              </div>
            </div>

            <label className="remember">
              <input type="checkbox" />
              <span className="custom-checkbox"></span>
              <span>Lembrar de mim</span>
            </label>

            {error && <p className="login-error">{error}</p>}

            <button type="submit" className="login-button" disabled={loading}>
              <span>{loading ? "ENTRANDO..." : "ENTRAR NA RARITYROOM"}</span>

              <span className="button-arrow">{loading ? "..." : "→"}</span>
            </button>
          </form>

          <p className="register">
            Ainda não é colecionador?{" "}
            <Link to="/register">Criar minha conta</Link>
          </p>

          <div className="security">
            <span>◆</span> COMPRA SEGURA &nbsp;•&nbsp; COLECIONADORES
            VERIFICADOS
          </div>
        </div>
      </section>
    </main>
  );
}

export default Login;
