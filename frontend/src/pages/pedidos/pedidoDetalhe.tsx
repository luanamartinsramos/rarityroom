import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate, useParams } from "react-router-dom";

import { obterResumoProduto } from "../../services/avaliacoesService";
import {
  avaliarItem,
  cancelarPedido,
  concluirPedido,
  entregarPedido,
  enviarPedido,
  obterPedido,
  ROTULO_FORMA,
  ROTULO_PAGAMENTO,
  ROTULO_PEDIDO,
  type ItemPedidoApi,
  type PedidoApi,
} from "../../services/pedidosService";
import { dataHora, moeda, urlImagem } from "../../utils/formato";
import { classeSelo, etapaAtual, NOMES_ETAPAS, obterMeuId } from "./rotulos";

import "../shared/paginas.css";
import "../shared/compra.css";

function AvaliarItem({
  pedidoId,
  item,
  aoAvaliar,
}: {
  pedidoId: number;
  item: ItemPedidoApi;
  aoAvaliar: () => void;
}) {
  const [nota, setNota] = useState(0);
  const [comentario, setComentario] = useState("");
  const [enviando, setEnviando] = useState(false);

  async function enviar() {
    if (nota < 1) {
      toast.error("Escolha de 1 a 5 estrelas.");
      return;
    }

    setEnviando(true);

    try {
      await avaliarItem(pedidoId, item.produtoId, nota, comentario);
      toast.success("Avaliação registrada. Obrigado!");
      aoAvaliar();
    } catch (falha) {
      toast.error(falha instanceof Error ? falha.message : "Não foi possível avaliar.");
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div style={{ marginTop: 8 }}>
      <div className="estrelas-escolha" role="radiogroup" aria-label="Nota">
        {[1, 2, 3, 4, 5].map((valor) => (
          <button
            key={valor}
            type="button"
            className={valor <= nota ? "ativa" : ""}
            aria-label={`${valor} estrela(s)`}
            onClick={() => setNota(valor)}
          >
            ★
          </button>
        ))}
      </div>
      <div className="campo" style={{ marginTop: 6 }}>
        <textarea
          rows={2}
          maxLength={500}
          placeholder="Conte como foi (opcional)"
          value={comentario}
          onChange={(evento) => setComentario(evento.target.value)}
        />
      </div>
      <button className="botao-secundario" style={{ marginTop: 8 }} disabled={enviando} onClick={() => void enviar()}>
        Enviar avaliação
      </button>
    </div>
  );
}

function PedidoDetalhePagina() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [pedido, setPedido] = useState<PedidoApi | null>(null);
  const [meuId, setMeuId] = useState(0);
  const [avaliados, setAvaliados] = useState<number[]>([]);
  const [erro, setErro] = useState("");
  const [ocupado, setOcupado] = useState(false);

  async function carregar() {
    try {
      const [dados, usuario] = await Promise.all([obterPedido(Number(id)), obterMeuId()]);
      await aplicar(dados, usuario);
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : "Pedido não encontrado.");
    }
  }

  async function aplicar(dados: PedidoApi, usuario: number) {
    {

      setPedido(dados);
      setMeuId(usuario);

      const resumos = await Promise.all(
        dados.itens.map((item) => obterResumoProduto(item.produtoId).catch(() => null)),
      );

      setAvaliados(
        dados.itens
          .filter((_, indice) =>
            resumos[indice]?.avaliacoes.some((avaliacao) => avaliacao.pedidoId === dados.id),
          )
          .map((item) => item.produtoId),
      );
    }
  }

  useEffect(() => {
    Promise.all([obterPedido(Number(id)), obterMeuId()])
      .then(([dados, usuario]) => aplicar(dados, usuario))
      .catch((falha: Error) => setErro(falha.message || "Pedido não encontrado."));
  }, [id]);

  async function agir(acao: () => Promise<unknown>, sucesso: string) {
    setOcupado(true);

    try {
      await acao();
      toast.success(sucesso);
      await carregar();
    } catch (falha) {
      toast.error(falha instanceof Error ? falha.message : "Não foi possível atualizar o pedido.");
    } finally {
      setOcupado(false);
    }
  }

  if (erro) {
    return (
      <main className="pagina">
        <div className="pagina-vazio">
          <h3>{erro}</h3>
          <button className="botao-primario" onClick={() => navigate("/pedidos")}>
            Minhas compras
          </button>
        </div>
      </main>
    );
  }

  if (!pedido) {
    return (
      <main className="pagina">
        <div className="pagina-carregando">Carregando pedido...</div>
      </main>
    );
  }

  const souComprador = pedido.compradorId === meuId;
  const souVendedor = pedido.vendedorId === meuId;
  const etapa = etapaAtual(pedido.estado);
  const recebido = pedido.estado === "Entregue" || pedido.estado === "Concluido";

  return (
    <main className="pagina">
      <div className="pagina-topo">
        <button onClick={() => navigate(souComprador ? "/pedidos" : "/conta")}>← Voltar</button>
        <h1>Pedido {pedido.codigo}</h1>
      </div>

      <div className="compra-layout">
        <div>
          <section className="compra-bloco">
            <div className="pedido-cabecalho">
              <small>Feito em {dataHora(pedido.criadoEm)}</small>
              <div className="acoes-linha" style={{ marginTop: 0 }}>
                <span className={`selo ${classeSelo(pedido.statusPagamento)}`}>
                  {ROTULO_PAGAMENTO[pedido.statusPagamento] ?? pedido.statusPagamento}
                </span>
                <span className={`selo ${classeSelo(pedido.estado)}`}>
                  {ROTULO_PEDIDO[pedido.estado] ?? pedido.estado}
                </span>
              </div>
            </div>

            {etapa >= 0 ? (
              <div className="linha-tempo">
                {NOMES_ETAPAS.map((nome, indice) => (
                  <div key={nome} className={indice <= etapa ? "feito" : ""}>
                    {nome}
                  </div>
                ))}
              </div>
            ) : (
              <p className="item-aviso">
                Pedido cancelado em {dataHora(pedido.canceladoEm)} ·{" "}
                {ROTULO_PAGAMENTO[pedido.statusPagamento] ?? pedido.statusPagamento}
              </p>
            )}
          </section>

          <section className="compra-bloco">
            <h2>Produtos</h2>

            {pedido.itens.map((item) => (
              <div className="item-compra" key={item.id}>
                <div className="item-compra-imagem" onClick={() => navigate(`/produto/${item.produtoId}`)}>
                  {item.imagemUrl ? <img src={urlImagem(item.imagemUrl)} alt={item.nomeProduto} /> : <span>◇</span>}
                </div>
                <div className="item-compra-info">
                  <strong onClick={() => navigate(`/produto/${item.produtoId}`)}>{item.nomeProduto}</strong>
                  <small>
                    {item.quantidade} × {moeda(item.precoUnitario)} (preço na data da compra)
                  </small>

                  {souComprador && recebido && (
                    avaliados.includes(item.produtoId) ? (
                      <small style={{ color: "#7fe3a6" }}>✓ Você já avaliou este produto</small>
                    ) : (
                      <AvaliarItem pedidoId={pedido.id} item={item} aoAvaliar={() => void carregar()} />
                    )
                  )}
                </div>
                <div className="item-compra-valor">
                  <strong>{moeda(item.subtotal)}</strong>
                </div>
              </div>
            ))}
          </section>

          <section className="compra-bloco">
            <h2>Entrega</h2>
            <p style={{ color: "#c7bdda", margin: 0, lineHeight: 1.6 }}>
              {pedido.entrega.logradouro}, {pedido.entrega.numero}
              {pedido.entrega.complemento && ` — ${pedido.entrega.complemento}`}
              <br />
              {pedido.entrega.bairro} · {pedido.entrega.cidade}/{pedido.entrega.estado} · CEP {pedido.entrega.cep}
            </p>
          </section>
        </div>

        <aside className="compra-bloco compra-resumo">
          <h2>Pagamento</h2>

          <div className="compra-linha">
            <span>Forma</span>
            <span>{ROTULO_FORMA[pedido.formaPagamento] ?? pedido.formaPagamento}</span>
          </div>
          <div className="compra-linha">
            <span>Pago em</span>
            <span>{dataHora(pedido.pagoEm)}</span>
          </div>
          <div className="compra-linha">
            <span>Itens</span>
            <span>{pedido.quantidadeTotal}</span>
          </div>
          <div className="compra-linha total">
            <span>Total</span>
            <strong>{moeda(pedido.total)}</strong>
          </div>

          <button className="link-texto" style={{ marginTop: 10 }} onClick={() => navigate(`/vendedor/${pedido.vendedorId}`)}>
            Ver loja do vendedor →
          </button>

          {souComprador && pedido.estado === "Pendente" && pedido.pagamentoId && (
            <button className="botao-primario botao-largo" onClick={() => navigate(`/pagamento/${pedido.pagamentoId}`)}>
              Ir para o pagamento
            </button>
          )}

          {souComprador && pedido.estado === "Enviado" && (
            <button
              className="botao-primario botao-largo"
              disabled={ocupado}
              onClick={() => void agir(() => entregarPedido(pedido.id), "Recebimento confirmado!")}
            >
              Confirmar recebimento
            </button>
          )}

          {souVendedor && pedido.estado === "Pago" && (
            <button
              className="botao-primario botao-largo"
              disabled={ocupado}
              onClick={() => void agir(() => enviarPedido(pedido.id), "Pedido marcado como enviado.")}
            >
              Marcar como enviado
            </button>
          )}

          {souVendedor && ["Pago", "Enviado", "Entregue"].includes(pedido.estado) && (
            <button
              className="botao-secundario botao-largo"
              disabled={ocupado}
              onClick={() => void agir(() => concluirPedido(pedido.id), "Pedido concluído.")}
            >
              Concluir pedido
            </button>
          )}

          {(pedido.estado === "Pendente" || pedido.estado === "Pago") && (
            <button
              className="botao-perigo botao-largo"
              disabled={ocupado}
              onClick={() => {
                const texto =
                  pedido.estado === "Pago"
                    ? "Cancelar este pedido? O valor será estornado (simulação) e as unidades voltam ao estoque."
                    : "Cancelar esta compra?";

                if (window.confirm(texto)) {
                  void agir(() => cancelarPedido(pedido.id), "Pedido cancelado.");
                }
              }}
            >
              Cancelar pedido
            </button>
          )}
        </aside>
      </div>
    </main>
  );
}

export default PedidoDetalhePagina;
