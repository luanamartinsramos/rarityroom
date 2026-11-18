# Auditoria final — 40 itens da AV2

Correspondência entre os **40 itens do enunciado** ("TRABALHO PRÁTICO — AV2",
checklist da seção 2, itens A01–G02), a implementação, os testes e os
resultados. A numeração e a redação resumida seguem o documento da
disciplina.

**Regra desta auditoria:** um item só é marcado **Atendido** se a evidência
foi **executada ou inspecionada nesta data** e pode ser localizada pelo
caminho indicado. O índice legível por máquina é
[`evidencias.json`](evidencias.json).

> **Correção de rumo registrada.** A versão anterior desta auditoria avaliava
> 40 critérios **sintetizados pelo grupo** (ex.: "nomenclatura consistente",
> "categoria modelada como entidade"), que não eram os itens do enunciado. Ela
> declarava 39 atendidos e trazia contagens de teste divergentes (104, 135,
> 154, 174). Esta versão substitui aquela e avalia os itens oficiais.

## Ambiente da auditoria

| Item | Valor |
|---|---|
| Data | 2026-11-18 |
| SDK | .NET 10.0.404 (Linux) |
| Build | `dotnet build backend` → **0 avisos, 0 erros** |
| Testes | `dotnet test tests/RarityRoom.Tests` → **245 aprovados, 0 falhas** |
| Branch avaliada | `main` |
| API | verificação real por `curl` em `http://localhost:5000` ([`excecoes.md`](excecoes.md) §3) |

## A — Aplicação dos conceitos de POO (8/8)

| Item | Exigência (resumo) | Situação | Evidência |
|---|---|---|---|
| A01 | ≥ 5 classes de conceitos reais do domínio | Atendido | `Usuario`, `Produto`, `Proposta`, `Pedido`, `ItemPedido`, `Categoria`, `Favorito`, `Avaliacao`, `Pagamento` em `backend/Models/` |
| A02 | ≥ 2 classes com comportamento próprio | Atendido | `Produto.AplicarOperacao`, `BaixarEstoque`, `ReporEstoque`; `Pedido.ConfirmarPagamento`, `CancelarPago`; `Proposta.Aceitar` |
| A03 | ≥ 5 regras de negócio implementadas e usadas no backend | Atendido | R1–R5 em [`requisitos.md`](requisitos.md) §4, cada uma com o membro que a implementa e o endpoint que a usa |
| A04 | fluxo com colaboração entre ≥ 2 classes de responsabilidades diferentes | Atendido | FL-02: `ServicoPropostas` coordena `Proposta`, `Produto` e `IEstrategiaNegociacao` ([`modelo-de-classes.md`](modelo-de-classes.md) §8) |
| A05 | objeto central com ≥ 3 estados e transições, preservando identidade | Atendido | `Produto`: `Disponivel`, `Reservado`, `Pausado`, `Vendido`; tabela em [`requisitos.md`](requisitos.md) §5; `ProdutoTransicoesTests` (12 casos) |
| A06 | transição proibida rejeitada sem inconsistência, inclusive persistida | Atendido | `Falha01_VenderAnuncioPausado_Responde409_EEstadoContinuaPausado` relê o banco após a rejeição |
| A07 | ≥ 2 regras protegidas por encapsulamento | Atendido | `set` privado em todas as entidades; estoque só muda por `BaixarEstoque`/`ReporEstoque`/`Atualizar` (R5); estado só por `AplicarOperacao` (R2); dicionário `Transicoes` é `private static readonly` |
| A08 | polimorfismo real: contrato + 2 implementações usadas | Atendido | `IEstrategiaNegociacao` + 3 implementações registradas na injeção; `Aceitar_MesmaOferta_DeveVariarPorModalidade` |

## B — Modelos de classes (8/8)

