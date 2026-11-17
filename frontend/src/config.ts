/**
 * Endereço do backend.
 * - Desenvolvimento: http://localhost:5000 (dotnet run).
 * - Produção (Vercel): vazio = mesmo domínio. A Vercel repassa /api e /uploads
 *   para o backend (ver vercel.json), então o cookie de login funciona sem
 *   configuração de CORS entre domínios.
 * Pode ser sobrescrito com a variável VITE_API_ORIGIN.
 */
export const API_ORIGIN: string =
  import.meta.env.VITE_API_ORIGIN ?? (import.meta.env.PROD ? "" : "http://localhost:5000");
