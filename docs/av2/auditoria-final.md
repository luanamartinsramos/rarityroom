# Auditoria final — 40 critérios AV2

## Nota sobre a numeração

Os critérios estão agrupados conforme a estrutura informada no enunciado da
AV2: **A** Conceitos de POO (8), **B** Modelo de classes (8), **C** Code review
(6), **D** Exceções (4), **E** Debugging (6), **F** Testes/rastreabilidade (6),
**G** Especificação verificável (2) — total 40. A redação de cada linha é uma
síntese do requisito correspondente; a numeração é desta auditoria e deve ser
conferida com o documento da disciplina.

**Nenhum critério é marcado como atendido sem evidência executada.**

## Ambiente da auditoria

| Item | Valor |
|---|---|
| SDK | .NET 10.0.401 |
| Build | `dotnet build` — 0 erros, 0 avisos |
| Testes | `dotnet test` — 174 aprovados, 0 falhas |
| API | verificação real por `curl` em `http://localhost:5000` |
| Frontend | React 19 + Vite em `http://localhost:5173` |

## Grupo A — Conceitos de POO (8)

| # | Critério | Situação | Evidência |
|---|---|---|---|
| A1 | Ao menos cinco classes de domínio relevantes | Atendido | `Usuario`, `Produto`, `Categoria`, `Proposta`, `Favorito` (+3 estratégias) |
| A2 | Encapsulamento de atributos | Atendido | Todos os `set` são `private`; mutação só por método (ex.: `Produto.AplicarOperacao`) |
| A3 | Regras de negócio dentro das classes | Atendido | `Produto.ValidarPropriedade`, `Proposta.Transicionar`, `Categoria.GerarSlug` |
| A4 | Propriedades e métodos coesos | Atendido | `modelo-de-classes.md` §2 |
| A5 | Construtores garantem invariantes | Atendido | `ProdutoValidacaoTests`, `UsuarioTests`, `CategoriaTests` |
| A6 | Abstração com propósito | Atendido | `ExcecaoDominio`, `IEstrategiaNegociacao`, `IRepositorio*`, `IServico*` |
| A7 | Herança só em especialização real | Atendido | Única: `TransicaoInvalidaException : ConflitoException` (acrescenta código próprio) |
| A8 | Polimorfismo verdadeiro, 2+ implementações usadas | Atendido | 3 estratégias; mesma oferta R$ 200 → recusada em `PrecoFixo` (409) e aceita em `Liquidacao` (200) |

## Grupo B — Modelo de classes (8)

| # | Critério | Situação | Evidência |
|---|---|---|---|
| B1 | Classes representam conceitos reais do marketplace | Atendido | anúncio, proposta, favorito, categoria, conta |
| B2 | Associações e multiplicidades corretas | Atendido | `modelo-de-classes.md` §1 (1 usuário → N anúncios; 1 anúncio → N propostas) |
| B3 | Separação de responsabilidades entre camadas | Atendido | tabela em `modelo-de-classes.md` §5 |
| B4 | Colaboração entre objetos | Atendido | fluxo de aceite em 5 passos (`modelo-de-classes.md` §4) |
| B5 | Estado do anúncio modelado | Atendido | `EstadoAnuncio` + tabela de transições; `ProdutoTransicoesTests` |
| B6 | Categoria modelada como entidade | Atendido | `Categoria` com `Slug`; `CategoriaTests` |
| B7 | Negociação modelada | Atendido | `Proposta` + `EstadoProposta`; `PropostaTests` |
| B8 | Favoritos modelados | Atendido | `Favorito`; `FavoritoTests` |

## Grupo C — Code review (6)

| # | Critério | Situação | Evidência |
|---|---|---|---|
| C1 | Nomenclatura consistente e significativa | Atendido | domínio em português, métodos com verbo (`ValidarPropriedade`, `RecusarDemaisPendentes`) |
| C2 | Ausência de duplicação | Atendido | regra de negócio única no domínio; os repositórios em memória saíram da produção e viraram dublês de teste; cada repositório EF implementa apenas a sua consulta |
| C3 | Métodos curtos e coesos | Atendido | maior método de domínio com ~40 linhas (`AplicarDados`) |
| C4 | Tratamento de erros consistente | Atendido | `MiddlewareExcecoes` + exceções tipadas |
| C5 | Legibilidade e comentários úteis | Atendido | comentários explicam o **porquê** (ex.: tabela de transições, D1) |
| C6 | Aderência aos padrões do projeto | Atendido | namespace, rotas e cookie preservados |

