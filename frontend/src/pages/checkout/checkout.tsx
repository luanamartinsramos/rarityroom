import { useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router-dom";

import { carrinhoVazio, obterCarrinho, type Carrinho } from "../../services/carrinhoService";
import { obterEnderecos, type EnderecoApi } from "../../services/enderecosService";
import {
  finalizarCompra,
  ROTULO_FORMA,
  type EntregaApi,
  type FormaPagamento,
} from "../../services/pedidosService";
import { moeda, urlImagem } from "../../utils/formato";

import "../shared/paginas.css";
import "../shared/compra.css";

const enderecoVazio: EntregaApi = {
  cep: "",
  logradouro: "",
  numero: "",
  complemento: "",
  bairro: "",
  cidade: "",
  estado: "",
};

const FORMAS: { valor: FormaPagamento; icone: string; descricao: string }[] = [
  { valor: "Pix", icone: "⚡", descricao: "QR Code e copia e cola · aprovação na hora" },
  { valor: "Boleto", icone: "🧾", descricao: "Vence em 3 dias · confirmação simulada" },
  { valor: "CartaoCredito", icone: "💳", descricao: "Em até 12x sem juros" },
];

function mascararCep(valor: string): string {
  const digitos = valor.replace(/\D/g, "").slice(0, 8);

  return digitos.length > 5 ? `${digitos.slice(0, 5)}-${digitos.slice(5)}` : digitos;
}

function validarEndereco(endereco: EntregaApi): Record<string, string> {
  const erros: Record<string, string> = {};

  if (endereco.cep.replace(/\D/g, "").length !== 8) erros.cep = "CEP deve ter 8 dígitos.";
  if (endereco.logradouro.trim().length < 3) erros.logradouro = "Informe a rua.";
  if (!endereco.numero.trim()) erros.numero = "Informe o número.";
  if (endereco.bairro.trim().length < 2) erros.bairro = "Informe o bairro.";
  if (endereco.cidade.trim().length < 2) erros.cidade = "Informe a cidade.";
  if (!/^[A-Za-z]{2}$/.test(endereco.estado.trim())) erros.estado = "UF com 2 letras.";

  return erros;
}

function CheckoutPagina() {
  const navigate = useNavigate();

  const [carrinho, setCarrinho] = useState<Carrinho>(carrinhoVazio);
  const [enderecos, setEnderecos] = useState<EnderecoApi[]>([]);
  const [endereco, setEndereco] = useState<EntregaApi>(enderecoVazio);
  const [forma, setForma] = useState<FormaPagamento | null>(null);
  const [erros, setErros] = useState<Record<string, string>>({});
  const [carregando, setCarregando] = useState(true);
  const [enviando, setEnviando] = useState(false);

  // Uma chave por tentativa: reenvios (duplo clique, rede lenta) não duplicam a compra.
  const chaveIdempotencia = useMemo(
    () => crypto.randomUUID?.() ?? `${Date.now()}-${Math.random()}`,
    [],
  );

  useEffect(() => {
    Promise.all([obterCarrinho(), obterEnderecos().catch(() => [])])
      .then(([dadosCarrinho, lista]) => {
        setCarrinho(dadosCarrinho);
        setEnderecos(lista);

        const principal = lista.find((item) => item.principal) ?? lista[0];

        if (principal) {
          setEndereco(paraEntrega(principal));
        }
      })
      .catch((falha: Error) => toast.error(falha.message))
      .finally(() => setCarregando(false));
  }, []);

  function paraEntrega(item: EnderecoApi): EntregaApi {
    return {
      cep: item.cep,
      logradouro: item.logradouro,
      numero: item.numero,
      complemento: item.complemento ?? "",
      bairro: item.bairro,
      cidade: item.cidade,
      estado: item.estado,
    };
  }

  function atualizar(campo: keyof EntregaApi, valor: string) {
    setEndereco((atual) => ({
      ...atual,
      [campo]: campo === "cep" ? mascararCep(valor) : campo === "estado" ? valor.toUpperCase().slice(0, 2) : valor,
    }));
    setErros((atual) => ({ ...atual, [campo]: "" }));
  }

  async function continuar() {
    const encontrados = validarEndereco(endereco);
    setErros(encontrados);

    if (Object.values(encontrados).some(Boolean)) {
      toast.error("Confira o endereço de entrega.");
      return;
    }

    if (!forma) {
      toast.error("Escolha a forma de pagamento.");
      return;
    }

    setEnviando(true);

    try {
      // O servidor revalida itens, preços e estoque; nada disso é enviado daqui.
      const compra = await finalizarCompra({
        ...endereco,
        formaPagamento: forma,
        chaveIdempotencia,
      });

      navigate(`/pagamento/${compra.id}`, { replace: true });
    } catch (falha) {
      toast.error(falha instanceof Error ? falha.message : "Não foi possível finalizar a compra.", {
        duration: 7000,
      });

      setCarrinho(await obterCarrinho().catch(() => carrinho));
    } finally {
      setEnviando(false);
    }
  }

  if (carregando) {
    return (
      <main className="pagina">
        <div className="pagina-carregando">Preparando seu checkout...</div>
      </main>
    );
  }

  if (carrinho.itens.length === 0) {
    return (
      <main className="pagina">
        <div className="pagina-vazio">
          <h3>Seu carrinho está vazio</h3>
          <button className="botao-primario" onClick={() => navigate("/home")}>
            Explorar produtos
          </button>
        </div>
      </main>
    );
  }

  const vendedores = new Set(carrinho.itens.map((item) => item.vendedorId)).size;

  const campo = (nome: keyof EntregaApi, rotulo: string, extra: Partial<React.InputHTMLAttributes<HTMLInputElement>> = {}, largo = false) => (
    <label className={`${largo ? "largo " : ""}${erros[nome] ? "campo-erro" : ""}`}>
      {rotulo}
      <input value={endereco[nome]} onChange={(evento) => atualizar(nome, evento.target.value)} {...extra} />
      {erros[nome] && <span className="erro-campo">{erros[nome]}</span>}
    </label>
  );

  return (
    <main className="pagina">
      <div className="pagina-topo">
        <button onClick={() => navigate("/carrinho")}>← Voltar ao carrinho</button>
        <h1>Finalizar compra</h1>
      </div>

      <div className="compra-layout">
        <div>
          <section className="compra-bloco">
            <h2>
              <span className="compra-passo">1</span> Revise seus produtos
            </h2>

            {carrinho.itens.map((item) => (
              <div className="item-compra" key={item.produtoId}>
                <div className="item-compra-imagem">
                  {item.imagemUrl ? <img src={urlImagem(item.imagemUrl)} alt={item.nome} /> : <span>◇</span>}
                </div>
                <div className="item-compra-info">
                  <strong>{item.nome}</strong>
                  <small>Vendido por {item.vendedorNome}</small>
                  <small>
                    {item.quantidade} × {moeda(item.precoUnitario)}
                  </small>
                  {item.aviso && <span className="item-aviso">⚠ {item.aviso}</span>}
                </div>
                <div className="item-compra-valor">
                  <strong>{moeda(item.subtotal)}</strong>
                </div>
              </div>
            ))}

            {!carrinho.podeFinalizar && (
              <div className="acoes-linha">
                <span className="item-aviso">Há itens com problema de estoque.</span>
                <button className="botao-secundario" onClick={() => navigate("/carrinho")}>
                  Corrigir no carrinho
                </button>
              </div>
            )}
          </section>

          <section className="compra-bloco">
            <h2>
              <span className="compra-passo">2</span> Endereço de entrega
            </h2>

            {enderecos.length > 0 && (
              <label className="campo" style={{ marginBottom: 12 }}>
                Endereços salvos
                <select
                  defaultValue=""
                  onChange={(evento) => {
                    const escolhido = enderecos.find((item) => item.id === Number(evento.target.value));
                    if (escolhido) {
                      setEndereco(paraEntrega(escolhido));
                      setErros({});
                    }
                  }}
                >
                  <option value="" disabled>
                    Escolha um endereço salvo...
                  </option>
                  {enderecos.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.logradouro}, {item.numero} — {item.cidade}/{item.estado}
                      {item.principal ? " (principal)" : ""}
                    </option>
                  ))}
                </select>
              </label>
            )}

            <div className="form-grade">
              {campo("cep", "CEP", { placeholder: "00000-000", inputMode: "numeric" })}
              {campo("numero", "Número")}
              {campo("logradouro", "Rua / Avenida", {}, true)}
              {campo("complemento", "Complemento (opcional)", {}, true)}
              {campo("bairro", "Bairro")}
              {campo("cidade", "Cidade")}
              {campo("estado", "UF", { placeholder: "SP", maxLength: 2 })}
            </div>
          </section>

          <section className="compra-bloco">
            <h2>
              <span className="compra-passo">3</span> Forma de pagamento
            </h2>

            <div className="aviso-simulacao">
              Ambiente acadêmico: os pagamentos são <strong>simulados</strong> dentro do RarityRoom.
              Nenhum valor é cobrado e nenhum dado é enviado a bancos ou gateways.
            </div>

            <div className="opcoes-pagamento">
              {FORMAS.map((opcao) => (
                <button
                  key={opcao.valor}
                  type="button"
                  className={`opcao-pagamento ${forma === opcao.valor ? "ativa" : ""}`}
                  onClick={() => setForma(opcao.valor)}
                >
                  <span>{opcao.icone}</span>
                  <strong>{ROTULO_FORMA[opcao.valor]}</strong>
                  <small>{opcao.descricao}</small>
                </button>
              ))}
            </div>
          </section>
        </div>

        <aside className="compra-bloco compra-resumo">
          <h2>
            <span className="compra-passo">4</span> Resumo da compra
          </h2>

          <div className="compra-linha">
            <span>Produtos ({carrinho.quantidadeItens})</span>
            <span>{moeda(carrinho.total)}</span>
          </div>
          <div className="compra-linha">
            <span>Vendedores</span>
            <span>
              {vendedores} {vendedores > 1 ? "(um pedido por vendedor)" : ""}
            </span>
          </div>
          <div className="compra-linha">
            <span>Pagamento</span>
            <span>{forma ? ROTULO_FORMA[forma] : "—"}</span>
          </div>
          <div className="compra-linha">
            <span>Entrega</span>
            <span>
              {endereco.cidade && endereco.estado ? `${endereco.cidade}/${endereco.estado}` : "—"}
            </span>
          </div>
          <div className="compra-linha total">
            <span>Total</span>
            <strong>{moeda(carrinho.total)}</strong>
          </div>

          <button
            className="botao-primario botao-largo"
            disabled={enviando || !carrinho.podeFinalizar}
            onClick={() => void continuar()}
          >
            {enviando ? "Validando estoque..." : "Continuar para o pagamento →"}
          </button>

          <p style={{ color: "#9b8fb0", fontSize: 12, marginTop: 10 }}>
            O estoque só é reservado definitivamente quando o pagamento for aprovado. Seu carrinho
            continua salvo até lá.
          </p>
        </aside>
      </div>
    </main>
  );
}

export default CheckoutPagina;
