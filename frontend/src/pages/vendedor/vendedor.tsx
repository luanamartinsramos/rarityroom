import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import { obterVendedor, type VendedorApi } from "../../services/catalogoService";

import { urlImagem } from "../../utils/formato";
import "../shared/paginas.css";

function VendedorPagina() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [vendedor, setVendedor] = useState<VendedorApi | null>(null);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");

  useEffect(() => {
    const vendedorId = Number(id);

    if (!Number.isFinite(vendedorId)) {
      setErro("Vendedor inválido.");
      setCarregando(false);
      return;
    }

    obterVendedor(vendedorId)
      .then(setVendedor)
      .catch(() => setErro("Não foi possível carregar este vendedor."))
      .finally(() => setCarregando(false));
  }, [id]);

  if (carregando) {
    return (
      <main className="pagina">
        <div className="pagina-carregando">Carregando vendedor...</div>
      </main>
    );
  }

  if (erro || !vendedor) {
    return (
      <main className="pagina">
        <div className="pagina-vazio">
          <h3>{erro || "Vendedor não encontrado"}</h3>

          <button onClick={() => navigate("/home")}>Voltar</button>
        </div>
      </main>
    );
  }

  const desde = new Date(vendedor.desde).toLocaleDateString("pt-BR");

  const ativos = vendedor.anuncios.filter(
    (anuncio) => anuncio.estado === "Disponivel",
  );

  return (
    <main className="pagina">
      <div className="pagina-topo">
        <button onClick={() => navigate("/home")}>← Catálogo</button>

        <h1>{vendedor.nome}</h1>
      </div>

      {vendedor.loja && (
        <div className="vendedor-card" style={{ marginTop: 0, marginBottom: 20 }}>
          <div className="vendedor-avatar">
            {vendedor.loja.fotoUrl ? (
              <img
                src={urlImagem(vendedor.loja.fotoUrl)}
                alt={vendedor.loja.nome}
                style={{ width: "100%", height: "100%", borderRadius: "50%", objectFit: "cover" }}
              />
            ) : (
              vendedor.loja.nome.charAt(0).toUpperCase()
            )}
          </div>

          <div>
            <strong>{vendedor.loja.nome}</strong>

            <div>{vendedor.loja.descricao || "Loja sem descrição."}</div>
          </div>
        </div>
      )}

      <div className="produto-tags">
        <span className="produto-tag">Na plataforma desde {desde}</span>
        <span className="produto-tag">
          {vendedor.totalAvaliacoes === 0
            ? "Sem avaliações"
            : `${vendedor.reputacaoMedia} ★ em ${vendedor.totalAvaliacoes} avaliação(ões)`}
        </span>
        <span className="produto-tag">
          {vendedor.anunciosAtivos} anúncio(s) ativo(s)
        </span>
        <span className="produto-tag">
          {vendedor.produtosVendidos} vendido(s)
        </span>
        <span className="produto-tag">Perfil: {vendedor.tipoConta}</span>
      </div>

      <h2 style={{ marginTop: 32 }}>Anúncios ativos</h2>

      {ativos.length === 0 ? (
        <div className="pagina-vazio">
          <span>◇</span>
          <p>Este vendedor não tem anúncios ativos no momento.</p>
        </div>
      ) : (
        <div className="grade-produtos">
          {ativos.map((anuncio) => (
            <article
              className="cartao-produto"
              key={anuncio.id}
              onClick={() => navigate(`/produto/${anuncio.id}`)}
            >
              <div className="cartao-produto-imagem">
                {anuncio.imagemUrl ? (
                  <img src={urlImagem(anuncio.imagemUrl)} alt={anuncio.nome} />
                ) : (
                  <span>◇</span>
                )}
              </div>

              <div className="cartao-produto-info">
                <strong>{anuncio.nome}</strong>
                <span>R$ {anuncio.preco.toFixed(2).replace(".", ",")}</span>
              </div>
            </article>
          ))}
        </div>
      )}

      <h2 style={{ marginTop: 32 }}>Avaliações recebidas</h2>

      {vendedor.avaliacoes.length === 0 ? (
        <div className="pagina-vazio">
          <p>Nenhuma avaliação ainda.</p>
        </div>
      ) : (
        <div className="avaliacoes">
          {vendedor.avaliacoes.map((avaliacao) => (
            <div className="avaliacao" key={avaliacao.id}>
              <strong>{"★".repeat(avaliacao.nota)}</strong>

              <p>{avaliacao.comentario || "Sem comentário."}</p>

              <small>
                {new Date(avaliacao.criadaEm).toLocaleDateString("pt-BR")}
              </small>
            </div>
          ))}
        </div>
      )}
    </main>
  );
}

export default VendedorPagina;
