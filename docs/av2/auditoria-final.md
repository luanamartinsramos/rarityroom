# Auditoria final — 40 itens da rubrica AV2

Auditoria item a item da rubrica oficial (*Trabalho Prático — AV2, Design e
Programação Orientados a Objetos*), numerada **A01–G02** como no enunciado.
Cada linha aponta a evidência no repositório. **Nenhum item é marcado como
atendido sem evidência verificável**; o que não foi comprovado aparece em
"Pendências e limitações".

## Ambiente e execução desta auditoria

| Item | Resultado | Evidência |
|---|---|---|
| Build do backend (clone limpo) | 0 erros, 0 avisos | `evidencias/clone-limpo.txt` |
| Testes (clone limpo) | **235 aprovados, 0 falhas** | `evidencias/clone-limpo.txt` |
| Banco local | criado na 1ª execução; 12 migrações aplicadas | `evidencias/clone-limpo.txt` |
| Frontend | `npm run build` sem erros | `evidencias/clone-limpo.txt` |
| SDK | .NET 10.0.404, Node 22 | — |

## A — Aplicação dos conceitos de POO

| Item | Situação | Evidência |
|---|---|---|
| A01 — ≥ 5 classes de domínio reais | Atendido | `Usuario`, `Produto`, `Categoria`, `Proposta`, `Favorito`, `Pedido`, `ItemPedido`, `Pagamento`, `ItemCarrinho`, `Avaliacao`, `DadosEntrega` (`backend/Models/`) |
| A02 — ≥ 2 classes com comportamento próprio | Atendido | `Produto.AplicarOperacao/BaixarEstoque/Reabastecer`; `Pedido.ConfirmarPagamento/CancelarPago`; `Pagamento.VerificarExpiracao/Aprovar` |
| A03 — ≥ 5 regras distintas usadas no backend | Atendido | RN1–RN5 em `especificacao.md` §3 (+ RC1–RC6 e RA1–RA10) |
| A04 — fluxo com colaboração entre classes | Atendido | aprovação do pagamento: `Pagamento` + `RegrasCompra` + `Produto` + `Pedido` (`modelo-de-classes.md` §8); aceite de proposta: `Proposta` + `Produto` + `IEstrategiaNegociacao` |
| A05 — objeto central com ≥ 3 estados e transições | Atendido | `Produto.Estado` (4 estados, tabela `Transicoes`); `Pedido.Estado` (6); `Pagamento.Status` (6) — identidade preservada (mesmo `Id`) |
| A06 — transição proibida rejeitada sem inconsistência | Atendido | `Reativar_DeReservado_NaoDeveCancelarReserva` (objeto); `TransicaoProibida_ViaApi_EhRejeitadaENaoAlteraOBanco` (dado persistido) |
| A07 — ≥ 2 regras protegidas por encapsulamento | Atendido | `Produto.Estoque`/`Estado` com *setter* privado, alterados só por métodos que validam; `Pedido.itens` privado + `IReadOnlyList` (`Itens_NaoPodemSerAlteradosDeFora_PreservandoOTotal`); `Pagamento.Status` só por `Aprovar/Recusar/...` |
| A08 — polimorfismo real usado | Atendido | `IEstrategiaNegociacao` + 3 implementações, escolhidas em `ServicoPropostas.Aceitar`; `Aceitar_MesmaOferta_DeveVariarPorModalidade` |

## B — Modelos de classes

| Item | Situação | Evidência |
|---|---|---|
| B01 — modelo textual com ≥ 5 classes | Atendido | `modelo-de-classes.md` §1 (Mermaid, 15 classes/interfaces) |
| B02 — ≥ 5 classes correspondem ao C# | Atendido | `modelo-de-classes.md` §6 (tabela classe ↔ arquivo) |
| B03 — atributos e métodos sem contradição | Atendido | nomes conferidos no código nesta auditoria; omissões deliberadas declaradas em §6 |
| B04 — associação com multiplicidade implementada | Atendido | `modelo-de-classes.md` §2 (ex.: `Pagamento 1 → 1..* Pedido`, `Usuario 1 → 0..* Produto`) |
| B05 — composição/ciclo de vida comprovado | Atendido | `Pedido ◆— ItemPedido` (`OwnsMany`, cascade, lista privada) — §3 |
| B06 — contrato polimórfico no modelo | Atendido | §1 e §4 (`IEstrategiaNegociacao` + 3 implementações) |
| B07 — invariante e objeto responsável | Atendido | §5 (estoque → `Produto`; total → `Pedido`; transição única → `Pagamento`; venda dupla → `ServicoPedidos` + transação) |
| B08 — 2 decisões comparando alternativas | Atendido | `decisoes-de-modelagem.md` §A: DA1–DA4 com tabelas de critérios |

## C — Análise crítica de código

| Item | Situação | Evidência |
|---|---|---|
| C01 — alteração funcional no Git com diff | Atendido | commit `1b32db1` (`git show 1b32db1`), `code-review.md` §1 |
| C02 — corresponde a requisito/regra | Atendido | `code-review.md` §2 (RN4, RN5, aprovação no servidor) |
| C03 — análise dos arquivos e do que está fora do escopo | Atendido | `code-review.md` §3 (4 arquivos; estorno e textos fora do escopo; omissão em `ServicoAdmin`) |
| C04 — impacto com verificação | Atendido | `code-review.md` §4 (contratos quebrados, dependências 4→9, build em `1b32db1` com 1 erro e em `09d4a32` sem erros) |
| C05 — ≥ 2 propriedades de POO | Atendido | `code-review.md` §5 (encapsulamento, coesão, acoplamento, responsabilidades) |
| C06 — decisão fundamentada | Atendido | `code-review.md` §6: corrigir (commit `3a61de9`) e aceitar |

