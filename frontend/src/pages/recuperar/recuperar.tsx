import { useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router-dom";

import { redefinirSenha, solicitarRecuperacao } from "../../services/senhaService";

import "../shared/paginas.css";

function RecuperarPagina() {
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [token, setToken] = useState("");
  const [novaSenha, setNovaSenha] = useState("");
  const [enviado, setEnviado] = useState(false);

  async function solicitar() {
    try {
      const mensagem = await solicitarRecuperacao(email);

      setEnviado(true);
      toast.success(mensagem);
    } catch (falha) {
      toast.error(
        falha instanceof Error ? falha.message : "Falha ao solicitar.",
      );
    }
  }

  async function redefinir() {
    try {
      await redefinirSenha(email, token.trim(), novaSenha);

      toast.success("Senha redefinida. Entre com a nova senha.");
      navigate("/login");
    } catch (falha) {
      toast.error(
        falha instanceof Error ? falha.message : "Falha ao redefinir.",
      );
    }
  }

  return (
    <main className="pagina">
      <div className="pagina-topo">
        <button onClick={() => navigate("/login")}>← Voltar</button>

        <h1>Recuperar senha</h1>
      </div>

      <form
        className="form-cartao"
        style={{ maxWidth: 560 }}
        onSubmit={(evento) => {
          evento.preventDefault();
          void solicitar();
        }}
      >
        <strong>1. Informe seu e-mail</strong>

        <p style={{ color: "#c7bdda", margin: "8px 0 16px", lineHeight: 1.5 }}>
          Enviaremos um código para redefinir a senha. Este projeto não tem
          servidor de e-mail: o “e-mail” é simulado e o código aparece no
          terminal onde o backend está rodando (linha <code>[E-MAIL SIMULADO]</code>).
        </p>

        <div className="campo-linha">
          <label className="campo">
            E-mail
            <input
              type="email"
              placeholder="seu@email.com"
              value={email}
              onChange={(evento) => setEmail(evento.target.value)}
            />
          </label>

          <button type="submit" className="botao" disabled={!email}>
            Enviar código
          </button>
        </div>
      </form>

      <form
        className="form-cartao"
        style={{ maxWidth: 560, opacity: enviado ? 1 : 0.6 }}
        onSubmit={(evento) => {
          evento.preventDefault();
          void redefinir();
        }}
      >
        <strong>2. Defina a nova senha</strong>

        <div className="grade-form" style={{ marginTop: 16 }}>
          <label className="campo col-cheia">
            Código recebido
            <input
              placeholder="Cole o código do e-mail"
              value={token}
              onChange={(evento) => setToken(evento.target.value)}
            />
          </label>

          <label className="campo col-cheia">
            Nova senha
            <input
              type="password"
              autoComplete="new-password"
              placeholder="Mínimo 8 caracteres, com letra e número"
              value={novaSenha}
              onChange={(evento) => setNovaSenha(evento.target.value)}
            />
          </label>
        </div>

        <div className="form-rodape">
          <button type="submit" className="botao-primario" disabled={!email || !token || !novaSenha}>
            Redefinir senha
          </button>
        </div>
      </form>
    </main>
  );
}

export default RecuperarPagina;
