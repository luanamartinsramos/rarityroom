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
 * O frontend roda em outra porta, então o caminho relativo precisa apontar
 * para o servidor da API. URLs absolutas (http/https) passam direto.
 */
export function urlImagem(url: string | null | undefined): string | undefined {
  if (!url) {
    return undefined;
  }

  return url.startsWith("/uploads/") ? `http://localhost:5000${url}` : url;
}
