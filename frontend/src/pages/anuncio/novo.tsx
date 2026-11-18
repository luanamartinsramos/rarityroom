import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router-dom";

import { obterCategorias, type Categoria } from "../../services/produtosService";

import "../shared/paginas.css";

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
      const resposta = await fetch("http://localhost:5000/api/produtos", {
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

      <form
        className="form-cartao"
        onSubmit={(evento) => {
          evento.preventDefault();
          void publicar();
        }}
      >
        <div className="grade-form">
          <label className="campo col-cheia">
            Nome do produto
            <input value={form.nome} onChange={(e) => alterar("nome", e.target.value)} placeholder="Funko Pop Batman #01" />
          </label>

          <label className="campo col-cheia">
            Descrição
            <textarea rows={4} value={form.descricao} onChange={(e) => alterar("descricao", e.target.value)} placeholder="Estado, detalhes, o que acompanha..." />
          </label>

          <label className="campo col-2">
            Preço (R$)
            <input inputMode="decimal" value={form.preco} onChange={(e) => alterar("preco", e.target.value)} placeholder="249,90" />
          </label>

          <label className="campo col-2">
            Estoque
            <input type="number" min={1} value={form.estoque} onChange={(e) => alterar("estoque", e.target.value)} />
          </label>

          <label className="campo col-2">
            Categoria
            <select value={form.categoria} onChange={(e) => alterar("categoria", e.target.value)}>
              {categorias.map((categoria) => (
                <option key={categoria.id} value={categoria.nome}>{categoria.nome}</option>
              ))}
            </select>
          </label>

          <label className="campo col-2">
            Franquia
            <input value={form.franquia} onChange={(e) => alterar("franquia", e.target.value)} placeholder="DC, Marvel, Naruto..." />
          </label>

          <label className="campo col-2">
            Marca
            <input value={form.marca} onChange={(e) => alterar("marca", e.target.value)} placeholder="Bandai, Hasbro..." />
          </label>

          <label className="campo col-2">
            Coleção
            <input value={form.colecao} onChange={(e) => alterar("colecao", e.target.value)} placeholder="Ex.: 30 anos" />
          </label>

          <label className="campo col-2">
            Condição
            <select value={form.condicao} onChange={(e) => alterar("condicao", e.target.value)}>
              <option value="Novo">Novo</option>
              <option value="Seminovo">Seminovo</option>
              <option value="Usado">Usado</option>
            </select>
          </label>

          <label className="campo col-2">
            Raridade
            <select value={form.raridade} onChange={(e) => alterar("raridade", e.target.value)}>
              <option value="Comum">Comum</option>
              <option value="Raro">Raro</option>
              <option value="Epico">Épico</option>
              <option value="Lendario">Lendário</option>
            </select>
          </label>

          <label className="campo col-2">
            Forma de negociação
            <select value={form.modalidade} onChange={(e) => alterar("modalidade", e.target.value)}>
              <option value="PrecoFixo">Preço fixo</option>
              <option value="AceitaOferta">Aceita ofertas</option>
              <option value="Liquidacao">Liquidação</option>
            </select>
          </label>

          <label className="campo-check">
            <input type="checkbox" checked={form.edicaoLimitada} onChange={(e) => alterar("edicaoLimitada", e.target.checked)} />
            Edição limitada
          </label>

          <label className="campo-check">
            <input type="checkbox" checked={form.destaque} onChange={(e) => alterar("destaque", e.target.checked)} />
            Destacar na vitrine
          </label>

          {erro && <p className="form-erro col-cheia" role="alert">{erro}</p>}
        </div>

        <div className="form-rodape">
          <button type="button" className="botao" onClick={() => navigate("/home")}>
            Cancelar
          </button>

          <button type="submit" className="botao-primario" disabled={enviando}>
            {enviando ? "Publicando..." : "Publicar anúncio"}
          </button>
        </div>
      </form>
    </main>
  );
}

export default NovoAnuncioPagina;
