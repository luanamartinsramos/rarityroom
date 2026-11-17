import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router-dom";

import {
  obterPedidos,
  ROTULO_FORMA,
  ROTULO_PAGAMENTO,
  ROTULO_PEDIDO,
  type PedidoApi,
} from "../../services/pedidosService";
import { dataHora, moeda, urlImagem } from "../../utils/formato";
import { classeSelo, obterMeuId } from "./rotulos";

import "../shared/paginas.css";
import "../shared/compra.css";

/** Histórico de compras do usuário logado (somente os pedidos dele). */
function PedidosPagina() {
  const navigate = useNavigate();

  const [pedidos, setPedidos] = useState<PedidoApi[]>([]);
  const [carregando, setCarregando] = useState(true);

  useEffect(() => {
    Promise.all([obterMeuId(), obterPedidos()])
      .then(([meuId, lista]) => setPedidos(lista.filter((pedido) => pedido.compradorId === meuId)))
      .catch((falha: Error) => toast.error(falha.message))
      .finally(() => setCarregando(false));
  }, []);

  if (carregando) {
    return (
      <main className="pagina">
        <div className="pagina-carregando">Carregando suas compras...</div>
      </main>
    );
  }

  return (
    <main className="pagina">
      <div className="pagina-topo">
        <button onClick={() => navigate("/home")}>← Catálogo</button>
        <h1>Minhas compras</h1>
      </div>

      {pedidos.length === 0 ? (
        <div className="pagina-vazio">
          <span style={{ fontSize: 40 }}>📦</span>
          <h3>Você ainda não fez nenhuma compra</h3>
          <button className="botao-primario" onClick={() => navigate("/home")}>
            Explorar produtos
          </button>
        </div>
      ) : (
        <div style={{ maxWidth: 900 }}>
          {pedidos.map((pedido) => (
            <article className="pedido-cartao" key={pedido.id}>
              <div className="pedido-cabecalho">
                <div>
                  <strong>Pedido {pedido.codigo}</strong>
                  <br />
                  <small>
                    {dataHora(pedido.criadoEm)} · {ROTULO_FORMA[pedido.formaPagamento] ?? pedido.formaPagamento}
                  </small>
                </div>

                <div className="acoes-linha" style={{ marginTop: 0 }}>
                  <span className={`selo ${classeSelo(pedido.statusPagamento)}`}>
                    {ROTULO_PAGAMENTO[pedido.statusPagamento] ?? pedido.statusPagamento}
                  </span>
                  <span className={`selo ${classeSelo(pedido.estado)}`}>
                    {ROTULO_PEDIDO[pedido.estado] ?? pedido.estado}
                  </span>
                </div>
              </div>

              {pedido.itens.map((item) => (
                <div className="item-compra" key={item.id}>
                  <div className="item-compra-imagem" onClick={() => navigate(`/produto/${item.produtoId}`)}>
                    {item.imagemUrl ? <img src={urlImagem(item.imagemUrl)} alt={item.nomeProduto} /> : <span>◇</span>}
                  </div>
                  <div className="item-compra-info">
                    <strong>{item.nomeProduto}</strong>
                    <small>
                      {item.quantidade} × {moeda(item.precoUnitario)}
                    </small>
                  </div>
                  <div className="item-compra-valor">
                    <strong>{moeda(item.subtotal)}</strong>
                  </div>
                </div>
              ))}

              <div className="pedido-cabecalho" style={{ marginTop: 8 }}>
                <span>
                  Total: <strong style={{ color: "#d9aa32" }}>{moeda(pedido.total)}</strong>
                </span>

                <div className="acoes-linha" style={{ marginTop: 0 }}>
                  {pedido.estado === "Pendente" && pedido.pagamentoId && (
                    <button className="botao-primario" onClick={() => navigate(`/pagamento/${pedido.pagamentoId}`)}>
                      Pagar agora
                    </button>
                  )}
                  <button className="botao-secundario" onClick={() => navigate(`/pedidos/${pedido.id}`)}>
                    Ver detalhes
                  </button>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}
    </main>
  );
}

export default PedidosPagina;
