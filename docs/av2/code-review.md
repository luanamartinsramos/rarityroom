# Code review

Revisão do código do backend, com foco em correção, coesão, duplicação e
clareza. Cada achado aponta o problema, a correção aplicada e a evidência.

## 1. Método

1. Leitura das 18 classes originais do backend.
2. Execução do fluxo real por HTTP para observar comportamento, não só código.
3. Correção na camada dona do problema.
4. Prova por teste automatizado + chamada HTTP.

## 2. Achados e correções

### C-01 — Regra de negócio ausente na entidade
**Antes.** `Produto.Atualizar(...)` copiava os 9 campos sem validar nada.
**Depois.** Validação compartilhada entre construtor e `Atualizar` em
`AplicarDados`, com mensagem específica por campo.
**Evidência.** `ProdutoValidacaoTests` (10 casos).

### C-02 — Entidade sem identidade de dono
**Antes.** `Produto` não conhecia o autor; a autorização era só `[Authorize]`.
**Depois.** `UsuarioId` obrigatório + `ValidarPropriedade`/`ValidarExclusao`.
**Evidência.** `ProdutoPropriedadeTests`; HTTP 403 para terceiro.

### C-03 — Identificador derivado da contagem
**Antes.** `ObterTodos().Count + 1`.
**Depois.** Contador próprio no repositório, sob `lock`.
**Evidência.** `RepositorioProdutoTests`; HTTP id 4 após excluir id 2.

### C-04 — Vazamento da coleção interna
**Antes.** `ObterTodos()` devolvia a `List<T>` real; qualquer camada alterava
o "banco" sem passar pelo repositório.
**Depois.** Devolve cópia; toda mutação passa por método do repositório.
**Evidência.** `ObterTodos_DeveDevolverCopiaDaLista`.

### C-05 — Exceção genérica escolhendo status HTTP
**Antes.** `ServicoAutenticacao` lançava `InvalidOperationException` e o
controller traduzia em `Conflict` dentro de `try/catch`.
**Depois.** `ConflitoException` + middleware central.
**Evidência.** HTTP 409 com `codigo: "conflito"`.

### C-06 — Validação duplicada entre HTTP e domínio
**Antes.** `[MinLength(6)]`, `[Range(0.01, …)]`, `[EmailAddress]` no modelo e
regras equivalentes ausentes no domínio.
**Depois.** Modelo só com `[Required]`; regra única no domínio.
**Evidência.** `ModelosRequisicao.cs`; 400 com mensagem do domínio.

### C-07 — Parse de sessão repetido em cada controller
**Antes.** Cada controller teria seu `int.TryParse(claim)`.
**Depois.** `ClaimsExtensions.ObterUsuarioId` — um dono para a conversão.
**Evidência.** `Controller/ClaimsExtensions.cs`.

### C-08 — Decisão de negócio codificada em condicional
**Antes.** (projetado) `if (modalidade == Liquidacao) ... else if ...`.
**Depois.** `IEstrategiaNegociacao` + 3 implementações injetadas.
**Evidência.** `EstrategiasTests`; HTTP com resultados divergentes.

### C-09 — Comparação de categoria por texto cru
**Antes.** `string` livre, sensível a caixa e acento.
**Depois.** `Categoria` com slug normalizado e unicidade por slug.
**Evidência.** `CategoriaTests` (inclui `ánimé` → `anime`).

### C-10 — Recusa de ofertas não modelada
**Antes.** Não existia negociação; só criação de anúncio.
**Depois.** `Proposta` com ciclo e recusa em cascata ao aceitar.
**Evidência.** `ServicoPropostasTests.Aceitar_DeveRecusarAsDemaisPendentes`.

## 3. O que foi preservado

- Namespace `RarityRoom`, rotas `api/produtos` e `api/autenticacao`.
- Autenticação por cookie `RarityRoom.Auth` (HttpOnly, SameSite=Lax).
- CORS restrito a `http://localhost:5173`.
- Contrato JSON do anúncio: todos os campos originais continuam presentes e
  o frontend não precisou de alteração.
- Hash PBKDF2 com salt (100 mil iterações) — já estava correto.

## 4. Pendências de revisão (declaradas, não escondidas)

| ID | Pendência | Impacto |
|---|---|---|
| P-01 | ~~Repositórios repetem `lock` + cópia~~ — resolvido: produção usa EF Core, memória ficou só nos testes | removida a duplicação |
| P-02 | Entidades são serializadas diretamente no JSON | qualquer propriedade nova vira contrato sem decisão |
| P-03 | Busca e filtro acontecem no cliente | não escala com catálogo grande |
| P-04 | ~~Sem persistência~~ — resolvido: SQLite + EF Core com migração versionada | dados sobrevivem ao reinício |

Nenhuma dessas pendências quebra regra de negócio; todas estão registradas em
`auditoria-final.md`.
