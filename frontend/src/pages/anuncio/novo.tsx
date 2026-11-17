import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router-dom";

import { obterCategorias, type Categoria } from "../../services/produtosService";

import "../shared/paginas.css";
import { API_ORIGIN } from "../../config";

interface AnuncioForm {
  nome: string;
  descricao: string;
  preco: string;
  estoque: string;
  categoria: string;
  marca: string;
  colecao: string;
  condicao: string;
  franquia: string;
  raridade: string;
  modalidade: string;
  edicaoLimitada: boolean;
  destaque: boolean;
}

const INICIAL: AnuncioForm = {
  nome: "",
  descricao: "",
  preco: "",
  estoque: "1",
  categoria: "",
  marca: "",
  colecao: "",
  condicao: "Novo",
  franquia: "",
  raridade: "Comum",
  modalidade: "PrecoFixo",
  edicaoLimitada: false,
  destaque: false,
};

const campo = {
  width: "100%",
  padding: 10,
  marginTop: 6,
  borderRadius: 8,
  border: "1px solid rgba(255,255,255,.15)",
  background: "rgba(255,255,255,.04)",
  color: "inherit",
} as const;

function NovoAnuncioPagina() {
  const navigate = useNavigate();

  const [form, setForm] = useState<AnuncioForm>(INICIAL);
  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState("");

  useEffect(() => {
    obterCategorias()
      .then((lista) => {
        setCategorias(lista);

        if (lista.length > 0) {
          setForm((atual) =>
            atual.categoria ? atual : { ...atual, categoria: lista[0].nome },
          );
        }
      })
      .catch(() => toast.error("Não foi possível carregar as categorias."));
  }, []);

  function alterar<C extends keyof AnuncioForm>(chave: C, valor: AnuncioForm[C]) {
    setForm((atual) => ({ ...atual, [chave]: valor }));
  }

  async function publicar() {
    setErro("");

    const preco = Number(form.preco.replace(",", "."));
    const estoque = Number(form.estoque);

    if (!form.nome.trim()) {
      setErro("Informe o nome do produto.");
      return;
    }

    if (!Number.isFinite(preco) || preco <= 0) {
      setErro("Informe um preço válido.");
      return;
    }

    if (!Number.isInteger(estoque) || estoque <= 0) {
      setErro("Informe uma quantidade em estoque válida.");
      return;
    }

    setEnviando(true);

    try {
      const resposta = await fetch(`${API_ORIGIN}/api/produtos`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          nome: form.nome,
          descricao: form.descricao,
          preco,
          estoque,
          categoria: form.categoria,
          marca: form.marca,
          colecao: form.colecao,
          condicao: form.condicao,
          imagemUrl: "",
          modalidade: form.modalidade,
          franquia: form.franquia,
          raridade: form.raridade,
          edicaoLimitada: form.edicaoLimitada,
          destaque: form.destaque,
        }),
      });

      if (!resposta.ok) {
        const problema = await resposta.json().catch(() => null);

        setErro(problema?.detail ?? "Não foi possível publicar o anúncio.");
        return;
      }

      const criado = await resposta.json();

      toast.success("Anúncio publicado!");
      navigate(`/produto/${criado.id}`);
    } catch {
      setErro("Não foi possível publicar o anúncio.");
    } finally {
      setEnviando(false);
    }
  }

  return (
    <main className="pagina">
      <div className="pagina-topo">
        <button onClick={() => navigate("/home")}>← Catálogo</button>
        <h1>Novo anúncio</h1>
      </div>

      <div className="avaliacoes" style={{ maxWidth: 720 }}>
        <div className="notificacao">
          <div style={{ width: "100%", display: "grid", gap: 12 }}>
            <label>
              Nome do produto
              <input value={form.nome} onChange={(e) => alterar("nome", e.target.value)} placeholder="Funko Pop Batman #01" style={campo} />
            </label>

            <label>
              Descrição
              <textarea rows={4} value={form.descricao} onChange={(e) => alterar("descricao", e.target.value)} placeholder="Estado, detalhes, o que acompanha..." style={campo} />
            </label>

            <label>
              Preço (R$)
              <input value={form.preco} onChange={(e) => alterar("preco", e.target.value)} placeholder="249,90" style={campo} />
            </label>

            <label>
              Estoque
              <input value={form.estoque} onChange={(e) => alterar("estoque", e.target.value)} style={campo} />
            </label>

            <label>
              Categoria
              <select value={form.categoria} onChange={(e) => alterar("categoria", e.target.value)} style={campo}>
                {categorias.map((categoria) => (
                  <option key={categoria.id} value={categoria.nome}>{categoria.nome}</option>
                ))}
              </select>
            </label>

            <label>
              Franquia
              <input value={form.franquia} onChange={(e) => alterar("franquia", e.target.value)} placeholder="DC, Marvel, Naruto..." style={campo} />
            </label>

            <label>
              Marca
              <input value={form.marca} onChange={(e) => alterar("marca", e.target.value)} style={campo} />
            </label>

            <label>
              Coleção
              <input value={form.colecao} onChange={(e) => alterar("colecao", e.target.value)} style={campo} />
            </label>

            <label>
              Condição
              <select value={form.condicao} onChange={(e) => alterar("condicao", e.target.value)} style={campo}>
                <option value="Novo">Novo</option>
                <option value="Seminovo">Seminovo</option>
                <option value="Usado">Usado</option>
              </select>
            </label>

            <label>
              Raridade
              <select value={form.raridade} onChange={(e) => alterar("raridade", e.target.value)} style={campo}>
                <option value="Comum">Comum</option>
                <option value="Raro">Raro</option>
                <option value="Epico">Épico</option>
                <option value="Lendario">Lendário</option>
              </select>
            </label>

            <label>
              Forma de negociação
              <select value={form.modalidade} onChange={(e) => alterar("modalidade", e.target.value)} style={campo}>
                <option value="PrecoFixo">Preço fixo</option>
                <option value="AceitaOferta">Aceita ofertas</option>
                <option value="Liquidacao">Liquidação</option>
              </select>
            </label>

            <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
              <input type="checkbox" checked={form.edicaoLimitada} onChange={(e) => alterar("edicaoLimitada", e.target.checked)} />
              Edição limitada
            </label>

            <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
              <input type="checkbox" checked={form.destaque} onChange={(e) => alterar("destaque", e.target.checked)} />
              Destacar na vitrine
            </label>

            {erro && <p style={{ color: "#ff8b8b" }} role="alert">{erro}</p>}

            <button onClick={publicar} disabled={enviando}>
              {enviando ? "Publicando..." : "Publicar anúncio"}
            </button>
          </div>
        </div>
      </div>
    </main>
  );
}

export default NovoAnuncioPagina;
