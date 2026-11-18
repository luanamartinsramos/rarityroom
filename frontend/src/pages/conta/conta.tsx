import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router-dom";

import { obterFavoritos, removerFavorito } from "../../services/favoritosService";
import {
  concluirPedido,
  entregarPedido,
  enviarPedido,
  obterPedidos,
  ROTULO_PAGAMENTO,
  ROTULO_PEDIDO,
  type PedidoApi,
} from "../../services/pedidosService";
import { moeda, urlImagem, rotulo } from "../../utils/formato";
import type { Produto } from "../../services/produtosService";
import {
  aceitarProposta,
  cancelarProposta,
  obterPropostas,
  recusarProposta,
  type PropostaApi,
} from "../../services/propostasService";

import {
  atualizarLoja,
  criarLoja,
  enviarFotoLoja,
  obterMinhaLoja,
  type LojaApi,
} from "../../services/lojaService";

import "../shared/paginas.css";

type Aba = "compras" | "vendas" | "favoritos" | "propostas" | "loja";

interface ContaApi {
  id: string;
  nome: string;
  email: string;
  tipoConta?: string;
  administrador?: boolean;
}

function ContaPagina() {
  const navigate = useNavigate();

  const [conta, setConta] = useState<ContaApi | null>(null);
  const [pedidos, setPedidos] = useState<PedidoApi[]>([]);
  const [favoritos, setFavoritos] = useState<Produto[]>([]);
  const [propostas, setPropostas] = useState<PropostaApi[]>([]);
  const [aba, setAba] = useState<Aba>("compras");
  const [loja, setLoja] = useState<LojaApi | null>(null);
  const [nomeLoja, setNomeLoja] = useState("");
  const [descricaoLoja, setDescricaoLoja] = useState("");
  const [salvandoLoja, setSalvandoLoja] = useState(false);
  const [carregando, setCarregando] = useState(true);

  async function carregar() {
    try {
      const [dadosConta, listaPedidos, listaFavoritos, listaPropostas] =
        await Promise.all([
          fetch("/api/autenticacao/usuario-atual", {
            credentials: "include",
          }).then((resposta) => (resposta.ok ? resposta.json() : null)),
          obterPedidos().catch(() => []),
          obterFavoritos().catch(() => []),
          obterPropostas().catch(() => []),
        ]);

      setConta(dadosConta);
      setPedidos(listaPedidos);
      setFavoritos(listaFavoritos);
      setPropostas(listaPropostas);

      const minhaLoja = await obterMinhaLoja().catch(() => null);

      setLoja(minhaLoja);
      setNomeLoja(minhaLoja?.nome ?? "");
      setDescricaoLoja(minhaLoja?.descricao ?? "");
    } catch {
      toast.error("Não foi possível carregar sua conta.");
    } finally {
      setCarregando(false);
    }
  }

  useEffect(() => {
    // carregar() só altera estado depois do await, então não há render em cascata.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void carregar();
  }, []);

  async function agirProposta(id: number, acao: string) {
    try {
      if (acao === "aceitar") {
        await aceitarProposta(id);
      } else if (acao === "recusar") {
        await recusarProposta(id);
      } else {
        await cancelarProposta(id);
      }

      toast.success("Proposta atualizada.");

      await carregar();
    } catch (falha) {
      toast.error(
        falha instanceof Error ? falha.message : "Falha ao atualizar a proposta.",
      );
    }
  }

  async function salvarLoja() {
    if (salvandoLoja) {
      return;
    }

    setSalvandoLoja(true);

    try {
      const salva = loja
        ? await atualizarLoja(nomeLoja, descricaoLoja)
        : await criarLoja(nomeLoja, descricaoLoja);

      setLoja(salva);

      toast.success(loja ? "Loja atualizada." : "Loja criada!");
    } catch (falha) {
      toast.error(
        falha instanceof Error ? falha.message : "Falha ao salvar a loja.",
      );
    } finally {
      setSalvandoLoja(false);
    }
  }

  async function enviarFoto(evento: React.ChangeEvent<HTMLInputElement>) {
    const arquivo = evento.target.files?.[0];

    if (!arquivo) {
      return;
    }

    try {
      const atualizada = await enviarFotoLoja(arquivo);

      setLoja(atualizada);

      toast.success("Foto da loja atualizada.");
    } catch (falha) {
      toast.error(
        falha instanceof Error ? falha.message : "Falha ao enviar a foto.",
      );
    }
  }

  async function agirPedido(id: number, acao: "concluir" | "enviar" | "entregar") {
    try {
      if (acao === "concluir") {
        await concluirPedido(id);
        toast.success("Pedido concluído.");
      } else if (acao === "enviar") {
        await enviarPedido(id);
        toast.success("Pedido marcado como enviado.");
      } else {
        await entregarPedido(id);
        toast.success("Entrega confirmada.");
      }

      await carregar();
    } catch (falha) {
      toast.error(
        falha instanceof Error ? falha.message : "Falha ao atualizar o pedido.",
      );
    }
  }

  async function desfavoritar(id: number) {
    try {
      await removerFavorito(id);

      toast.success("Removido dos favoritos.");

      await carregar();
    } catch (falha) {
      toast.error(
        falha instanceof Error ? falha.message : "Falha ao remover o favorito.",
      );
    }
  }

  if (carregando) {
    return (
      <main className="pagina">
        <div className="pagina-carregando">Carregando sua conta...</div>
      </main>
    );
  }

  const meuId = Number(conta?.id ?? 0);

  const compras = pedidos.filter((pedido) => pedido.compradorId === meuId);
  const vendas = pedidos.filter((pedido) => pedido.vendedorId === meuId);

  return (
    <main className="pagina">
      <div className="pagina-topo">
        <button onClick={() => navigate("/home")}>← Catálogo</button>

        <h1>Minha conta</h1>
      </div>

      {conta && (
        <div className="produto-tags">
          <span className="produto-tag">{conta.nome}</span>
          <span className="produto-tag">{conta.email}</span>
          <span className="produto-tag">Perfil: {rotulo(conta.tipoConta)}</span>
          {conta.administrador && (
            <span className="produto-tag">Administrador</span>
          )}
        </div>
      )}

      <div className="atalhos">
        <button onClick={() => navigate("/anuncios/novo")}>＋ Publicar anúncio</button>
        <button onClick={() => navigate("/pedidos")}>Histórico de compras</button>
        <button onClick={() => navigate("/carrinho")}>Carrinho</button>
        <button onClick={() => navigate("/notificacoes")}>Notificações</button>
        <button onClick={() => navigate("/perfil")}>Perfil e endereços</button>
      </div>

      <div className="abas" role="tablist">
        {(
          [
            ["compras", `Compras (${compras.length})`],
            ["vendas", `Vendas (${vendas.length})`],
            ["favoritos", `Favoritos (${favoritos.length})`],
            ["propostas", `Propostas (${propostas.length})`],
            ["loja", "Minha loja"],
          ] as [Aba, string][]
        ).map(([valor, texto]) => (
          <button
            key={valor}
            role="tab"
            aria-selected={aba === valor}
            className={aba === valor ? "ativa" : ""}
            onClick={() => setAba(valor)}
          >
            {texto}
          </button>
        ))}
      </div>

      {aba === "compras" && (
        compras.length === 0 ? (
          <div className="pagina-vazio">
            <span>◇</span>
            <h3>Nenhuma compra ainda</h3>
            <p>Quando você comprar algo, o pedido aparece aqui.</p>
          </div>
        ) : (
          <div className="avaliacoes">
            {compras.map((pedido) => (
              <div className="notificacao" key={pedido.id}>
                <div>
                  <strong>Pedido {pedido.codigo}</strong>

                  <p>
                    {pedido.itens
                      .map((item) => `${item.quantidade}× ${item.nomeProduto}`)
                      .join(", ")}{" "}
                    · {moeda(pedido.total)}
                  </p>

                  <small>
                    {ROTULO_PEDIDO[pedido.estado] ?? pedido.estado} ·{" "}
                    {ROTULO_PAGAMENTO[pedido.statusPagamento] ?? pedido.statusPagamento} ·{" "}
                    {pedido.formaPagamento} ·{" "}
                    {new Date(pedido.criadoEm).toLocaleDateString("pt-BR")} ·
                    entrega em {pedido.entrega.cidade}/{pedido.entrega.estado}
                  </small>
                </div>

                <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                  {pedido.estado === "Pendente" && pedido.pagamentoId && (
                    <button onClick={() => navigate(`/pagamento/${pedido.pagamentoId}`)}>
                      Pagar
                    </button>
                  )}

                  <button onClick={() => navigate(`/pedidos/${pedido.id}`)}>
                    Detalhes
                  </button>

                  {pedido.estado === "Enviado" && (
                    <button onClick={() => agirPedido(pedido.id, "entregar")}>
                      Confirmar entrega
                    </button>
                  )}

                  <button onClick={() => navigate(`/vendedor/${pedido.vendedorId}`)}>
                    Ver vendedor
                  </button>
                </div>
              </div>
            ))}
          </div>
        )
      )}

      {aba === "vendas" && (
        vendas.length === 0 ? (
          <div className="pagina-vazio">
            <span>◇</span>
            <h3>Nenhuma venda ainda</h3>
            <p>Publique anúncios na área do vendedor para começar.</p>
          </div>
        ) : (
          <div className="avaliacoes">
            {vendas.map((pedido) => (
              <div className="notificacao" key={pedido.id}>
                <div>
                  <strong>Pedido {pedido.codigo}</strong>

                  <p>
                    {pedido.itens
                      .map((item) => `${item.quantidade}× ${item.nomeProduto}`)
                      .join(", ")}{" "}
                    · {moeda(pedido.total)}
                  </p>

                  <small>
                    {ROTULO_PEDIDO[pedido.estado] ?? pedido.estado} ·{" "}
                    {ROTULO_PAGAMENTO[pedido.statusPagamento] ?? pedido.statusPagamento} · comprador #
                    {pedido.compradorId} · entrega em {pedido.entrega.cidade}/{pedido.entrega.estado}
                  </small>
                </div>

                <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                  {pedido.estado === "Pago" && (
                    <button onClick={() => agirPedido(pedido.id, "enviar")}>
                      Marcar como enviado
                    </button>
                  )}

                  {["Pago", "Enviado", "Entregue"].includes(pedido.estado) && (
                    <button onClick={() => agirPedido(pedido.id, "concluir")}>
                      Concluir
                    </button>
                  )}

                  <button onClick={() => navigate(`/pedidos/${pedido.id}`)}>
                    Detalhes
                  </button>
                </div>
              </div>
            ))}
          </div>
        )
      )}

      {aba === "favoritos" && (
        favoritos.length === 0 ? (
          <div className="pagina-vazio">
            <span>♡</span>
            <h3>Nenhum favorito</h3>
            <p>Toque no coração de um anúncio para guardá-lo aqui.</p>
          </div>
        ) : (
          <div className="grade-produtos">
            {favoritos.map((produto) => (
              <article className="cartao-produto" key={produto.id}>
                <div
                  className="cartao-produto-imagem"
                  onClick={() => navigate(`/produto/${produto.id}`)}
                >
                  {produto.imagemUrl ? (
                    <img src={urlImagem(produto.imagemUrl)} alt={produto.nome} />
                  ) : (
                    <span>◇</span>
                  )}
                </div>

                <div className="cartao-produto-info">
                  <strong>{produto.nome}</strong>
                  <span>{moeda(produto.preco)}</span>

                  <button onClick={() => desfavoritar(produto.id)}>
                    Remover
                  </button>
                </div>
              </article>
            ))}
          </div>
        )
      )}

      {aba === "propostas" && (
        propostas.length === 0 ? (
          <div className="pagina-vazio">
            <span>◇</span>
            <h3>Nenhuma proposta</h3>
            <p>Suas ofertas e as ofertas recebidas aparecem aqui.</p>
          </div>
        ) : (
          <div className="avaliacoes">
            {propostas.map((proposta) => {
              const minha = proposta.proponenteId === meuId;

              return (
                <div className="notificacao" key={proposta.id}>
                  <div>
                    <strong>
                      {moeda(proposta.valor)} ·{" "}
                      {proposta.estado}
                    </strong>

                    <p>{proposta.mensagem || "Sem mensagem."}</p>

                    <small>
                      {minha ? "Enviada por você" : "Recebida"} · anúncio #
                      {proposta.produtoId} ·{" "}
                      {new Date(proposta.criadaEm).toLocaleDateString("pt-BR")}
                    </small>
                  </div>

                  {proposta.estado === "Pendente" && (
                    <div style={{ display: "flex", gap: 8 }}>
                      {minha ? (
                        <button onClick={() => agirProposta(proposta.id, "cancelar")}>
                          Cancelar
                        </button>
                      ) : (
                        <>
                          <button onClick={() => agirProposta(proposta.id, "aceitar")}>
                            Aceitar
                          </button>

                          <button onClick={() => agirProposta(proposta.id, "recusar")}>
                            Recusar
                          </button>
                        </>
                      )}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )
      )}
      {aba === "loja" && (
        <div className="avaliacoes">
          <div className="notificacao">
            <div style={{ width: "100%" }}>
              <strong>{loja ? "Editar loja" : "Criar minha loja"}</strong>

              <p>
                Nome, descrição e foto aparecem no seu perfil público de
                vendedor.
              </p>

              <label style={{ display: "block", marginTop: 12 }}>
                Nome da loja
                <input
                  value={nomeLoja}
                  onChange={(evento) => setNomeLoja(evento.target.value)}
                  placeholder="Coleção da Ana"
                  style={{
                    width: "100%",
                    padding: 10,
                    marginTop: 6,
                    borderRadius: 8,
                    border: "1px solid rgba(255,255,255,.15)",
                    background: "rgba(255,255,255,.04)",
                    color: "inherit",
                  }}
                />
              </label>

              <label style={{ display: "block", marginTop: 12 }}>
                Descrição
                <textarea
                  value={descricaoLoja}
                  onChange={(evento) => setDescricaoLoja(evento.target.value)}
                  rows={4}
                  placeholder="O que você vende, de onde envia, condições..."
                  style={{
                    width: "100%",
                    padding: 10,
                    marginTop: 6,
                    borderRadius: 8,
                    border: "1px solid rgba(255,255,255,.15)",
                    background: "rgba(255,255,255,.04)",
                    color: "inherit",
                  }}
                />
              </label>

              <div style={{ display: "flex", gap: 10, marginTop: 14, alignItems: "center" }}>
                <button onClick={salvarLoja} disabled={salvandoLoja}>
                  {salvandoLoja
                    ? "Salvando..."
                    : loja
                      ? "Salvar alterações"
                      : "Criar loja"}
                </button>

                {loja && (
                  <label
                    style={{
                      cursor: "pointer",
                      padding: "8px 12px",
                      borderRadius: 8,
                      border: "1px solid rgba(255,255,255,.15)",
                      fontSize: 13,
                    }}
                  >
                    Enviar foto
                    <input
                      type="file"
                      accept="image/png,image/jpeg,image/webp"
                      onChange={enviarFoto}
                      style={{ display: "none" }}
                    />
                  </label>
                )}
              </div>

              {loja?.fotoUrl && (
                <img
                  src={urlImagem(loja.fotoUrl)}
                  alt={loja.nome}
                  style={{
                    marginTop: 16,
                    width: 96,
                    height: 96,
                    objectFit: "cover",
                    borderRadius: 12,
                  }}
                />
              )}

              {loja && (
                <p style={{ marginTop: 12, fontSize: 12, color: "#9b9b9b" }}>
                  Loja pública em /vendedor/{loja.donoId}
                </p>
              )}
            </div>
          </div>
        </div>
      )}
    </main>
  );
}

export default ContaPagina;
