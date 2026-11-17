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
  const [tokenGerado, setTokenGerado] = useState<string | null>(null);

  async function solicitar() {
    try {
      const gerado = await solicitarRecuperacao(email);

      if (gerado) {
        setToken(gerado);
        setTokenGerado(gerado);
        toast.success("Token gerado.");
      } else {
        setTokenGerado(null);
        toast("Se este e-mail existir, um token foi gerado.", { icon: "ℹ️" });
      }
    } catch (falha) {
      toast.error(
        falha instanceof Error ? falha.message : "Falha ao solicitar.",
      );
    }
  }

  async function redefinir() {
    try {
      await redefinirSenha(email, token, novaSenha);

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

      <div className="avaliacoes" style={{ maxWidth: 520 }}>
        <div className="notificacao">
          <div style={{ width: "100%" }}>
            <strong>1. Informe seu e-mail</strong>

            <p>
              Sem provedor de e-mail configurado, o token aparece na tela —
              este fluxo é demonstrativo.
            </p>

            <input
              placeholder="seu@email.com"
              value={email}
              onChange={(evento) => setEmail(evento.target.value)}
              style={{
                width: "100%",
                padding: 10,
                marginTop: 10,
                borderRadius: 8,
                border: "1px solid rgba(255,255,255,.15)",
                background: "rgba(255,255,255,.04)",
                color: "inherit",
              }}
            />

            <button style={{ marginTop: 10 }} onClick={solicitar}>
              Gerar token
            </button>

            {tokenGerado && (
              <p style={{ marginTop: 12, wordBreak: "break-all" }}>
                <small>Token de demonstração:</small>
                <br />
                <code>{tokenGerado}</code>
              </p>
            )}
          </div>
        </div>

        <div className="notificacao">
          <div style={{ width: "100%" }}>
            <strong>2. Defina a nova senha</strong>

            <input
              placeholder="Token recebido"
              value={token}
              onChange={(evento) => setToken(evento.target.value)}
              style={{
                width: "100%",
                padding: 10,
                marginTop: 10,
                borderRadius: 8,
                border: "1px solid rgba(255,255,255,.15)",
                background: "rgba(255,255,255,.04)",
                color: "inherit",
              }}
            />

            <input
              type="password"
              placeholder="Nova senha (mínimo 6)"
              value={novaSenha}
              onChange={(evento) => setNovaSenha(evento.target.value)}
              style={{
                width: "100%",
                padding: 10,
                marginTop: 10,
                borderRadius: 8,
                border: "1px solid rgba(255,255,255,.15)",
                background: "rgba(255,255,255,.04)",
                color: "inherit",
              }}
            />

            <button style={{ marginTop: 10 }} onClick={redefinir}>
              Redefinir senha
            </button>
          </div>
        </div>
      </div>
    </main>
  );
}

export default RecuperarPagina;