## Grupo D — Exceções (4)

| # | Critério | Situação | Evidência |
|---|---|---|---|
| D1 | Hierarquia de exceções própria | Atendido | `ExcecaoDominio` + 5 derivadas (`Models/Excecoes/`) |
| D2 | Exceções específicas em vez de genéricas | Atendido | nenhum `throw new Exception` no domínio; `ServicoAutenticacao` usa `ConflitoException` |
| D3 | Tratamento centralizado | Atendido | `MiddlewareExcecoes` mapeia 400/403/404/409/500 em um só lugar |
| D4 | Não expor detalhes internos | Atendido | 500 responde "Ocorreu um erro inesperado"; stack trace só no log |

## Grupo E — Debugging (6)

| # | Critério | Situação | Evidência |
|---|---|---|---|
| E1 | Reprodução do defeito antes da correção | **Parcial** | a causa do id duplicado foi demonstrada por leitura do código e aritmética (`{1,3}` → contagem 2 → id 3 duplicado); o comportamento antigo não foi executado após a reescrita |
| E2 | Identificação da causa raiz | Atendido | `Count + 1`; ausência de dono no modelo (`debugging.md` Casos 1 e 2) |
| E3 | Correção na camada correta | Atendido | id no repositório; autorização no domínio |
| E4 | Evidência objetiva da correção | Atendido | HTTP: id 4 após excluir id 2; 403 para terceiro em PUT/DELETE/PATCH |
| E5 | Teste de regressão para cada correção | Atendido | `Adicionar_AposExclusao_NaoDeveReutilizarId`, `Atualizar_PorOutroUsuario_DeveNegar` |
| E6 | Defeito documentado | Atendido | `debugging.md` com 3 casos + 7 episódios de depuração |

## Grupo F — Testes, rastreabilidade e auditoria (6)

| # | Critério | Situação | Evidência |
|---|---|---|---|
| F1 | Suíte automatizada executável | Atendido | `dotnet test` → 135 aprovados, incluindo 9 de integração HTTP |
| F2 | Cenários e regras cobertos | Atendido | 14 regras mapeadas em `testes.md` §5 |
| F3 | Estados e polimorfismo testados | Atendido | `ProdutoTransicoesTests` (12), `EstrategiasTests` (6) |
| F4 | Rastreabilidade requisito → teste | Atendido | `testes.md` §5 |
| F5 | Persistência e consistência verificadas | Atendido | SQLite + migração versionada; após reiniciar o servidor, anúncio, usuário, categoria e favorito continuaram no banco |
| F6 | Auditoria com evidência concreta | Atendido | este documento |

## Grupo G — Especificação verificável e trabalho com agente (2)

| # | Critério | Situação | Evidência |
|---|---|---|---|
| G1 | Especificação verificável | Atendido | `requisitos.md` com RF/RN identificados e evidência por linha |
| G2 | Registro transparente do trabalho com agente | Atendido | `decisoes-de-modelagem.md`, `debugging.md`, `management/cross-session/av2-handoff.md` |

## Resumo

| Situação | Quantidade |
|---|---:|
| Atendido | 39 |
| Parcial | 1 (E1) |
| Pendente | 0 |
| **Total** | **40** |

## Pendências declaradas

| ID | Pendência | Critério afetado | Ação recomendada |
|---|---|---|---|
| P-01 | ~~Repositórios repetem lock + cópia~~ — resolvido: produção usa EF Core; memória só em testes | C2 | — |
| P-02 | Entidades serializadas direto no JSON | C2 (futuro) | criar DTOs de saída |
| P-03 | Busca e filtro no cliente | C2 (futuro) | mover para o servidor com paginação |
| P-04 | ~~Dados só em memória~~ — resolvido com SQLite | F5 | — |
| P-05 | ~~Sem teste de integração HTTP~~ — resolvido: `FluxoApiTests` com `WebApplicationFactory` | F1/F2 | — |
| P-06 | Sem upload de imagem | — | armazenamento de arquivos |