| Item | Exigência (resumo) | Situação | Evidência |
|---|---|---|---|
| B01 | modelo textual interpretável com ≥ 5 classes | Atendido | Mermaid `classDiagram` em [`modelo-de-classes.md`](modelo-de-classes.md) §2 |
| B02 | ≥ 5 classes do modelo correspondem ao código | Atendido | §6: `Usuario`, `Produto`, `Proposta`, `Pedido`, `ItemPedido` conferidas por arquivo |
| B03 | atributos e métodos das 5 classes sem contradição com o código | Atendido | §6, membro a membro; a seção antiga que descrevia `Pedido` com `ProdutoId`/`PrecoUnitario` (anterior aos itens) foi substituída |
| B04 | associação com multiplicidade, implementada | Atendido | §3: `Usuario 1 → 0..* Produto` via `Produto.UsuarioId` obrigatório; favorito único por par via índice único |
| B05 | composição / dependência de ciclo de vida comprovada | Atendido | §4: `Pedido *-- 1..* ItemPedido` (`OwnsMany`, criação só no construtor, pedido vazio proibido) |
| B06 | contrato polimórfico e implementações no modelo | Atendido | §5 + realizações `<|..` no diagrama |
| B07 | invariante relevante e objeto responsável | Atendido | §7: tabela invariante → responsável → teste; invariante entre instâncias atribuída a `ServicoPropostas` |
| B08 | duas decisões comparando alternativas com justificativa técnica | Atendido | [`decisoes-de-modelagem.md`](decisoes-de-modelagem.md) DM-A (4 alternativas) e DM-B (3 alternativas), com critérios e código |

## C — Análise crítica de código (6/6)

| Item | Exigência (resumo) | Situação | Evidência |
|---|---|---|---|
| C01 | alteração funcional no Git com diff verificável | Atendido | commit `78cae8b` ([`code-review.md`](code-review.md) §1; `git show 78cae8b`) |
| C02 | alteração corresponde a requisito/regra | Atendido | R5 e CA-03.1/03.2; tabela de inspeção orientada por requisito (§2) |
| C03 | análise dos arquivos modificados e do que está fora do escopo | Atendido | §3: inventário (1 arquivo), nenhuma mudança fora do escopo, ausências (chamadores e testes) rastreadas até `1b32db1` e `4bd1f9b` |
| C04 | impacto sobre regras, contratos e dependências, com verificação | Atendido | §4: dependentes por `grep`, contrato implícito com `RegrasCompra`; 17 testes relacionados executados (aprovados) |
| C05 | avaliação por ≥ 2 propriedades de POO | Atendido | §5: encapsulamento (com falha encontrada), coesão/responsabilidade, acoplamento ao ciclo de estados |
| C06 | decisão de aceitar/corrigir/rejeitar fundamentada | Atendido | §6: **aceitar com correção**; correção em `116d245` com testes de regressão |

## D — Controle de exceções (4/4)

| Item | Exigência (resumo) | Situação | Evidência |
|---|---|---|---|
| D01 | 3 situações de falha, separando regra de negócio e excepcional | Atendido | [`excecoes.md`](excecoes.md) §2: FALHA-01 (transição), FALHA-02 (dado inválido), FALHA-03 (erro inesperado); política no §1 |
| D02 | caminho reproduzível de lançamento/propagação até o tratamento | Atendido | §3: pilha `Produto` → serviço → controller → middleware, reproduzida por `curl` com resposta e log reais |
| D03 | falha excepcional tratada sem ocultar e sem vazar detalhes | Atendido | §4: `catch (Exception)` → 500 genérico + `LogError` com a exceção completa |
| D04 | testes das 3 situações | Atendido | `FalhasApiTests` (3 testes, aprovados), verificando resposta **e** estado |

## E — Depuração (6/6)

| Item | Exigência (resumo) | Situação | Evidência |
|---|---|---|---|
| E01 | defeito reproduzível com evidência | Atendido | DEF-01 ([`debugging.md`](debugging.md) §1): teste falha em `166c21d` com estoque `-2147483648` |
| E02 | condições, esperado e observado | Atendido | §2 |
| E03 | hipóteses e investigação com evidência | Atendido | §3–4: H1 confirmada, H2 e H3 descartadas pelo rastro |
| E04 | técnica de depuração comprovada | Atendido | §4: rastreamento por log com 3 pontos instrumentados; rastro reproduzido no documento; experimento `checked` |
| E05 | causa, correção e eliminação demonstradas | Atendido | §5: causa raiz (invariante sem teto + escrita sem validação), correção `116d245`, mesmo teste aprovado |
| E06 | antes × depois e comparação entre técnicas | Atendido | §6: tabela antes/depois; log × breakpoint (× `git bisect`) por 7 critérios, com recomendação |

