import QRCode from "qrcode";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate, useParams } from "react-router-dom";

import {
  cancelarCompra,
  obterCompra,
  pagarComCartao,
  ROTULO_FORMA,
  ROTULO_PAGAMENTO,
  simularConfirmacao,
  simularExpiracao,
  type CartaoPayload,
  type CompraApi,
} from "../../services/pedidosService";
import { obterCarrinho } from "../../services/carrinhoService";
import { data, dataHora, moeda } from "../../utils/formato";

import "../shared/paginas.css";
import "../shared/compra.css";

// ---------------------------------------------------------------------------
// Apoio
// ---------------------------------------------------------------------------

async function copiar(texto: string, rotulo: string) {
  try {
    await navigator.clipboard.writeText(texto);
    toast.success(`${rotulo} copiado!`);
  } catch {
    toast.error("Não foi possível copiar. Selecione o texto e copie manualmente.");
  }
}

function digitos(valor: string): string {
  return valor.replace(/\D/g, "");
}

function luhn(numero: string): boolean {
  let soma = 0;
  let dobrar = false;

  for (let i = numero.length - 1; i >= 0; i--) {
    let valor = Number(numero[i]);

    if (dobrar) {
      valor *= 2;
      if (valor > 9) valor -= 9;
    }

    soma += valor;
    dobrar = !dobrar;
  }

  return soma % 10 === 0;
}

function cpfValido(cpf: string): boolean {
  if (cpf.length !== 11 || /^(\d)\1+$/.test(cpf)) return false;

  const digito = (tamanho: number) => {
    let soma = 0;
    for (let i = 0; i < tamanho; i++) soma += Number(cpf[i]) * (tamanho + 1 - i);
    const resto = soma % 11;
    return resto < 2 ? 0 : 11 - resto;
  };

  return digito(9) === Number(cpf[9]) && digito(10) === Number(cpf[10]);
}

function bandeira(numero: string): string {
  if (/^3[47]/.test(numero)) return "Amex";
  if (/^(4011|4389|4514|4576|5041|5066|5067|6362|6363|6504|6505|6516|6550)/.test(numero)) return "Elo";
  if (/^6062/.test(numero)) return "Hipercard";
  if (/^4/.test(numero)) return "Visa";
  if (/^(5[1-5]|2(2[2-9]|[3-6]\d|7[01]|720))/.test(numero)) return "Mastercard";
  return "";
}

/** Código de barras ILUSTRATIVO (Interleaved 2 of 5) desenhado a partir dos dígitos. */
function CodigoBarras({ codigo }: { codigo: string }) {
  const padroes = ["nnwwn", "wnnnw", "nwnnw", "wwnnn", "nnwnw", "wnwnn", "nwwnn", "nnnww", "wnnwn", "nwnwn"];
  const texto = codigo.length % 2 === 0 ? codigo : codigo.slice(0, -1);
  const larguras: number[] = [1, 1, 1, 1];

  for (let i = 0; i < texto.length; i += 2) {
    const barras = padroes[Number(texto[i])];
    const espacos = padroes[Number(texto[i + 1])];

    for (let j = 0; j < 5; j++) {
      larguras.push(barras[j] === "w" ? 3 : 1, espacos[j] === "w" ? 3 : 1);
    }
  }

  larguras.push(3, 1, 1);

  let x = 0;
  const retangulos = larguras.map((largura, indice) => {
    const atual = x;
    x += largura;
    return indice % 2 === 0 ? <rect key={indice} x={atual} y={0} width={largura} height={50} /> : null;
  });

  return (
    <svg className="boleto-barras" viewBox={`0 0 ${x} 50`} preserveAspectRatio="none" aria-label="Código de barras ilustrativo">
      <g fill="#111">{retangulos}</g>
    </svg>
  );
}

