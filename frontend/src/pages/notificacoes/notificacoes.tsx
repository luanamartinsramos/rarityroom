import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router-dom";

import {
  marcarComoLida,
  marcarTodasComoLidas,
  obterNotificacoes,
  type NotificacaoApi,
} from "../../services/notificacoesService";

import "../shared/paginas.css";

function NotificacoesPagina() {
  const navigate = useNavigate();

  const [itens, setItens] = useState<NotificacaoApi[]>([]);
  const [carregando, setCarregando] = useState(true);

  async function carregar() {
    try {
      const central = await obterNotificacoes();
      setItens(central.itens);
    } catch {
      toast.error("Não foi possível carregar as notificações.");
    } finally {
      setCarregando(false);
    }
  }

  useEffect(() => {
    // carregar() só altera estado depois do await, então não há render em cascata.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void carregar();
  }, []);

  async function ler(id: number) {
    await marcarComoLida(id);
    await carregar();
  }

  async function lerTodas() {
    await marcarTodasComoLidas();

    toast.success("Todas as notificações foram marcadas como lidas.");

    await carregar();
  }

  return (
    <main className="pagina">
      <div className="pagina-topo">
        <button onClick={() => navigate("/home")}>← Catálogo</button>

        <h1>Notificações</h1>

        {itens.some((item) => !item.lida) && (
          <button onClick={lerTodas}>Marcar todas como lidas</button>
        )}
      </div>

      {carregando ? (
        <div className="pagina-carregando">Carregando notificações...</div>
      ) : itens.length === 0 ? (
        <div className="pagina-vazio">
          <span>◇</span>
          <h3>Nenhuma notificação</h3>
          <p>Quando alguém negociar com você, o aviso aparece aqui.</p>
        </div>
      ) : (
        <div className="lista-notificacoes">
          {itens.map((item) => (
            <div
              className={`notificacao${item.lida ? "" : " nao-lida"}`}
              key={item.id}
            >
              <div>
                <strong>{item.tipo}</strong>

                <p>{item.mensagem}</p>

                <small>
                  {new Date(item.criadaEm).toLocaleString("pt-BR")}
                </small>
              </div>

              {!item.lida && (
                <button onClick={() => ler(item.id)}>Marcar como lida</button>
              )}
            </div>
          ))}
        </div>
      )}
    </main>
  );
}

export default NotificacoesPagina;
