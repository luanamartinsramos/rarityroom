# Controle de exceções

> Critérios: **D01** (três situações de falha, separando violação de regra e
> situação excepcional), **D02** (caminho reproduzível de lançamento e
> propagação até o tratamento), **D03** (tratamento adequado sem ocultar e sem
> vazar detalhes), **D04** (testes das três situações).

## 1. Política: nem todo problema é uma exceção

| Tipo de situação | Como o código trata | Exemplo |
|---|---|---|
| **Resultado normal, ainda que "vazio"** | **sem exceção**: devolve valor | busca sem resultado → `200 []`; vendedor sem avaliações → reputação `0 / 0`; favoritos de conta nova → lista vazia |
| **Violação de regra de negócio** (esperada, causada pelo pedido do usuário) | exceção **de domínio tipada** (`ExcecaoDominio` e derivadas), com código estável; vira `4xx`; log em nível `Warning` | transição proibida, dado inválido, acesso de terceiro |
| **Conflito técnico previsível** | exceção da infraestrutura capturada pelo nome; vira `409`; log `Warning` | `DbUpdateConcurrencyException` (duas compras do mesmo estoque) |
| **Situação excepcional** (não prevista; defeito ou falha de ambiente) | **não é tratada no local**: propaga até o middleware; vira `500` genérico; log `Error` com a exceção completa | banco indisponível, `NullReferenceException`, disco cheio |

Lançamos exceção para regra de negócio, em vez de devolver código de erro,
porque a violação acontece **dentro** da entidade (`Produto`, `Pedido`), a
várias chamadas de distância do controller. A exceção tipada interrompe a
operação **antes** de qualquer campo ser alterado. Assim, o objeto e o banco
nunca ficam em estado parcial.

**Hierarquia** (`backend/Models/Excecoes/`):

```
Exception
└── ExcecaoDominio (abstrata, propriedade Codigo)
    ├── ValidacaoException          400  dados-invalidos
    ├── AcessoNegadoException       403  acesso-negado
    ├── NaoEncontradoException      404  nao-encontrado
    ├── MuitasTentativasException   429
    └── ConflitoException           409  conflito
        ├── TransicaoInvalidaException  409  transicao-invalida
        └── PagamentoRecusadoException  409  pagamento-recusado
```

## 2. As três situações de falha (D01)

### FALHA-01 — Transição de estado proibida *(violação de regra de negócio, R2)*

| | |
|---|---|
| **Condição** | o dono pede `Vender` para um anúncio `Pausado` |
| **Classificação** | regra de negócio violada: um pedido válido do ponto de vista técnico, proibido pelo domínio |
| **Quem detecta** | `Produto.AplicarOperacao`: o par (`Pausado`, `Vender`) não existe em `Transicoes` |
| **Comportamento esperado** | `409 Conflict`, `codigo: "transicao-invalida"`, mensagem explicando o estado; **nenhuma alteração** no anúncio (continua `Pausado`, estoque igual); log `Warning` |
| **Teste** | `Falha01_VenderAnuncioPausado_Responde409_EEstadoContinuaPausado` (API + releitura do banco); `CancelarReserva_DePausado_DeveFalhar` (domínio) |

### FALHA-02 — Dado de entrada inválido *(violação de regra de negócio, RC-01)*

| | |
|---|---|
| **Condição** | `POST /api/produtos` com `preco: 0` |
| **Classificação** | regra de formação do anúncio violada (invariante "preço > 0") |
| **Quem detecta** | `Produto.AplicarDados`, chamado pelo construtor, **antes** de o objeto existir |
| **Comportamento esperado** | `400 Bad Request`, `codigo: "dados-invalidos"`, mensagem `"O preço deve ser maior que zero."`; nenhum anúncio gravado; log `Warning` |
| **Teste** | `Falha02_AnuncioComPrecoZero_Responde400_ENadaEGravado`; `Criar_ComDadosInvalidos_NaoDevePersistir` |

### FALHA-03 — Erro inesperado *(situação excepcional)*

| | |
|---|---|
| **Condição** | qualquer exceção não prevista: por exemplo, o SQLite não consegue abrir o arquivo do banco (`SQLite Error 14: unable to open database file`) |
| **Classificação** | excepcional: não é decorrência do pedido do usuário e não há como o usuário corrigi-la |
| **Quem detecta** | ninguém captura no local. A exceção sobe até `MiddlewareExcecoes`, no `catch (Exception)` |
| **Comportamento esperado** | `500`, `codigo: "erro-interno"`, mensagem genérica `"Ocorreu um erro inesperado. Tente novamente."`; **sem** tipo da exceção, caminho de arquivo, SQL ou stack trace na resposta; registro `LogError` com a exceção completa (a falha **não é ocultada**) |
| **Teste** | `Falha03_ErroInesperado_Responde500Generico_SemVazarDetalhes_ERegistraNoLog` |

