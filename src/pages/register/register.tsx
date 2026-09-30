import { useState } from "react";
import toast from "react-hot-toast";
import { Link, useNavigate } from "react-router-dom";
import "./register.css";

interface CadastroResponse {
  mensagem: string;
  usuario: {
    id: number;
    nome: string;
    email: string;
  };
}

interface ErrorResponse {
  mensagem?: string;
  errors?: Record<string, string[]>;
}

function Register() {
  const navigate = useNavigate();

  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [termsAccepted, setTermsAccepted] = useState(false);
  const [loading, setLoading] = useState(false);

  function validateForm() {
    const trimmedName = name.trim();
    const trimmedEmail = email.trim();

    if (!trimmedName) {
      toast.error("Digite seu nome completo.");
      return false;
    }

    if (trimmedName.length < 3) {
      toast.error("Seu nome precisa ter pelo menos 3 caracteres.");
      return false;
    }

    if (!trimmedEmail) {
      toast.error("Digite seu e-mail.");
      return false;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    if (!emailRegex.test(trimmedEmail)) {
      toast.error("Digite um e-mail válido.");
      return false;
    }

    if (!password) {
      toast.error("Digite uma senha.");
      return false;
    }

    if (password.length < 6) {
      toast.error("Sua senha precisa ter pelo menos 6 caracteres.");
      return false;
    }

    if (!confirmPassword) {
      toast.error("Confirme sua senha.");
      return false;
    }

    if (password !== confirmPassword) {
      toast.error("As senhas não coincidem. Verifique e tente novamente.");
      return false;
    }

    if (!termsAccepted) {
      toast.error(
        "Você precisa concordar com os Termos de Uso e a Política de Privacidade.",
      );
      return false;
    }

    return true;
  }

  async function handleRegister(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!validateForm()) {
      return;
    }

    setLoading(true);

    try {
      const response = await fetch(
        "http://localhost:5000/api/autenticacao/cadastrar",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          credentials: "include",
          body: JSON.stringify({
            nome: name.trim(),
            email: email.trim(),
            senha: password,
          }),
        },
      );

      const data: CadastroResponse | ErrorResponse | null = await response
        .json()
        .catch(() => null);

      if (!response.ok) {
        const errorData = data as ErrorResponse | null;

        if (response.status === 409) {
          toast.error(
            errorData?.mensagem ||
              "Este e-mail já está cadastrado. Tente entrar na sua conta.",
          );
          return;
        }

        if (response.status === 400) {
          if (errorData?.errors) {
            const messages = Object.entries(errorData.errors).flatMap(
              ([field, fieldMessages]) => {
                return fieldMessages.map((message) => {
                  const lowerMessage = message.toLowerCase();

                  if (field === "Nome") {
                    if (
                      lowerMessage.includes("required") ||
                      lowerMessage.includes("obrigatório")
                    ) {
                      return "Digite seu nome completo.";
                    }

                    return "Verifique o nome informado.";
                  }

                  if (field === "Email") {
                    if (lowerMessage.includes("required")) {
                      return "Digite seu e-mail.";
                    }

                    return "Digite um e-mail válido.";
                  }

                  if (field === "Senha") {
                    if (
                      lowerMessage.includes("minimum") ||
                      lowerMessage.includes("6")
                    ) {
                      return "Sua senha precisa ter pelo menos 6 caracteres.";
                    }

                    if (lowerMessage.includes("required")) {
                      return "Digite uma senha.";
                    }

                    return "Verifique a senha informada.";
                  }

                  return "Verifique os dados preenchidos.";
                });
              },
            );

            const uniqueMessages = [...new Set(messages)];

            uniqueMessages.forEach((message) => {
              toast.error(message);
            });

            return;
          }

          toast.error(
            errorData?.mensagem ||
              "Verifique os dados preenchidos e tente novamente.",
          );

          return;
        }

        toast.error(
          errorData?.mensagem ||
            "Não foi possível criar sua conta. Tente novamente.",
        );

        return;
      }

      const cadastroData = data as CadastroResponse;

      toast.success(
        "Conta criada com sucesso! ✨ Você já pode entrar na RarityRoom.",
      );

      setTimeout(() => {
        navigate("/", {
          state: {
            mensagem: cadastroData.mensagem,
            email: cadastroData.usuario.email,
          },
        });
      }, 1000);
    } catch {
      toast.error(
        "Não conseguimos conectar ao servidor. Verifique se o backend está rodando.",
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="register-page">
      <div className="register-glow register-glow-one"></div>
      <div className="register-glow register-glow-two"></div>

      <section className="register-container">
        <div className="register-header">
          <div className="register-brand">
            <span className="register-brand-symbol">✦</span>
            <span>RARITYROOM</span>
          </div>

          <div className="register-title">
            <span className="register-eyebrow">
              JUNTE-SE AOS COLECIONADORES
            </span>

            <h1>
              Crie sua
              <br />
              <span>conta.</span>
            </h1>

            <p>
              Entre para a RarityRoom e descubra um universo de itens raros,
              exclusivos e colecionáveis.
            </p>
          </div>
        </div>

        <form className="register-form" onSubmit={handleRegister} noValidate>
          <div className="register-input-group">
            <label htmlFor="name">NOME COMPLETO</label>

            <input
              id="name"
              type="text"
              placeholder="Seu nome completo"
              value={name}
              onChange={(event) => setName(event.target.value)}
            />
          </div>

          <div className="register-input-group">
            <label htmlFor="email">E-MAIL</label>

            <input
              id="email"
              type="email"
              placeholder="seu@email.com"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          </div>

          <div className="register-input-group">
            <label htmlFor="password">SENHA</label>

            <input
              id="password"
              type="password"
              placeholder="Crie uma senha segura"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          </div>

          <div className="register-input-group">
            <label htmlFor="confirmPassword">CONFIRMAR SENHA</label>

            <input
              id="confirmPassword"
              type="password"
              placeholder="Digite sua senha novamente"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
            />
          </div>

          <label className="terms">
            <input
              type="checkbox"
              checked={termsAccepted}
              onChange={(event) => setTermsAccepted(event.target.checked)}
            />

            <span className="terms-checkbox"></span>

            <span>
              Concordo com os{" "}
              <a href="#" onClick={(event) => event.preventDefault()}>
                Termos de Uso
              </a>{" "}
              e a{" "}
              <a href="#" onClick={(event) => event.preventDefault()}>
                Política de Privacidade
              </a>
              .
            </span>
          </label>

          <button type="submit" className="register-button" disabled={loading}>
            <span>{loading ? "CRIANDO CONTA..." : "CRIAR MINHA CONTA"}</span>

            <span className="register-arrow">{loading ? "..." : "→"}</span>
          </button>

          <p className="already-account">
            Já possui uma conta? <Link to="/">Entrar na RarityRoom</Link>
          </p>
        </form>
      </section>
    </main>
  );
}

export default Register;
