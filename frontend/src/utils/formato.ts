export function moeda(valor: number): string {
  return valor.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

export function dataHora(valor: string | null | undefined): string {
  if (!valor) {
    return "—";
  }

  // O backend grava em UTC; sem sufixo o navegador leria como hora local.
  const texto = /Z|[+-]\d\d:\d\d$/.test(valor) ? valor : `${valor}Z`;

  return new Date(texto).toLocaleString("pt-BR", {
    dateStyle: "short",
    timeStyle: "short",
  });
}

export function data(valor: string | null | undefined): string {
  if (!valor) {
    return "—";
  }

  const texto = /Z|[+-]\d\d:\d\d$/.test(valor) ? valor : `${valor}Z`;

  return new Date(texto).toLocaleDateString("pt-BR");
}

export function estrelas(media: number): string {
  const cheias = Math.round(media);

  return "★".repeat(cheias) + "☆".repeat(Math.max(0, 5 - cheias));
}

/**
 * Imagens enviadas por upload ficam no backend como "/uploads/arquivo.png".
 * Frontend e API são servidos pela mesma origem (em desenvolvimento, pelo
 * proxy do Vite), então o caminho relativo funciona como está. URLs
 * absolutas (http/https) passam direto.
 */
export function urlImagem(url: string | null | undefined): string | undefined {
  return url || undefined;
}

const ROTULOS: Record<string, string> = {
  Comum: "Comum",
  Raro: "Raro",
  Epico: "Épico",
  Lendario: "Lendário",
  Comprador: "Comprador",
  Vendedor: "Vendedor",
  Ambos: "Comprador e vendedor",
  PrecoFixo: "Preço fixo",
  AceitaOferta: "Aceita ofertas",
  Liquidacao: "Liquidação",
};

/** Converte valores de enum do backend (sem acento) em texto para a tela. */
export function rotulo(valor: string | null | undefined): string {
  if (!valor) {
    return "—";
  }

  return ROTULOS[valor] ?? valor;
}