## 3. Caminho reproduzível de lançamento e propagação (D02)

FALHA-01, percorrida pela pilha real:

```
PATCH /api/produtos/10/estado {"operacao":"Vender"}
  └─ ProdutosController.AlterarEstado          (sem try/catch)
      └─ ServicoAnuncios.AlterarEstado         (sem try/catch; valida dono — R1)
          └─ Produto.AplicarOperacao(Vender)
              └─ throw new TransicaoInvalidaException(...)   ← LANÇAMENTO
          ← propaga (nenhuma escrita foi feita: SaveChanges não é chamado)
      ← propaga
  ← propaga
MiddlewareExcecoes.InvokeAsync
  └─ catch (ExcecaoDominio)                    ← CAPTURA onde existe tratamento
      ├─ logger.LogWarning("Regra de negócio violada ({Codigo}) ...")
      └─ StatusDe(excecao) → 409 + ProblemDetails
```

**Reprodução executada em 2026-11-18** (servidor local, `curl`, conta e
anúncio criados só para o teste e excluídos em seguida):

```
POST  /api/autenticacao/cadastrar           → 200
POST  /api/autenticacao/entrar              → 200
POST  /api/produtos (preço 150, estoque 3)  → 201 (id 10)
PATCH /api/produtos/10/estado  Pausar       → 200
PATCH /api/produtos/10/estado  Vender       →
  HTTP/1.1 409 Conflict
  Content-Type: application/json; charset=utf-8
  {"title":"Não foi possível concluir a operação","status":409,
   "detail":"Não é possível vender um anúncio no estado Pausado.",
   "instance":"/api/produtos/10/estado","codigo":"transicao-invalida"}
GET   /api/produtos/10                      → estado Pausado, estoque 3  (inalterado)

POST  /api/produtos (preço 0)               →
  HTTP/1.1 400 Bad Request
  {"title":"Não foi possível concluir a operação","status":400,
   "detail":"O preço deve ser maior que zero.","instance":"/api/produtos",
   "codigo":"dados-invalidos"}
```

Log do servidor no mesmo instante:

```
Regra de negócio violada (transicao-invalida) em PATCH /api/produtos/10/estado
RarityRoom.Modelos.Excecoes.TransicaoInvalidaException: Não é possível vender um anúncio no estado Pausado.
```

## 4. Tratamento central sem ocultar nem vazar (D03)

`backend/Middleware/MiddlewareExcecoes.cs` é o **único** ponto de captura
genérica. Os controllers não têm `try/catch`.

| Bloco | Status | O que vai ao cliente | O que vai ao log |
|---|---|---|---|
| `catch (ExcecaoDominio)` | pelo tipo (`StatusDe`) | mensagem da regra + código | `Warning` com a exceção |
| `catch (DbUpdateConcurrencyException)` | 409 | mensagem fixa pedindo para tentar de novo | `Warning` com a exceção |
| `catch (Exception)` | 500 | **só** `"Ocorreu um erro inesperado. Tente novamente."` + `erro-interno` | `Error` com a exceção completa |

- **Não oculta:** a exceção nunca é engolida. Sempre há registro no log, e o
  status reflete a falha (500 não vira 200).
- **Não vaza:** a mensagem da exceção técnica não é copiada para a resposta.
  O Swagger, que descreve a API interna, só é ativado em `Development`.
- **Ordem importa:** `StatusDe` avalia `TransicaoInvalidaException` antes de
  `ConflitoException`, porque a especialização precisa ser reconhecida antes
  da classe base.

## 5. Testes das três situações (D04)

Arquivo `tests/RarityRoom.Tests/Api/FalhasApiTests.cs`:

| Teste | Verifica a resposta | Verifica o estado |
|---|---|---|
| `Falha01_VenderAnuncioPausado_Responde409_EEstadoContinuaPausado` | 409, `transicao-invalida` | anúncio relido do banco: `Pausado`, estoque 3 |
| `Falha02_AnuncioComPrecoZero_Responde400_ENadaEGravado` | 400, `dados-invalidos`, mensagem cita o preço | catálogo vazio: nada foi persistido |
| `Falha03_ErroInesperado_Responde500Generico_SemVazarDetalhes_ERegistraNoLog` | 500, `erro-interno`, mensagem genérica; corpo **não contém** "SQLite", "/data/" nem o nome da exceção | exatamente 1 registro de log, nível `Error`, com a `InvalidOperationException` original |

```
$ dotnet test --filter FalhasApiTests
Aprovado! – Com falha: 0, Aprovado: 3, Total: 3
```
