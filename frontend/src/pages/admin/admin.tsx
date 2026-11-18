import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router-dom";

import {
  listarAnuncios,
  listarPedidos,
  listarUsuarios,
  moderarAnuncio,
  obterResumo,
  reativarUsuario,
  suspenderUsuario,
  type ResumoAdmin,
  type UsuarioAdmin,
} from "../../services/adminService";
import type { PedidoApi } from "../../services/pedidosService";
import type { Produto } from "../../services/produtosService";

import "../shared/paginas.css";
import { moeda } from "../../utils/formato";

type Aba = "usuarios" | "anuncios" | "pedidos";

function AdminPagina() {
  const navigate = useNavigate();

  const [resumo, setResumo] = useState<ResumoAdmin | null>(null);
  const [usuarios, setUsuarios] = useState<UsuarioAdmin[]>([]);
  const [anuncios, setAnuncios] = useState<Produto[]>([]);
  const [pedidos, setPedidos] = useState<PedidoApi[]>([]);
  const [aba, setAba] = useState<Aba>("usuarios");
  const [erro, setErro] = useState("");
  const [carregando, setCarregando] = useState(true);

  async function carregar() {
    try {
      const [dadosResumo, listaUsuarios, listaAnuncios, listaPedidos] =
        await Promise.all([
          obterResumo(),
          listarUsuarios(),
          listarAnuncios(),
          listarPedidos(),
        ]);

      setResumo(dadosResumo);
      setUsuarios(listaUsuarios);
      setAnuncios(listaAnuncios);
      setPedidos(listaPedidos);
    } catch (falha) {
      setErro(
        falha instanceof Error ? falha.message : "Falha ao carregar o painel.",
      );
    } finally {
      setCarregando(false);
    }
  }

  useEffect(() => {
    // carregar() só altera estado depois do await, então não há render em cascata.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void carregar();
  }, []);

  async function alternarConta(usuario: UsuarioAdmin) {
    try {
      if (usuario.ativo) {
        await suspenderUsuario(usuario.id);
        toast.success("Conta suspensa.");
      } else {
        await reativarUsuario(usuario.id);
        toast.success("Conta reativada.");
      }

      await carregar();
    } catch (falha) {
      toast.error(falha instanceof Error ? falha.message : "Falha na operação.");
    }
  }

  async function moderar(anuncio: Produto, acao: string) {
    try {
      await moderarAnuncio(anuncio.id, acao);

      toast.success(
        acao === "remover" ? "Anúncio removido." : "Anúncio pausado.",
      );

      await carregar();
    } catch (falha) {
      toast.error(falha instanceof Error ? falha.message : "Falha na moderação.");
    }
  }

  if (carregando) {
    return (
      <main className="pagina">
        <div className="pagina-carregando">Carregando painel...</div>
      </main>
    );
  }

  if (erro) {
    return (
      <main className="pagina">
        <div className="pagina-vazio">
          <h3>{erro}</h3>

          <button onClick={() => navigate("/home")}>Voltar</button>
        </div>
      </main>
    );
  }

  return (
    <main className="pagina">
      <div className="pagina-topo">
        <button onClick={() => navigate("/home")}>← Catálogo</button>

        <h1>Administração</h1>
      </div>

      {resumo && (
        <div className="produto-tags">
          <span className="produto-tag">Usuários: {resumo.usuarios}</span>
          <span className="produto-tag">Ativos: {resumo.usuariosAtivos}</span>
          <span className="produto-tag">
            Anúncios ativos: {resumo.anunciosAtivos}
          </span>
          <span className="produto-tag">
            Vendidos: {resumo.anunciosVendidos}
          </span>
          <span className="produto-tag">Pedidos: {resumo.pedidos}</span>
          <span className="produto-tag">
            Total vendido: {moeda(resumo.totalVendido)}
          </span>
        </div>
      )}

      <div className="produto-acoes" style={{ marginBottom: 20 }}>
        <button onClick={() => setAba("usuarios")}>
          Usuários ({usuarios.length})
        </button>

        <button onClick={() => setAba("anuncios")}>
          Anúncios ({anuncios.length})
        </button>

        <button onClick={() => setAba("pedidos")}>
          Pedidos ({pedidos.length})
        </button>
      </div>

      {aba === "usuarios" && (
        <div className="avaliacoes">
          {usuarios.map((usuario) => (
            <div className="notificacao" key={usuario.id}>
              <div>
                <strong>{usuario.nome}</strong>

                <p>{usuario.email}</p>

                <small>
                  {usuario.tipoConta} · {usuario.ativo ? "ativa" : "suspensa"}
                  {usuario.administrador ? " · administrador" : ""}
                </small>
              </div>

              {!usuario.administrador && (
                <button onClick={() => alternarConta(usuario)}>
                  {usuario.ativo ? "Suspender" : "Reativar"}
                </button>
              )}
            </div>
          ))}
        </div>
      )}

      {aba === "anuncios" && (
        <div className="avaliacoes">
          {anuncios.map((anuncio) => (
            <div className="notificacao" key={anuncio.id}>
              <div>
                <strong>{anuncio.nome}</strong>

                <p>
                  {moeda(anuncio.preco)} ·{" "}
                  {anuncio.categoria} · {anuncio.estado}
                </p>

                <small>
                  Vendedor #{anuncio.usuarioId} · estoque {anuncio.estoque}
                </small>
              </div>

              <div style={{ display: "flex", gap: 8 }}>
                <button onClick={() => moderar(anuncio, "pausar")}>
                  Pausar
                </button>

                <button onClick={() => moderar(anuncio, "remover")}>
                  Remover
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {aba === "pedidos" && (
        <div className="avaliacoes">
          {pedidos.length === 0 ? (
            <div className="pagina-vazio">
              <p>Nenhum pedido registrado.</p>
            </div>
          ) : (
            pedidos.map((pedido) => (
              <div className="notificacao" key={pedido.id}>
                <div>
                  <strong>Pedido {pedido.codigo}</strong>

                  <p>
                    {pedido.nomeProduto} · {pedido.quantidadeTotal} un. ·{" "}
                    {moeda(pedido.total)}
                  </p>

                  <small>
                    {pedido.estado} · {pedido.statusPagamento} · {pedido.formaPagamento} · comprador #
                    {pedido.compradorId}
                  </small>
                </div>
              </div>
            ))
          )}
        </div>
      )}
    </main>
  );
}

export default AdminPagina;
