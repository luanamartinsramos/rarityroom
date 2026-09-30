import { useState } from "react";
import toast from "react-hot-toast";
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

interface ErrorResponse {
  mensagem?: string;
  errors?: Record<string, string[]>;
}

function Login() {
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleLogin(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const trimmedEmail = email.trim();

    if (!trimmedEmail) {
      toast.error("Digite seu e-mail.");
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    if (!emailRegex.test(trimmedEmail)) {
      toast.error("Digite um e-mail válido.");
      return;
    }

    if (!password) {
      toast.error("Digite sua senha.");
      return;
    }

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
            email: trimmedEmail,
            senha: password,
          }),
        },
      );

      const data: LoginResponse | ErrorResponse | null = await response
        .json()
        .catch(() => null);

      if (!response.ok) {
        const errorData = data as ErrorResponse | null;

        if (response.status === 401) {
          toast.error(
            "E-mail ou senha incorretos. Verifique seus dados e tente novamente.",
          );
          return;
        }

        if (response.status === 400) {
          if (errorData?.errors) {
            Object.entries(errorData.errors).forEach(([field, messages]) => {
              messages.forEach((message) => {
                if (field === "Email") {
                  toast.error("Digite um e-mail válido.");
                } else if (field === "Senha") {
                  toast.error("Digite sua senha.");
                } else {
                  toast.error(message);
                }
              });
            });
          } else {
            toast.error(
              errorData?.mensagem || "Verifique os dados preenchidos.",
            );
          }

          return;
        }

        toast.error(
          errorData?.mensagem || "Não foi possível entrar na sua conta.",
        );

        return;
      }

      const loginData = data as LoginResponse;

      toast.success(`Bem-vindo de volta, ${loginData.usuario.nome}! ✨`);

      navigate("/home");
    } catch {
      toast.error(
        "Não conseguimos conectar ao servidor. Verifique se o backend está rodando.",
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

          <form className="login-form" onSubmit={handleLogin} noValidate>
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
                />
              </div>
            </div>

            <div className="input-group">
              <div className="password-label">
                <label htmlFor="password">SENHA</label>

                <a href="#" onClick={(event) => event.preventDefault()}>
                  Esqueceu a senha?
                </a>
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
                />
              </div>
            </div>

            <label className="remember">
              <input type="checkbox" />

              <span className="custom-checkbox"></span>

              <span>Lembrar de mim</span>
            </label>

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