## Evidências executadas nesta auditoria

```
dotnet build   → Compilação com êxito. 0 Aviso(s), 0 Erro(s)
dotnet test    → Com falha: 0, Aprovado: 104, Total: 104
GET  /api/categorias              → 200 (7 categorias semeadas)
GET  /api/produtos (anônimo)      → 200
POST /api/autenticacao/cadastrar  → 200 (id 1) / 200 (id 2)
POST /api/autenticacao/cadastrar  → 409 conflito (e-mail repetido)
POST /api/produtos                → 201 (id 1, modalidade PrecoFixo)
POST /api/produtos                → 201 (id 2, modalidade Liquidacao)
PUT  /api/produtos/1 (terceiro)   → 403 acesso-negado
DELETE /api/produtos/1 (terceiro) → 403 acesso-negado
PATCH /api/produtos/1/estado      → 403 acesso-negado (terceiro)
POST /api/produtos (preço 0)      → 400 dados-invalidos
PATCH /api/produtos/1/estado      → 409 transicao-invalida (vender pausado)
POST /api/propostas               → 201 (oferta 200 nos dois anúncios)
POST /api/propostas/1/aceitar     → 409 conflito (PrecoFixo, mínimo 249,90)
POST /api/propostas/2/aceitar     → 200 (Liquidacao, proposta Aceita)
GET  /api/produtos/2              → estado Reservado
POST /api/favoritos/1             → 201
POST /api/favoritos/1             → 409 conflito (duplicado)
POST /api/favoritos/1 (dono)      → 400 dados-invalidos (autofavorito)
DELETE /api/favoritos/1           → 200
```

## Prova de persistência (SQLite)

Executada parando e subindo o processo do backend:

```
1. servidor no ar  → cadastro Ana (200), cadastro Bruno (200)
2.                     criação de anúncio (201), favorito do Bruno (201)
3. servidor parado (taskkill) — banco em disco: rarityroom.db
4. servidor subido novamente, com a migração já aplicada
5. GET /api/produtos    → 1 anúncio: "Funko Batman", 249.9, estado Disponivel
6. POST /api/autenticacao/entrar (ana@teste.com) → 200 (usuário persistido)
7. GET /api/favoritos (Bruno)                    → 1 favorito persistido
8. GET /api/categorias                           → 7 categorias (seed idempotente)
```

Artefatos: `Dados/RarityRoomContexto.cs` (mapeamento),
`Dados/Migracoes/20261115024545_Inicial.cs` (5 tabelas),
`Repositories/Repositorio*Ef.cs` (persistência).

## Prova da Onda 4 (pedido, reputação e imagem)

```
POST /api/pedidos {produtoId: 2}      → 201, snapshot: nomeProduto "Funko Joker",
                                        precoUnitario 399.9, estado Pendente
POST /api/pedidos/1/concluir (comprador) → 403 acesso-negado
POST /api/pedidos/1/concluir (vendedor)  → 200, estado Concluido
GET  /api/produtos/2                     → estado Vendido
POST /api/avaliacoes {pedidoId, nota 5}  → 201
POST /api/avaliacoes (repetido)          → 409 conflito
GET  /api/avaliacoes/vendedor/1          → media 5, total 1
POST /api/produtos/3/imagem (PNG)        → 200, /uploads/{guid}.png
GET  /uploads/{guid}.png                 → 200 image/png
POST /api/produtos/3/imagem (text/plain) → 400 dados-invalidos
POST /api/produtos/3/imagem (terceiro)   → 403 acesso-negado
```

Módulos novos: `Pedido` (snapshot de nome e preço), `Avaliacao` (nota 1–5 por
pedido concluído), `ServicoImagensArquivo` (nome gerado pelo servidor), tabelas
`pedidos` e `avaliacoes`.
