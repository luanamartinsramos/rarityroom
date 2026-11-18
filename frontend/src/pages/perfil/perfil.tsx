import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router-dom";

import {
  criarEndereco,
  definirPrincipal,
  obterEnderecos,
  removerEndereco,
  type EnderecoApi,
  type EnderecoForm,
} from "../../services/enderecosService";
import {
  alterarEmail,
  alterarNome,
  enviarFotoPerfil,
  obterPerfil,
  type PerfilApi,
} from "../../services/perfilService";

import { urlImagem, rotulo } from "../../utils/formato";
import "../shared/paginas.css";

const ENDERECO_VAZIO: EnderecoForm = {
  cep: "",
  logradouro: "",
  numero: "",
  complemento: "",
  bairro: "",
  cidade: "",
  estado: "",
};

function PerfilPagina() {
  const navigate = useNavigate();

  const [perfil, setPerfil] = useState<PerfilApi | null>(null);
  const [nome, setNome] = useState("");
  const [email, setEmail] = useState("");
  const [senhaAtual, setSenhaAtual] = useState("");
  const [enderecos, setEnderecos] = useState<EnderecoApi[]>([]);
  const [novo, setNovo] = useState<EnderecoForm>(ENDERECO_VAZIO);
  const [carregando, setCarregando] = useState(true);

  async function carregar() {
    try {
      const [dados, lista] = await Promise.all([
        obterPerfil(),
        obterEnderecos().catch(() => []),
      ]);

      setPerfil(dados);
      setNome(dados.nome);
      setEmail(dados.email);
      setEnderecos(lista);
    } catch {
      toast.error("Não foi possível carregar seu perfil.");
    } finally {
      setCarregando(false);
    }
  }

  useEffect(() => {
    // carregar() só altera estado depois do await, então não há render em cascata.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void carregar();
  }, []);

  async function salvarNome() {
    try {
      const atualizado = await alterarNome(nome);

      setPerfil(atualizado);

      toast.success("Nome atualizado.");
    } catch (falha) {
      toast.error(falha instanceof Error ? falha.message : "Falha ao salvar.");
    }
  }

  async function salvarEmail() {
    try {
      const atualizado = await alterarEmail(email, senhaAtual);

      setPerfil(atualizado);
      setSenhaAtual("");

      toast.success("E-mail atualizado.");
    } catch (falha) {
      toast.error(falha instanceof Error ? falha.message : "Falha ao salvar.");
    }
  }

  async function enviarFoto(evento: React.ChangeEvent<HTMLInputElement>) {
    const arquivo = evento.target.files?.[0];

    if (!arquivo) {
      return;
    }

    try {
      const atualizado = await enviarFotoPerfil(arquivo);

      setPerfil(atualizado);

      toast.success("Foto de perfil atualizada.");
    } catch (falha) {
      toast.error(falha instanceof Error ? falha.message : "Falha no envio.");
    }
  }

  async function adicionarEndereco() {
    try {
      await criarEndereco(novo);

      setNovo(ENDERECO_VAZIO);

      toast.success("Endereço adicionado.");

      setEnderecos(await obterEnderecos());
    } catch (falha) {
      toast.error(
        falha instanceof Error ? falha.message : "Falha ao salvar endereço.",
      );
    }
  }

  async function remover(id: number) {
    try {
      await removerEndereco(id);

      toast.success("Endereço removido.");

      setEnderecos(await obterEnderecos());
    } catch (falha) {
      toast.error(falha instanceof Error ? falha.message : "Falha ao remover.");
    }
  }

  async function tornarPrincipal(id: number) {
    try {
      await definirPrincipal(id);

      setEnderecos(await obterEnderecos());
    } catch (falha) {
      toast.error(
        falha instanceof Error ? falha.message : "Falha ao definir principal.",
      );
    }
  }

  if (carregando) {
    return (
      <main className="pagina">
        <div className="pagina-carregando">Carregando perfil...</div>
      </main>
    );
  }

  return (
    <main className="pagina">
      <div className="pagina-topo">
        <button onClick={() => navigate("/conta")}>← Minha conta</button>

        <h1>Perfil</h1>
      </div>

      {perfil && (
        <div className="vendedor-card" style={{ marginTop: 0 }}>
          <div className="vendedor-avatar">
            {perfil.fotoUrl ? (
              <img
                src={urlImagem(perfil.fotoUrl)}
                alt={perfil.nome}
                style={{
                  width: "100%",
                  height: "100%",
                  borderRadius: "50%",
                  objectFit: "cover",
                }}
              />
            ) : (
              perfil.nome.charAt(0).toUpperCase()
            )}
          </div>

          <div style={{ flex: 1 }}>
            <strong>{perfil.nome}</strong>

            <div style={{ fontSize: 13, color: "#9b9b9b" }}>
              {perfil.email} · {rotulo(perfil.tipoConta)}
            </div>
          </div>

          <label className="botao">
            Trocar foto
            <input
              type="file"
              accept="image/png,image/jpeg,image/webp"
              onChange={enviarFoto}
              style={{ display: "none" }}
            />
          </label>
        </div>
      )}

      <h2>Dados da conta</h2>

      <div className="form-cartao">
        <div className="campo-linha">
          <label className="campo">
            Nome
            <input
              value={nome}
              onChange={(evento) => setNome(evento.target.value)}
            />
          </label>

          <button className="botao" onClick={salvarNome}>
            Salvar nome
          </button>
        </div>

        <div className="campo-linha" style={{ marginTop: 16 }}>
          <label className="campo">
            E-mail
            <input
              type="email"
              value={email}
              onChange={(evento) => setEmail(evento.target.value)}
            />
          </label>

          <label className="campo">
            Senha atual
            <input
              type="password"
              autoComplete="current-password"
              placeholder="Confirme para trocar o e-mail"
              value={senhaAtual}
              onChange={(evento) => setSenhaAtual(evento.target.value)}
            />
          </label>

          <button className="botao" onClick={salvarEmail} disabled={!senhaAtual}>
            Salvar e-mail
          </button>
        </div>
      </div>

      <h2>Endereços</h2>

      {enderecos.length === 0 ? (
        <div className="pagina-vazio">
          <p>Nenhum endereço salvo. Cadastre o primeiro abaixo.</p>
        </div>
      ) : (
        <div className="avaliacoes">
          {enderecos.map((endereco) => (
            <div className="notificacao" key={endereco.id}>
              <div>
                <strong>
                  {endereco.logradouro}, {endereco.numero}
                  {endereco.principal ? " · principal" : ""}
                </strong>

                <p>
                  {endereco.bairro} · {endereco.cidade}/{endereco.estado}
                </p>

                <small>CEP {endereco.cep}</small>
              </div>

              <div className="endereco-acoes">
                {!endereco.principal && (
                  <button onClick={() => tornarPrincipal(endereco.id)}>
                    Tornar principal
                  </button>
                )}

                <button onClick={() => remover(endereco.id)}>Remover</button>
              </div>
            </div>
          ))}
        </div>
      )}

      <h2>Novo endereço</h2>

      <div className="form-cartao">
        <div className="grade-form">
          <label className="campo col-2">
            CEP
            <input placeholder="00000-000"
              value={novo.cep}
              onChange={(evento) =>
                setNovo({ ...novo, cep: evento.target.value })
              }
            />
          </label>
          <label className="campo col-4">
            Logradouro
            <input placeholder="Rua, avenida..."
              value={novo.logradouro}
              onChange={(evento) =>
                setNovo({ ...novo, logradouro: evento.target.value })
              }
            />
          </label>
          <label className="campo col-2">
            Número
            <input
              value={novo.numero}
              onChange={(evento) =>
                setNovo({ ...novo, numero: evento.target.value })
              }
            />
          </label>
          <label className="campo col-4">
            Complemento
            <input placeholder="Apto, bloco (opcional)"
              value={novo.complemento}
              onChange={(evento) =>
                setNovo({ ...novo, complemento: evento.target.value })
              }
            />
          </label>
          <label className="campo col-2">
            Bairro
            <input
              value={novo.bairro}
              onChange={(evento) =>
                setNovo({ ...novo, bairro: evento.target.value })
              }
            />
          </label>
          <label className="campo col-2">
            Cidade
            <input
              value={novo.cidade}
              onChange={(evento) =>
                setNovo({ ...novo, cidade: evento.target.value })
              }
            />
          </label>
          <label className="campo col-2">
            UF
            <input maxLength={2} placeholder="SP"
              value={novo.estado}
              onChange={(evento) =>
                setNovo({ ...novo, estado: evento.target.value })
              }
            />
          </label>
        </div>

        <div className="form-rodape">
          <button className="botao-primario" onClick={adicionarEndereco}>
            Adicionar endereço
          </button>
        </div>
      </div>
    </main>
  );
}

export default PerfilPagina;