## F — Testes, rastreabilidade e auditoria (6/6)

| Item | Exigência (resumo) | Situação | Evidência |
|---|---|---|---|
| F01 | sucesso e violação para as 5 regras (≥ 10 cenários) | Atendido | [`testes.md`](testes.md) §3: 22 cenários nomeados |
| F02 | teste que viola invariante e confirma objeto válido | Atendido | §4: `BaixarEstoque_AcimaDoDisponivel_DeveFalharSemAlterar` e outros 3 |
| F03 | integração pela API com regra e persistência | Atendido | §5: `Falha01_VenderAnuncioPausado_Responde409_EEstadoContinuaPausado` |
| F04 | compila e testa a partir de clone limpo, seguindo as instruções | Atendido | §1; ver "Prova de clone limpo" abaixo |
| F05 | rastreabilidade 3 fluxos × 5 regras × implementação × testes | Atendido | §6 (três tabelas) |
| F06 | auditoria final com evidências, pendências e limitações, sem declarar o não comprovado | Atendido | este documento |

## G — Especificação verificável e agentes (2/2)

| Item | Exigência (resumo) | Situação | Evidência |
|---|---|---|---|
| G01 | especificação dos 3 fluxos e 5 regras com condições e critérios de aceitação | Atendido | [`requisitos.md`](requisitos.md) §3–4 (Dado/Quando/Então) |
| G02 | especificação para o DsCode, associada a alteração funcional, com inspeção | Atendido, com ressalva | [`especificacao-dscode.md`](especificacao-dscode.md): mudança "conta única", executada pelo DsCode em 2026-11-14 e reinspecionada hoje. **Ressalva:** o texto literal do pedido não foi versionado na época; a especificação foi reconstruída a partir do registro da sessão, e isso está declarado no próprio documento |

## Resumo

| Área | Atendidos |
|---|---:|
| A — POO | 8/8 |
| B — Modelo de classes | 8/8 |
| C — Análise de código | 6/6 |
| D — Exceções | 4/4 |
| E — Depuração | 6/6 |
| F — Testes e auditoria | 6/6 |
| G — Especificação e agentes | 2/2 (G02 com ressalva) |
| **Total** | **40/40** |

## Pendências e limitações declaradas

| ID | Pendência / limitação | Impacto | Ação recomendada |
|---|---|---|---|
| P-01 | Entidades de domínio são serializadas direto no JSON da API | uma propriedade nova vira contrato público sem decisão explícita | criar DTOs de saída |
| P-02 | `Usuario.PodeComprar`/`PodeVender` sem uso depois da "conta única" | código morto | remover (achado da inspeção G02) |
| P-03 | Transições induzidas por estoque (→ `Vendido`) não passam pela tabela `Transicoes` | duas fontes de mudança de estado | decisão consciente (DM-A, C06); manter documentado |
| P-04 | Pagamento, PIX e boleto são **simulados**; não há gateway | fora do escopo | — |
| P-05 | Recuperação de senha envia o token para o log do servidor, não por e-mail | sem provedor de e-mail | integrar provedor |
| P-06 | Frontend sem testes automatizados (verificado só por `build`/`lint`) | regressão visual não detectada automaticamente | — |
| P-07 | Associações entre agregados por identificador, sem chave estrangeira no banco | integridade referencial garantida pelo código, não pelo SGBD | avaliar FKs nas tabelas principais |

## Prova de clone limpo (F04)

Executada em 2026-11-18 sobre o commit `5c9a1a0`, num diretório temporário
vazio (o clone não traz `bin/`, `obj/` nem `rarityroom.db`):

```
$ git clone <repositório> clone-limpo && cd clone-limpo
$ git log --oneline -1
5c9a1a0 docs(av2): documentação alinhada aos 40 itens do enunciado
$ dotnet build backend
    0 Aviso(s)
    0 Erro(s)
$ dotnet test tests/RarityRoom.Tests
Aprovado!  – Com falha: 0, Aprovado: 245, Ignorado: 0, Total: 245
```