function Contagem({ ate, aoTerminar }: { ate: string; aoTerminar: () => void }) {
  const alvo = new Date(/Z|[+-]\d\d:\d\d$/.test(ate) ? ate : `${ate}Z`).getTime();
  const [agora, setAgora] = useState(() => Date.now());

  useEffect(() => {
    const intervalo = window.setInterval(() => setAgora(Date.now()), 1000);
    return () => window.clearInterval(intervalo);
  }, []);

  const restante = Math.max(0, alvo - agora);

  useEffect(() => {
    if (restante === 0) aoTerminar();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [restante === 0]);

  const minutos = Math.floor(restante / 60000);
  const segundos = Math.floor((restante % 60000) / 1000);

  return (
    <span>
      {String(minutos).padStart(2, "0")}:{String(segundos).padStart(2, "0")}
    </span>
  );
}

// ---------------------------------------------------------------------------
// Formulário do cartão
// ---------------------------------------------------------------------------

const cartaoVazio: CartaoPayload = {
  nomeImpresso: "",
  numero: "",
  validade: "",
  cvv: "",
  cpf: "",
  parcelas: 1,
};

function FormularioCartao({
  compra,
  aoPagar,
}: {
  compra: CompraApi;
  aoPagar: (cartao: CartaoPayload) => Promise<void>;
}) {
  const [cartao, setCartao] = useState<CartaoPayload>(cartaoVazio);
  const [erros, setErros] = useState<Record<string, string>>({});
  const [enviando, setEnviando] = useState(false);

  const numero = digitos(cartao.numero);
  const marca = bandeira(numero);
  const tamanhoCvv = marca === "Amex" ? 4 : 3;

  function mascarar(campo: keyof CartaoPayload, valor: string) {
    let formatado: string | number = valor;

    if (campo === "numero") {
      formatado = digitos(valor).slice(0, 19).replace(/(\d{4})(?=\d)/g, "$1 ");
    } else if (campo === "validade") {
      const d = digitos(valor).slice(0, 4);
      formatado = d.length > 2 ? `${d.slice(0, 2)}/${d.slice(2)}` : d;
    } else if (campo === "cvv") {
      formatado = digitos(valor).slice(0, 4);
    } else if (campo === "cpf") {
      const d = digitos(valor).slice(0, 11);
      formatado = d
        .replace(/(\d{3})(\d)/, "$1.$2")
        .replace(/(\d{3})(\d)/, "$1.$2")
        .replace(/(\d{3})(\d{1,2})$/, "$1-$2");
    } else if (campo === "nomeImpresso") {
      formatado = valor.toUpperCase();
    } else if (campo === "parcelas") {
      formatado = Number(valor);
    }

    setCartao((atual) => ({ ...atual, [campo]: formatado }));
    setErros((atual) => ({ ...atual, [campo]: "" }));
  }

  function validar(): Record<string, string> {
    const encontrados: Record<string, string> = {};

    if (cartao.nomeImpresso.trim().length < 3) encontrados.nomeImpresso = "Informe o nome como está no cartão.";
    if (numero.length < 13 || !luhn(numero)) encontrados.numero = "Número do cartão inválido.";

    const [mes, ano] = cartao.validade.split("/").map(Number);
    if (!mes || !ano || mes < 1 || mes > 12 || cartao.validade.length !== 5) {
      encontrados.validade = "Use MM/AA.";
    } else {
      const fim = new Date(2000 + ano, mes, 1);
      if (fim <= new Date()) encontrados.validade = "Cartão vencido.";
    }

    if (cartao.cvv.length !== tamanhoCvv) encontrados.cvv = `CVV com ${tamanhoCvv} dígitos.`;

    const cpf = digitos(cartao.cpf);
    if (cpf.length > 0 && !cpfValido(cpf)) encontrados.cpf = "CPF inválido.";

    return encontrados;
  }

  async function enviar(evento: React.FormEvent) {
    evento.preventDefault();

    const encontrados = validar();
    setErros(encontrados);

    if (Object.values(encontrados).some(Boolean)) {
      return;
    }

    setEnviando(true);

    try {
      await aoPagar(cartao);
    } finally {
      setEnviando(false);
    }
  }

  const parcelas = Array.from({ length: 12 }, (_, indice) => indice + 1);

  const campo = (nome: keyof CartaoPayload, rotulo: string, extra: Partial<React.InputHTMLAttributes<HTMLInputElement>> = {}, largo = false) => (
    <label className={`${largo ? "largo " : ""}${erros[nome] ? "campo-erro" : ""}`}>
      {rotulo}
      <input
        value={String(cartao[nome])}
        onChange={(evento) => mascarar(nome, evento.target.value)}
        autoComplete="off"
        {...extra}
      />
      {erros[nome] && <span className="erro-campo">{erros[nome]}</span>}
    </label>
  );

  return (
    <form onSubmit={enviar} noValidate>
      <div className="form-grade">
        {campo("nomeImpresso", "Nome impresso no cartão", { placeholder: "COMO ESTÁ NO CARTÃO" }, true)}
        {campo("numero", `Número do cartão${marca ? ` · ${marca}` : ""}`, { placeholder: "0000 0000 0000 0000", inputMode: "numeric" }, true)}
        {campo("validade", "Validade", { placeholder: "MM/AA", inputMode: "numeric" })}
        {campo("cvv", "Código de segurança (CVV)", { placeholder: tamanhoCvv === 4 ? "0000" : "000", inputMode: "numeric", type: "password" })}
        {campo("cpf", "CPF do titular (opcional)", { placeholder: "000.000.000-00", inputMode: "numeric" })}

        <label>
          Parcelas
          <select value={cartao.parcelas} onChange={(evento) => mascarar("parcelas", evento.target.value)}>
            {parcelas.map((quantidade) => (
              <option key={quantidade} value={quantidade}>
                {quantidade}x de {moeda(Math.round((compra.valor / quantidade) * 100) / 100)} sem juros
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="compra-linha total" style={{ marginTop: 14 }}>
        <span>{cartao.parcelas > 1 ? `${cartao.parcelas}x sem juros` : "À vista"}</span>
        <strong>{moeda(compra.valor)}</strong>
      </div>

      <button className="botao-primario botao-largo" type="submit" disabled={enviando}>
        {enviando ? "Processando simulação..." : `Pagar ${moeda(compra.valor)}`}
      </button>

      <p style={{ color: "#9b8fb0", fontSize: 12, marginTop: 12, lineHeight: 1.5 }}>
        Cartões de teste: <code>4111 1111 1111 1111</code> é aprovado ·{" "}
        <code>4000 0000 0000 0002</code> é recusado · <code>4000 0000 0000 9995</code> recusa por
        limite. Qualquer outro número válido é aprovado. Validade futura e qualquer CVV.
        Número completo, validade e CVV não são gravados.
      </p>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Página
// ---------------------------------------------------------------------------

function PagamentoPagina() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [compra, setCompra] = useState<CompraApi | null>(null);
  const [qr, setQr] = useState("");
  const [erro, setErro] = useState("");
  const [processando, setProcessando] = useState(false);

  const compraId = Number(id);

  async function carregar() {
    try {
      setCompra(await obterCompra(compraId));
    } catch (falha) {
      setErro(falha instanceof Error ? falha.message : "Compra não encontrada.");
    }
  }

  useEffect(() => {
    obterCompra(compraId)
      .then(setCompra)
      .catch((falha: Error) => setErro(falha.message || "Compra não encontrada."));
  }, [compraId]);

  useEffect(() => {
    if (!compra?.codigoPix) return;

    QRCode.toDataURL(compra.codigoPix, { margin: 1, width: 240, errorCorrectionLevel: "M" })
      .then(setQr)
      .catch(() => setQr(""));
  }, [compra?.codigoPix]);

  async function executar(acao: () => Promise<CompraApi>) {
    setProcessando(true);

    try {
      const atualizada = await acao();
      setCompra(atualizada);

      if (atualizada.status === "Pago") {
        toast.success("Pagamento aprovado! (simulação)");
        void obterCarrinho().catch(() => undefined); // atualiza o contador do carrinho
      } else if (atualizada.status !== "Pendente") {
        toast.error(ROTULO_PAGAMENTO[atualizada.status] ?? atualizada.status);
      }
    } catch (falha) {
      toast.error(falha instanceof Error ? falha.message : "Falha ao processar o pagamento.", {
        duration: 6000,
      });
      await carregar();
    } finally {
      setProcessando(false);
    }
  }

  if (erro) {
    return (
      <main className="pagina">
        <div className="pagina-vazio">
          <h3>{erro}</h3>
          <button className="botao-primario" onClick={() => navigate("/pedidos")}>
            Ver minhas compras
          </button>
        </div>
      </main>
    );
  }

  if (!compra) {
    return (
      <main className="pagina">
        <div className="pagina-carregando">Carregando pagamento...</div>
      </main>
    );
  }

  const pendente = compra.status === "Pendente";
  const itens = compra.pedidos.flatMap((pedido) => pedido.itens);

  return (
    <main className="pagina">
      <div className="pagina-topo">
        <button onClick={() => navigate("/pedidos")}>← Minhas compras</button>
        <h1>Pagamento · {compra.codigo}</h1>
      </div>

      <div className="aviso-simulacao">
        🧪 <strong>SIMULAÇÃO.</strong> Este pagamento acontece só dentro do RarityRoom (projeto
        acadêmico). Códigos PIX, boletos e cartões aqui não têm validade bancária e nada é cobrado.
      </div>

      <div className="compra-layout">
        <div>
          {!pendente && <Resultado compra={compra} />}

          {pendente && compra.forma === "Pix" && (
            <section className="compra-bloco">
              <h2>⚡ Pague com PIX (demonstrativo)</h2>

              <div className="pix-area">
                <div className="pix-qr">
                  {qr ? <img src={qr} alt="QR Code demonstrativo — não é um PIX válido" /> : <div style={{ height: 200 }} />}
                  <em>DEMONSTRATIVO · SEM VALOR</em>
                </div>

                <div>
                  <p style={{ color: "#c7bdda", marginTop: 0 }}>
                    Valor: <strong style={{ color: "#d9aa32" }}>{moeda(compra.valor)}</strong>
                    <br />
                    Expira em <Contagem ate={compra.expiraEm} aoTerminar={() => void carregar()} />
                  </p>

                  <small style={{ color: "#9b8fb0" }}>PIX copia e cola (fictício)</small>
                  <div className="codigo-copia">
                    <code>{compra.codigoPix}</code>
                    <button className="botao-secundario" onClick={() => void copiar(compra.codigoPix, "Código PIX")}>
                      Copiar
                    </button>
                  </div>
                </div>
              </div>

              <div className="acoes-linha">
                <button
                  className="botao-primario"
                  disabled={processando}
                  onClick={() => void executar(() => simularConfirmacao(compra.id))}
                >
                  {processando ? "Processando..." : "✓ Simular pagamento confirmado"}
                </button>
                <button
                  className="botao-secundario"
                  disabled={processando}
                  onClick={() => void executar(() => simularExpiracao(compra.id))}
                >
                  Simular expiração
                </button>
              </div>
            </section>
          )}

          {pendente && compra.forma === "Boleto" && (
            <section className="compra-bloco">
              <h2>🧾 Boleto bancário (demonstrativo)</h2>

              <div className="boleto area-impressao">
                <div className="boleto-marca">
                  SIMULAÇÃO
                  <br />
                  SEM VALIDADE BANCÁRIA
                </div>

                <div className="boleto-topo">
                  RarityRoom Demo <span>999-9</span>
                  <code>{compra.linhaDigitavel}</code>
                </div>

                <div className="boleto-campos">
                  <div>
                    <small>Beneficiário</small>RarityRoom (simulação acadêmica)
                  </div>
                  <div>
                    <small>Vencimento</small>
                    {data(compra.expiraEm)}
                  </div>
                  <div>
                    <small>Valor do documento</small>
                    {moeda(compra.valor)}
                  </div>
                  <div>
                    <small>Nosso número / compra</small>
                    {compra.codigo}
                  </div>
                  <div>
                    <small>Data do documento</small>
                    {data(compra.criadoEm)}
                  </div>
                  <div>
                    <small>Pedidos</small>
                    {compra.pedidos.map((pedido) => pedido.codigo).join(", ")}
                  </div>
                </div>

                <CodigoBarras codigo={compra.codigoBarras} />

                <p style={{ fontSize: 11, color: "#555", margin: "6px 0 0" }}>
                  Documento ilustrativo gerado para demonstração. Não pague este boleto em nenhum banco.
                </p>
              </div>

              <small style={{ color: "#9b8fb0", display: "block", marginTop: 14 }}>
                Linha digitável (fictícia)
              </small>
              <div className="codigo-copia">
                <code>{compra.linhaDigitavel}</code>
                <button className="botao-secundario" onClick={() => void copiar(compra.linhaDigitavel, "Linha digitável")}>
                  Copiar
                </button>
              </div>

              <div className="acoes-linha">
                <button className="botao-secundario" onClick={() => window.print()}>
                  🖨 Visualizar / imprimir boleto
                </button>
                <button
                  className="botao-primario"
                  disabled={processando}
                  onClick={() => void executar(() => simularConfirmacao(compra.id))}
                >
                  {processando ? "Processando..." : "✓ Simular pagamento confirmado"}
                </button>
                <button
                  className="botao-secundario"
                  disabled={processando}
                  onClick={() => void executar(() => simularExpiracao(compra.id))}
                >
                  Simular vencimento
                </button>
              </div>
            </section>
          )}

          {pendente && compra.forma === "CartaoCredito" && (
            <section className="compra-bloco">
              <h2>💳 Cartão de crédito (simulado)</h2>
              <FormularioCartao
                compra={compra}
                aoPagar={(cartao) => executar(() => pagarComCartao(compra.id, cartao))}
              />
            </section>
          )}

          {pendente && (
            <div className="acoes-linha" style={{ marginBottom: 16 }}>
              <button
                className="botao-perigo"
                disabled={processando}
                onClick={() => {
                  if (window.confirm("Cancelar esta compra? Seus itens continuam no carrinho.")) {
                    void executar(() => cancelarCompra(compra.id));
                  }
                }}
              >
                Cancelar compra
              </button>
            </div>
          )}
        </div>

        <aside className="compra-bloco compra-resumo">
          <h2>Resumo</h2>

          {itens.map((item) => (
            <div className="compra-linha" key={`${item.produtoId}-${item.id}`}>
              <span>
                {item.quantidade}× {item.nomeProduto}
              </span>
              <span>{moeda(item.subtotal)}</span>
            </div>
          ))}

          <div className="compra-linha">
            <span>Forma</span>
            <span>
              {ROTULO_FORMA[compra.forma]}
              {compra.cartaoFinal && ` · ${compra.cartaoBandeira} final ${compra.cartaoFinal}`}
              {compra.parcelas > 1 && ` · ${compra.parcelas}x ${moeda(compra.valorParcela)}`}
            </span>
          </div>
          <div className="compra-linha">
            <span>Criada em</span>
            <span>{dataHora(compra.criadoEm)}</span>
          </div>
          <div className="compra-linha">
            <span>Situação</span>
            <span>{ROTULO_PAGAMENTO[compra.status] ?? compra.status}</span>
          </div>
          <div className="compra-linha total">
            <span>Total</span>
            <strong>{moeda(compra.valor)}</strong>
          </div>
        </aside>
      </div>
    </main>
  );
}

function Resultado({ compra }: { compra: CompraApi }) {
  const navigate = useNavigate();

  const conteudo: Record<string, { icone: string; titulo: string; texto: string }> = {
    Pago: {
      icone: "✅",
      titulo: "Pagamento aprovado!",
      texto: "Seu pedido foi registrado e o estoque reservado. Os vendedores já foram avisados.",
    },
    Recusado: {
      icone: "❌",
      titulo: "Pagamento recusado",
      texto: "A simulação recusou o cartão. Nada foi cobrado e seus itens continuam no carrinho.",
    },
    Expirado: {
      icone: "⌛",
      titulo: "Pagamento expirado",
      texto: "O prazo terminou sem confirmação. Seus itens continuam no carrinho para uma nova compra.",
    },
    Cancelado: {
      icone: "🚫",
      titulo: "Compra cancelada",
      texto: "Nenhum valor foi cobrado e o estoque não foi alterado.",
    },
  };

  const info = conteudo[compra.status] ?? conteudo.Cancelado;

  return (
    <section className="compra-bloco resultado-pagamento">
      <span>{info.icone}</span>
      <h2>{info.titulo}</h2>
      <p>{info.texto}</p>
      {compra.motivo && <p className="item-aviso">{compra.motivo}</p>}

      <div className="acoes-linha" style={{ justifyContent: "center" }}>
        {compra.status === "Pago" ? (
          <>
            {compra.pedidos.map((pedido) => (
              <button key={pedido.id} className="botao-primario" onClick={() => navigate(`/pedidos/${pedido.id}`)}>
                Ver pedido {pedido.codigo}
              </button>
            ))}
            <button className="botao-secundario" onClick={() => navigate("/home")}>
              Continuar comprando
            </button>
          </>
        ) : (
          <>
            <button className="botao-primario" onClick={() => navigate("/carrinho")}>
              Voltar ao carrinho
            </button>
            <button className="botao-secundario" onClick={() => navigate("/pedidos")}>
              Minhas compras
            </button>
          </>
        )}
      </div>
    </section>
  );
}

export default PagamentoPagina;