## D — Exceções

| Item | Situação | Evidência |
|---|---|---|
| D01 — 3 situações, regra × excepcional | Atendido | `excecoes.md` §1 (S1 regra, S2 entrada inválida, S3 excepcional; recusa de cartão como resultado sem exceção) |
| D02 — caminho de lançamento/propagação/tratamento | Atendido | `excecoes.md` §2 (`Produto.BaixarEstoque` → `UnidadeDeTrabalhoEf` rollback + `throw;` → `MiddlewareExcecoes`) |
| D03 — falha excepcional sem ocultar nem vazar | Atendido | `MiddlewareExcecoes` (`LogError` + 500 genérico); log real em `evidencias/depuracao/01-…` |
| D04 — testes das 3 situações | Atendido | `FalhasEInvariantesApiTests` S1, S2, S3 |

## E — Depuração

| Item | Situação | Evidência |
|---|---|---|
| E01 — defeito reproduzível com evidência | Atendido | `debugging.md` §A1; defeito real reintroduzido de forma controlada; `evidencias/depuracao/01-…`, `02-…` |
| E02 — condições, esperado e observado | Atendido | `debugging.md` §A2; `evidencias/depuracao/reproduzir.sh` |
| E03 — hipóteses com evidência | Atendido | `debugging.md` §A3 (H1 e H2 descartadas, H3 confirmada) |
| E04 — técnica de depuração comprovada | Atendido | análise de logs, stack trace e log temporário de diagnóstico com saída salva (`02-…`) |
| E05 — causa, correção e eliminação | Atendido | `debugging.md` §A5 (`entrega with { }`, commit `1b32db1`) |
| E06 — antes/depois + comparação de técnicas | Atendido | `debugging.md` §A6; `evidencias/depuracao/03-verificacao-antes-depois.txt` |

## F — Testes, rastreabilidade e auditoria

| Item | Situação | Evidência |
|---|---|---|
| F01 — sucesso e violação das 5 regras (≥ 10) | Atendido | `especificacao.md` §4: 10 de sucesso + 17 de violação |
| F02 — violar invariante e estado continuar válido | Atendido | `BaixarEstoque_AcimaDoDisponivel_DeveFalharSemAlterar`, `Itens_NaoPodemSerAlteradosDeFora_PreservandoOTotal` |
| F03 — integração pela API com regra e persistência | Atendido | `Pix_EstoqueSoBaixaDepoisDaAprovacao`, `TransicaoProibida_ViaApi_EhRejeitadaENaoAlteraOBanco` |
| F04 — compila e testa a partir de clone limpo | Atendido | `evidencias/clone-limpo.txt` (README §"Como baixar e executar") |
| F05 — rastreabilidade fluxos ↔ regras ↔ código ↔ testes | Atendido | `especificacao.md` §4 |
| F06 — auditoria com pendências, sem exageros | Atendido | este documento |

## G — Especificação verificável e agentes

| Item | Situação | Evidência |
|---|---|---|
| G01 — especificação dos 3 fluxos e 5 regras com critérios | Atendido | `especificacao.md` §1–§3 |
| G02 — especificação para o DsCode + verificação | Atendido, com ressalva | `especificacao-dscode.md` (commit `5d228b8` antes da implementação; execução `3e77f5b`, `54f4775`, `35e8232`; verificação §8). **Ressalva:** a execução foi feita por um agente de código (Claude Code) seguindo a especificação; não há registro de execução dentro da ferramenta DsCode |

## Resumo

| Situação | Itens |
|---|---:|
| Atendido | 39 |
| Atendido com ressalva | 1 (G02) |
| Não atendido | 0 |

## Pendências e limitações (declaradas)

| # | Pendência | Impacto |
|---|---|---|
| P1 | `ServicoPedidos` concentra checkout, pagamento simulado e pós-venda (baixa coesão, 9 dependências) — `code-review.md` §5 | manutenção; nenhuma regra quebrada. Ação: extrair `ServicoPagamentos` |
| P2 | Commits intermediários não foram feitos para compilar isoladamente: a sincronização da versão do zip e a sequência do marketplace foram divididas por camada (modelos, dados, serviços...). Verificado: `1b32db1` tem 1 erro de compilação; `09d4a32` compila | só afeta quem fizer *checkout* de um commit intermediário; o estado final e a `main` compilam e passam nos testes |
| P3 | `Usuario.TipoConta`, `PodeComprar` e `PodeVender` existem, mas nenhuma regra os usa desde que toda conta compra e vende | código morto declarado; nenhuma regra afirma o contrário |
| P4 | Expiração de PIX/boleto é verificada sob demanda (ao consultar/pagar), sem processo em segundo plano | compra vencida só muda de estado quando é acessada |
| P5 | Frontend sem testes automatizados versionados; fluxo verificado em navegador automatizado durante o desenvolvimento | regressões de interface não são barradas pela suíte |
| P6 | Integração testada só com SQLite; implantação gratuita (Render) tem disco efêmero | dados de demonstração online são apagados quando o serviço reinicia |
| P7 | Pagamentos são simulados por definição do trabalho (sem gateway); frete não é calculado | escopo acadêmico |
