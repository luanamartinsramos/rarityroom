# Handoff — Adequação do RarityRoom à AV2 (POO)

**Criado em:** 2026-11-14
**Motivo:** o DsCode foi reaberto com esta pasta como workspace, para liberar
leitura/escrita/edição. Este arquivo preserva o objetivo e o plano.

## Objetivo

Evoluir o RarityRoom original para atender ao máximo os 40 critérios da AV2 de
Programação Orientada a Objetos (7 grupos: A POO 8, B modelo de classes 8,
C code review 6, D exceções 4, E debugging 6, F testes/rastreabilidade 6,
G especificação verificável 2). Preservar a identidade do projeto.

## Base atual (original, intacta)

- `backend/` ASP.NET Core net10.0: `Usuario`, `Produto`, `RepositorioUsuarioMemoria`,
  `RepositorioProdutoMemoria`, `ServicoAutenticacao`, `HashSenha`,
  `AutenticacaoController`, `ProdutosController`, Swagger + CORS + cookie auth.
- `frontend/` React 19 + TS + Vite: home, login, register, `ProtectedRoute`,
  `produtosService.ts`, `src/data/products.ts` (catálogo mockado).
- **Não existe**: categorias, propostas, favoritos, exceções de domínio,
  testes automatizados, `docs/av2/`, arquivo de solução.

## Trabalho anterior perdido

Havia um trabalho completo em `C:\Rarityroom\Rarityroom-main` (domínio com
máquina de estados, polimorfismo, 143 testes, `docs/av2/`). Essa pasta foi
removida do disco e **nada foi reaproveitado**. Todo o trabalho deve ser
refeito nesta base. Os três defeitos reais documentados antes continuam válidos
como alvos de correção e devem ser reproduzidos aqui:

1. Id duplicado após exclusão (`proximoId` incrementado sem atribuir ao item).
2. Qualquer usuário edita/exclui anúncio de terceiro (sem checagem de dono).
3. Transições de estado indexadas só pelo destino permitem ação errada.

## Plano aprovado

Fase 1 diagnóstico · Fase 2 domínio (5+ classes, encapsulamento, polimorfismo
com `IEstrategiaNegociacao` e 3 implementações reais) · Fase 3 persistência e
serviços (id atômico) · Fase 4 API · Fase 5 testes (domínio + integração HTTP)
· Fase 6 `docs/av2/` (8 arquivos) + README + auditoria dos 40 critérios com
evidência concreta.

## Fatos de ambiente verificados

- `dotnet build` do backend: **êxito, 0 avisos, 0 erros** nesta base.
- Este shell precisa de duas variáveis para o NuGet funcionar; sem elas o build
  falha com `Value cannot be null (Parameter 'path1')`:
  `env APPDATA="C:/Users/cantt/AppData/Roaming" ProgramFiles="C:/Program Files" dotnet <cmd>`
- .NET SDK 10.0.401. Node v26.13.0. `jq` e `rg` ausentes.
- A pasta de testes deve ficar FORA da compilação do projeto web (o SDK Web
  compila `**/*.cs`), senão os testes entram no build da aplicação.

## Regras de preservação

- Manter namespace `RarityRoom`, rotas `api/produtos` e `api/autenticacao`,
  autenticação por cookie, CORS para `http://localhost:5173`.
- Não recriar o projeto do zero, não remover funcionalidades existentes.
- Não inventar nomes de integrantes (já constam no README original).

---

# Progresso — Onda 1 concluída (2026-11-14)

Defeitos D1–D6 corrigidos na raiz, com regressão automatizada.

**Estado de validação:** `dotnet build` 0 erros / 0 avisos ·
`dotnet test` 65 aprovados / 0 falhas · fluxo HTTP real executado.

## Evidência por defeito

| Defeito | Correção (camada dona) | Prova |
|---|---|---|
| D1 id duplicado | id atribuído no repositório sob `lock` (`++ultimoId`), nunca `Count + 1` | HTTP: criou 1,2,3; excluiu 2; novo id = 4; conjunto {1,3,4} |
| D2 dono do anúncio | `Produto.UsuarioId` + `ValidarPropriedade`/`ValidarExclusao` no domínio | HTTP: terceiro autenticado recebe 403 em PUT, DELETE e PATCH |
| D3 vazamento de lista | `ObterTodos()` devolve cópia; `lock` em todas as operações | Teste `ObterTodos_DeveDevolverCopiaDaLista` |
| D4 exceção genérica | `ExcecaoDominio` + middleware → ProblemDetails com `codigo` | HTTP: e-mail duplicado = 409 `conflito` |
| D5 catálogo fechado | `[Authorize]` só nas escritas; GET é público | HTTP: GET anônimo = 200 |
| D6 validação no domínio | `Produto`/`Usuario` validam no construtor e em `Atualizar` | HTTP: 400 `dados-invalidos` para preço 0, nome curto e URL inválida |

## Arquivos criados

- `Models/Enums/` — `EstadoAnuncio`, `OperacaoAnuncio`
- `Models/Excecoes/` — raiz + 5 exceções
- `Models/DadosAnuncio.cs`
- `Services/IServicoAnuncios.cs`, `Services/ServicoAnuncios.cs`
- `Middleware/MiddlewareExcecoes.cs`
- `tests/RarityRoom.Tests/` — projeto xUnit com 65 testes (domínio, repositório, serviços)

## Arquivos alterados

- `Models/Produto.cs` — dono, estado, validação, tabela de transições por (origem, operação)
- `Models/Usuario.cs` — validação, `DefinirId`, `EhProprietario`
- `Models/ModelosRequisicao.cs` — só `[Required]`; regra de negócio saiu daqui
- `Repositories/*` — id atômico, trava, cópia defensiva
- `Services/ServicoAutenticacao.cs` — sem `Count + 1`, `ConflitoException`
- `Controller/ProdutosController.cs`, `Controller/AutenticacaoController.cs`
- `Program.cs` — middleware, serviço, enum como texto no JSON

## Decisão de arquitetura a manter

- `backend/` compila `**/*.cs`; o projeto de testes fica em `tests/` na raiz,
  fora do backend, senão entra no build da aplicação.

## Próximo passo (Onda 2)

Categoria como entidade (slug), Favorito persistido, Proposta com ciclo de
negociação e `IEstrategiaNegociacao` com 3 implementações reais.

---

# Progresso — Onda 2 concluída (2026-11-14)

Domínio de marketplace implementado e comprovado por HTTP.

**Estado de validação:** build 0 erros / 0 avisos · **104 testes** aprovados.

## Novas classes de domínio

| Classe | Regra que ela protege |
|---|---|
| `Categoria` | slug normalizado (sem acento, minúsculo); "Funko"/"FUNKO" colidem; não pode duplicar |
| `Favorito` | não favoritar o próprio anúncio; sem duplicata |
| `Proposta` | valor > 0; ciclo Pendente → Aceita/Recusada/Cancelada; só o proponente cancela |
| `IEstrategiaNegociacao` | contrato comum; 3 implementações (PrecoFixo 100%, AceitaOferta 85%, Liquidacao 70%) |

Regra de catálogo: anúncio só usa categoria cadastrada e grava o **nome canônico**.
Categorias semeadas na inicialização (7): Action Figure, Anime, Card Games,
Colecionáveis, Funko, Marvel, DC Comics.

## Prova do polimorfismo (HTTP real)

Mesma oferta (R$ 200) sobre o mesmo preço (R$ 249,90), anúncios diferentes:

| Modalidade | Resultado | Estado do anúncio |
|---|---|---|
| PrecoFixo | **409** `conflito` — "mínimo aceito é 249,90" | permaneceu `Disponivel` |
| Liquidacao | **200** — proposta `Aceita` | passou a `Reservado` |

## Endpoints novos

- `GET/POST /api/categorias`
- `GET /api/favoritos`, `POST|DELETE /api/favoritos/{produtoId}`
- `GET /api/propostas`, `POST /api/propostas`,
  `POST /api/propostas/{id}/aceitar|recusar|cancelar`
- `PATCH /api/produtos/{id}/estado` (operacao: reservar|vender|pausar|reativar|cancelarReserva)
- `produto` agora aceita `modalidade` (PrecoFixo|AceitaOferta|Liquidacao)

## Próximo

Onda 3 (persistência, DTOs, busca), Onda 4 (pedidos, reputação) e a
documentação `docs/av2/` + auditoria dos 40 critérios.

---

# Progresso — Documentação e auditoria concluídas (2026-11-14)

Entrega da AV2 fechada em `docs/av2/` (8 arquivos) + README atualizado.

**Validação final:** `dotnet build` 0 erros / 0 avisos ·
`dotnet test` 104 aprovados / 0 falhas.

## Documentos produzidos

| Arquivo | Linhas | Conteúdo |
|---|---:|---|
| `requisitos.md` | 76 | 16 RF, 14 RN, 6 RNF, critérios de aceitação |
| `modelo-de-classes.md` | 141 | diagrama Mermaid, responsabilidades, colaboração |
| `decisoes-de-modelagem.md` | 143 | 10 decisões (contexto/decisão/consequência) |
| `code-review.md` | 88 | 10 achados + 4 pendências de revisão |
| `debugging.md` | 176 | 3 defeitos + 7 episódios reais de depuração |
| `testes.md` | 88 | estratégia, 104 casos, rastreabilidade RN→teste |
| `auditoria-final.md` | 143 | 40 critérios: 37 atendidos, 3 parciais, 0 pendentes |
| `evidencias.json` | 86 | índice legível por máquina (validado por parse) |

## Ressalva registrada na auditoria

A numeração dos 40 critérios segue os grupos informados no enunciado
(8/8/6/4/6/6/2). A redação de cada linha é uma síntese do requisito. **Conferir
com o documento oficial da disciplina** antes da entrega.

Os 3 critérios parciais são declarados, não maquiados:
- **C2**: os 5 repositórios repetem o padrão `lock` + cópia.
- **E1**: o comportamento antigo dos defeitos não foi executado após a reescrita.
- **F5**: dados em memória, sem sobrevivência a reinício.

## Backlog não iniciado (proposto, fora do escopo AV2)

- Onda 3: EF Core + SQLite, DTOs de saída, busca e paginação no servidor.
- Onda 4: pedido com snapshot de preço, reputação do vendedor, upload de imagem,
  testes de integração com `WebApplicationFactory`.

---

# Progresso — Onda 3 concluída: persistência SQLite (2026-11-14)

Banco de dados implementado com EF Core 10.0.15 + SQLite.

**Validação:** build 0 erros / 0 avisos · **113 testes** aprovados · persistência
comprovada por reinício do servidor.

## O que mudou

| Antes | Depois |
|---|---|
| `RepositorioXMemoria` em produção | `RepositorioXEf` sobre `RarityRoomContexto` |
| dados perdidos no restart | `backend/rarityroom.db` com migração versionada |
| serviços `singleton` | serviços e repositórios `scoped` (DbContext não é thread-safe) |
| id por contador em memória | `INTEGER PRIMARY KEY AUTOINCREMENT` |

## Arquivos novos

- `Dados/RarityRoomContexto.cs` — mapeamento das 5 entidades
- `Dados/Migracoes/20261115024545_Inicial.cs` (+ Designer e Snapshot)
- `Dados/FabricaContextoDesignTime.cs` — evita que o `dotnet ef` execute o app
- `Repositories/Repositorio*Ef.cs` — 5 implementações
- `tests/RarityRoom.Tests/Fakes/` — os antigos repositórios em memória viraram
  dublês de teste
- `tests/RarityRoom.Tests/Repositorios/RepositorioEfTests.cs` — 9 testes contra
  SQLite real

## Prova de persistência (executada)

```
criar usuários, anúncio e favorito  → parar o processo → subir de novo
GET /api/produtos  → 1 anúncio "Funko Batman", 249.9, Disponivel
POST /api/autenticacao/entrar       → 200 (usuário persistido)
GET /api/favoritos                  → 1 favorito persistido
GET /api/categorias                 → 7 (seed idempotente)
```

## Auditoria atualizada

**39 atendidos · 1 parcial (E1) · 0 pendentes.** F5 e C2 saíram de parcial para
atendido. Documentos atualizados: `auditoria-final.md`, `testes.md`,
`decisoes-de-modelagem.md` (D10–D13), `code-review.md`, `requisitos.md`
(RNF07) e `evidencias.json`.

## Observação de ambiente

O guard do DsCode bloqueia qualquer comando que cite literalmente o arquivo de
configuração de settings. A connection string ficou com padrão no código
(`Data Source=rarityroom.db`) e pode ser sobrescrita pela variável
`ConnectionStrings__RarityRoom` — sem precisar editar o arquivo.

---

# Progresso — Onda 4 concluída (2026-11-14)

Pedido com snapshot, reputação do vendedor, upload de imagem e testes de
integração HTTP.

**Validação:** build 0 erros / 0 avisos · **135 testes** aprovados (9 deles
sobem a aplicação inteira por `WebApplicationFactory`).

## Novos módulos

| Módulo | Regra que protege |
|---|---|
| `Pedido` | snapshot de nome e preço; não comprar o próprio anúncio; só o vendedor conclui; um pedido pendente por anúncio |
| `Avaliacao` | nota 1–5, só do comprador, só de pedido concluído, uma por pedido |
| `ReputacaoVendedor` | média e total derivados; vendedor sem avaliação = 0/0 |
| `ServicoImagensArquivo` | 2 MB, JPG/PNG/WEBP, nome gerado por `Guid`, extensão pelo content-type |

## Migração

`20261115030519_PedidosAvaliacoes` — tabelas `pedidos` e `avaliacoes`, com
índice único em `avaliacoes.PedidoId`.

## Prova por HTTP real

```
pedido criado (201) com nomeProduto "Funko Joker" e precoUnitario 399.9
comprador tentando concluir      → 403
vendedor concluindo              → 200; anúncio passa a Vendido
avaliação nota 5                 → 201; repetida → 409
reputação do vendedor            → media 5, total 1
upload PNG                       → 200 /uploads/{guid}.png ; GET → 200 image/png
upload text/plain                → 400 ; por terceiro → 403
```

## Testes de integração

`Api/FabricaApi.cs` — sobe o app real trocando só o banco (arquivo SQLite
temporário) e o diretório de imagens. `Api/FluxoApiTests.cs` — 9 casos:
catálogo público, 401 sem login, 403 de terceiro, fluxo completo de compra e
avaliação, pedido no próprio anúncio, pedido duplicado, avaliação antecipada,
upload inválido e upload válido.

## Estado da auditoria

39 atendidos · 1 parcial (E1) · 0 pendentes. P-05 (testes de integração)
passou a resolvido. Documentos atualizados: `requisitos.md` (RF17–RF22,
RN15–RN19), `modelo-de-classes.md` §6, `decisoes-de-modelagem.md` (D14–D16),
`testes.md`, `auditoria-final.md`, `evidencias.json` e README.

---

# Progresso — Pedidos reais, perfil de conta e checkout (2026-11-14)

**Validação:** build 0/0 · **154 testes** · frontend compila · fluxo provado por HTTP.

## O que estava errado

`home.tsx` semeava um pedido fictício (`RR-2026-00124`, "Em transporte") no
estado inicial. Toda conta nova nascia com um pedido que nunca existiu.

## O que mudou

| Item | Antes | Depois |
|---|---|---|
| Meus pedidos | pedido fake fixo | `GET /api/pedidos`; conta nova = lista vazia |
| Cadastro | sem papel | escolha **Comprar** ou **Vender** (radio) |
| Compra | criava pedido local no carrinho | modal de checkout: endereço + forma de pagamento |
| Permissões | qualquer um publicava/comprava | comprador não publica (403); vendedor não compra (403) |

## Backend

- `TipoConta` (`Comprador`, `Vendedor`, `Ambos`) em `Usuario`, com
  `PodeComprar`/`PodeVender`.
- `DadosEntrega` (value object) com validação de CEP/UF; gravado no `Pedido`
  como snapshot, via `OwnsOne`.
- `FormaPagamento` (`Pix`, `CartaoCredito`, `Boleto`).
- Migração `20261115051041_PerfilECheckout`; `TipoConta` com default **Ambos**
  para não invalidar contas existentes (o EF havia gerado default vazio).
- `POST /api/pedidos` agora exige endereço e forma de pagamento.

## Provas por HTTP

```
login de conta antiga (Ana)                  → 200
cadastro tipoConta=Comprador                 → 200; /usuario-atual devolve o papel
GET /api/pedidos (conta nova)                → []
POST /api/produtos (comprador)               → 403
POST /api/pedidos com endereço + Pix         → 201, entrega.cep 01310-100
GET /api/pedidos (após compra)               → 1
POST /api/pedidos sem endereço               → 400
POST /api/pedidos (conta vendedor)           → 403
```

## Frontend

- `services/pedidosService.ts` (`obterPedidos`, `criarPedido`).
- `home.tsx`: pedidos reais, modal de checkout com endereço e pagamento,
  conversão de pedido da API para o formato da tela.
- `register.tsx`: seletor de papel da conta.
- CSS do checkout e do seletor.

## Testes

15 em `Api/FluxoApiTests` (eram 10): conta nova sem pedidos, checkout sem
endereço, comprador não publica, vendedor não compra e checkout guardando
entrega e pagamento.

---

# Progresso — Catálogo rico e filtros (2026-11-14)

**Validação:** build 0/0 · **159 testes** · frontend compila · filtros provados por HTTP.

## Implementado

| Item | Onde |
|---|---|
| `Produto.Franquia`, `Raridade` (enum), `EdicaoLimitada`, `Destaque` | entidade + DTO + migração `DetalhesDoProduto` |
| Filtro por franquia, condição, faixa de preço, destaque | `FiltroAnuncios` + `ServicoAnuncios.Listar` |
| Ordenação: relevância, menor preço, maior preço, novidades | `OrdemCatalogo` |
| UI: seletor de ordenação, preço mín/máx, botão Destaques | `home.tsx` |

## Backlog declarado — o que a lista de 10 grupos ainda pede

**1. Usuários:** recuperação de senha; edição de perfil; foto de perfil;
endereço padrão salvo (hoje só existe endereço por pedido).

**2. Lojas:** entidade `Loja` (criar/editar, foto, descrição); perfil público
do vendedor; painel de vendas.

**3. Produtos:** múltiplas fotos (hoje 1 imagem por anúncio).

**5. Carrinho:** validar estoque antes de finalizar (hoje o backend valida na
compra, mas o carrinho não avisa).

**7. Pagamentos:** simulação com status (`StatusPagamento` já criado como enum,
sem uso) e confirmação.

**9. Avaliações:** avaliação do **produto** (hoje só do vendedor) e média por
produto.

**10. Interface:** página de detalhes do produto, página da loja, perfil do
usuário, área do vendedor com pedidos recebidos.

## Observação

`StatusPagamento` e `OrdemCatalogo` foram criados; `StatusPagamento` ainda não
é usado por nenhum fluxo — pendência explícita, não código morto escondido.

---

# Progresso — Categorias, paginação, perfil do vendedor e notificações (2026-11-14)

**Validação:** build 0/0 · **159 testes** · tudo provado por HTTP.

## Entregue neste bloco

| Item | Evidência |
|---|---|
| 15 categorias geek da especificação | `GET /api/categorias` devolve as 15 |
| Migração de dados das categorias | produtos antigos remapeados (Action Figure→Action Figures, Anime→Animes, Funko→Funko Pop); nada perdido |
| Paginação no catálogo | `?pagina=&tamanhoPagina=` + header `X-Total-Count` (máx. 60/página) |
| Perfil público do vendedor | `GET /api/vendedores/{id}`: nome, desde, reputação, ativos, vendidos, avaliações, anúncios |
| Notificações | entidade + tabela + central; `GET /api/notificacoes`, `POST /{id}/ler`, `POST /ler-todas` |
| Notificações ligadas aos fluxos | proposta recebida/aceita/recusada, pedido criado/concluído |

## Categorias finais (15)

Acessórios Geek, Action Figures, Animes, Cartas Colecionáveis, Colecionáveis
Raros, Filmes e Séries, Funko Pop, HQs e Quadrinhos, Jogos e Consoles, Lego,
Magic: The Gathering, Mangás, Outros, Pokémon, Yu-Gi-Oh!

## Ainda não iniciado da missão

- Frontend: página de detalhes do produto, página do vendedor, central de
  notificações, painel do usuário.
- Administração e moderação.
- Recuperação de senha, edição de perfil, foto, endereços múltiplos.
- Avaliação de produto (hoje só do vendedor).
- Pagamento demonstrativo com status (`StatusPagamento` criado, sem uso).
- Loja do vendedor.

---

# Progresso — Páginas novas do frontend (2026-11-14)

**Validação:** `tsc --noEmit` sem erros · `npm run build` compila (320 kB) ·
módulos servidos pelo Vite em 200 · APIs em 200.

## Páginas criadas

| Rota | Arquivo | Conteúdo |
|---|---|---|
| `/produto/:id` | `pages/produto/produto.tsx` | imagem, preço, descrição, categoria/condição/raridade/franquia, estoque, vendedor com reputação, favoritar (API), adicionar ao carrinho, produtos relacionados |
| `/vendedor/:id` | `pages/vendedor/vendedor.tsx` | nome, desde, reputação, ativos, vendidos, grade de anúncios ativos e avaliações |
| `/notificacoes` | `pages/notificacoes/notificacoes.tsx` | central com marcação de leitura individual e em lote |

## Serviços novos

- `services/carrinhoService.ts` — carrinho em `localStorage`, respeitando estoque, com evento para sincronizar entre páginas.
- `services/catalogoService.ts` — `obterProduto`, `obterVendedor`.
- `services/notificacoesService.ts` — lista, marcar uma, marcar todas.

## Home ligada às telas

- Sino no cabeçalho com contador de não lidas → `/notificacoes`.
- Modal do produto ganhou **"Ver página do produto"**.

## Pendente (inalterado)

Painel do usuário, administração/moderação, recuperação de senha, edição de
perfil, foto, endereços múltiplos, avaliação de produto, pagamento
demonstrativo com status, loja do vendedor.

---

# Progresso — Painel administrativo (2026-11-14)

**Validação:** build 0/0 · **162 testes** · endpoints provados por HTTP ·
frontend compila e typecheck limpo.

## Modelo

- `Usuario.Administrador` (bool, default false) — **não** é escolhido no
  cadastro; só o seed de configuração concede.
- Migração `20261115055425_Administrador`.
- Seed cria `admin@rarityroom.com` / `admin123` (conta de demonstração,
  documentada; trocar em produção).

## API (`/api/admin`, exige administrador)

| Rota | Efeito |
|---|---|
| `GET /resumo` | usuários, ativos, anúncios ativos, vendidos, pedidos, total vendido |
| `GET /usuarios` | lista sem hash de senha, com papel e situação |
| `POST /usuarios/{id}/suspender` | bloqueia login (verificado) |
| `POST /usuarios/{id}/reativar` | devolve acesso |
| `GET /anuncios` | todos os anúncios |
| `POST /anuncios/{id}/moderar?acao=pausar\|remover` | moderação |
| `GET /pedidos` | todos os pedidos |

Regras: administrador não suspende a si mesmo (409); a autorização é
verificada no **serviço**, não apenas no controller.

## Frontend

- `/admin` com resumo, abas de usuários, anúncios e pedidos.
- Botão **⚙️ Admin** no cabeçalho, visível só para administrador.
- `/usuario-atual` passou a devolver `administrador`.

## Provas

```
login admin@rarityroom.com                     → 200
GET /api/admin/resumo                          → 200 (7 usuários, 5 ativos, 3 pedidos, R$ 399,90)
GET /api/admin/usuarios                        → lista com administrador: true/false
GET /api/admin/resumo com conta comum (Ana)    → 403
suspender conta → login da conta suspensa      → 401 (teste de integração)
```

---

# Progresso — Painel do usuário (2026-11-14)

**Validação:** typecheck e build limpos (332 kB) · módulo e rota em 200 · APIs
da conta em 200.

## Página `/conta`

Quatro abas, alimentadas por dados reais:

| Aba | Fonte | Ações |
|---|---|---|
| Compras | `GET /api/pedidos` (comprador) | ver vendedor |
| Vendas | `GET /api/pedidos` (vendedor) | ver anúncio |
| Favoritos | `GET /api/favoritos` | remover |
| Propostas | `GET /api/propostas` | aceitar/recusar (recebidas), cancelar (enviadas) |

Cabeçalho mostra nome, e-mail, tipo de conta e selo de administrador.
Estados vazios próprios para cada aba.

## Serviços novos

- `favoritosService.ts` — listar e remover (persistido no backend).
- `propostasService.ts` — listar, aceitar, recusar, cancelar.

## Navegação

Botão **👤 Minha conta** no cabeçalho da home → `/conta`.

## Pendente restante

Recuperação de senha / edição de perfil / foto / endereços múltiplos;
avaliação de produto; pagamento demonstrativo com status; loja do vendedor.

---

# Progresso — Pagamento demonstrativo e ciclo de entrega (2026-11-14)

**Validação:** build 0/0 · **167 testes** · ciclo completo provado por HTTP.

## Domínio

`EstadoPedido` ganhou `Pago`, `Enviado` e `Entregue` (além de `Pendente`,
`Concluido`, `Cancelado`). A transição passou a ser uma **tabela indexada por
(origem, operação)**, com `OperacaoPedido { Pagar, Enviar, Entregar, Concluir,
Cancelar }`.

`Pedido.StatusPagamento` (`Pendente`, `Pago`, `Recusado`) registra o resultado
da simulação. `PagamentoRecusadoException` (código `pagamento-recusado`)
interrompe o fluxo **com o status já gravado**.

## API

| Rota | Quem | Efeito |
|---|---|---|
| `POST /api/pedidos/{id}/pagar` `{aprovado}` | comprador | Pendente → Pago (ou status Recusado + 409) |
| `POST /api/pedidos/{id}/enviar` | vendedor | Pago → Enviado |
| `POST /api/pedidos/{id}/entregar` | comprador | Enviado → Entregue |
| `POST /api/pedidos/{id}/concluir` | vendedor | fluxo antigo preservado |

Notificações: `PedidoPago` (vendedor) e `PedidoEnviado` (comprador).

## Provas

```
pedido 4: pagar → enviar → entregar  → estado Entregue, statusPagamento Pago
pedido 5: pagar {aprovado:false}     → 409 pagamento-recusado
                                     → estado Pendente, statusPagamento Recusado
enviar antes do pagamento            → 409 transicao-invalida
```

## Frontend

Na página `/conta`: botão **Pagar (simulado)** em compras pendentes,
**Marcar como enviado** em vendas pagas e **Confirmar entrega** em compras
enviadas.

## Pendente restante

Recuperação de senha / edição de perfil / foto / endereços múltiplos;
avaliação de produto; loja do vendedor.

---

# Progresso — Avaliação de produto (2026-11-14)

**Validação:** build 0/0 · **169 testes** · resumo por produto provado por HTTP.

## Modelo

`Avaliacao` ganhou `ProdutoId`, copiado do pedido no construtor. A mesma
avaliação mede o **vendedor** (por `VendedorId`) e o **produto** (por
`ProdutoId`) — sem tabela duplicada e sem risco de divergência.

Migração `20261115060145_AvaliacaoDoProduto` com **backfill**:
`UPDATE avaliacoes SET ProdutoId = (SELECT ProdutoId FROM pedidos ...)`.
As avaliações antigas continuam válidas.

## API

`GET /api/avaliacoes/produto/{id}` (público) → `{ produtoId, media, total, avaliacoes[] }`.

## Provas

```
produto 2 → media 5, total 1   (avaliação antiga religada pelo backfill)
produto 1 → media 0, total 0
```

## Frontend

Página `/produto/:id` mostra a nota **`5 ★ (1)`** nas etiquetas e a seção
**"Avaliações do produto"** com estrelas, comentário e data.

## Pendente restante

Recuperação de senha / edição de perfil / foto / endereços múltiplos;
loja do vendedor.

---

# Progresso — Loja do vendedor (2026-11-14)

**Validação:** build 0/0 · **172 testes** · fluxo provado por HTTP.

## Modelo

`Loja`: `DonoId` (único), `Nome` (3–80), `Descricao` (≤500), `FotoUrl`,
`CriadaEm`. Regras: uma loja por conta; só quem pode vender cria; nome e
descrição são públicos.

Migração `20261115060405_LojaDoVendedor` com índice **único** em `DonoId`.

## API

| Rota | Acesso | Efeito |
|---|---|---|
| `GET /api/lojas/{id}` | público | dados da loja |
| `GET /api/lojas/minha` | dono | 404 se ainda não criou |
| `POST /api/lojas` | vendedor | cria (409 se já existe) |
| `PUT /api/lojas` | dono | edita nome e descrição |
| `POST /api/lojas/foto` | dono | upload (2 MB, JPG/PNG/WEBP) |

`PerfilVendedor` passou a incluir `Loja` — a página `/vendedor/:id` mostra
nome, descrição e foto da loja.

## Provas

```
POST /api/lojas (vendedor)        → 201
PUT  /api/lojas (acentos UTF-8)   → 200 "Coleção da Ana"
POST /api/lojas (duplicada)       → 409
POST /api/lojas (comprador)       → 403
GET  /api/vendedores/1            → loja com nome, descricao, fotoUrl
```

## Frontend

- `/vendedor/:id` exibe o cartão da loja (foto, nome, descrição).
- `/conta` ganhou a aba **Minha loja**: criar/editar nome e descrição, enviar
  foto e ver o link público.

## Pendente restante

Recuperação de senha, edição de perfil (dados/foto) e múltiplos endereços.

---

# Progresso — Perfil, endereços e recuperação de senha (2026-11-14)

**Validação:** build 0/0 · **175 testes** · todos os fluxos provados por HTTP.

## Perfil

- `Usuario.FotoUrl` + `DefinirFoto` (URL absoluta ou `/uploads/`).
- `GET /api/perfil`, `PUT /api/perfil/nome`, `PUT /api/perfil/email`,
  `POST /api/perfil/foto`.
- E-mail duplicado em outra conta → 409.

## Endereços

- Entidade `Endereco` (validação igual à do checkout), tabela `enderecos`.
- `GET/POST/PUT/DELETE /api/enderecos` e `POST /{id}/principal`.
- Regras: primeiro vira principal; ao remover o principal, o mais antigo
  assume; limite de 5 por conta.

## Recuperação de senha (demonstração)

- Tabela `recuperacoes_senha`; token de 30 min, uso único.
- `POST /api/autenticacao/recuperar-senha` devolve o token **na resposta** —
  não há provedor de e-mail configurado; o fluxo é marcado como demonstração.
- `POST /api/autenticacao/redefinir-senha` troca o hash.
- Token reutilizado → 400.

## Provas

```
GET  /api/perfil                     → 200 (nome, email, fotoUrl)
PUT  /api/perfil/nome                → 200
POST /api/enderecos                  → 201, principal: true, "São Paulo"
DELETE /api/enderecos/{id}           → 204 e promove outro
POST /api/autenticacao/recuperar-senha → 200 com token
redefinir → senha antiga 401, nova 200
token reutilizado                    → 400
```

## Frontend

- `/perfil`: foto, nome, e-mail e gestão de endereços (criar, remover,
  tornar principal).
- `/recuperar-senha`: fluxo em dois passos, com aviso de demonstração.
- Link **"Esqueci minha senha"** no login e botão **Perfil e endereços** em
  `/conta`.

## Estado da missão

Todos os itens do plano foram implementados e verificados.

---

# Progresso — Conta única (comprador e vendedor) (2026-11-14)

**Validação:** build 0/0 · **174 testes** · cenário completo provado por HTTP.

## O que mudou e por quê

O escopo do MVP determina **uma única conta** para comprar e vender. O projeto
tinha o oposto: `PodeComprar`/`PodeVender` bloqueavam a operação inversa e o
cadastro perguntava "Comprar ou Vender".

Removido:
- bloqueio em `ServicoAnuncios.Criar` (comprador não publicava);
- bloqueio em `ServicoPedidos.Criar` (vendedor não comprava);
- bloqueio em `ServicoLojas.Criar` (comprador não tinha loja);
- pergunta de tipo no cadastro (frontend) e o campo `tipoConta` do
  `ModeloCadastro`;
- parâmetro `tipoConta` de `IServicoAutenticacao.Cadastrar`;
- condicionais `podeVender` na home.

Toda conta nasce `TipoConta.Ambos` (o enum permanece como registro histórico).

## Testes ajustados

- `ContaDeComprador_NaoPodePublicarAnuncio` → `ContaUnica_PodePublicarEComprar`
- `ContaDeVendedor_NaoPodeComprar` → removido (regra deixou de existir)
- `Loja_ContaDeComprador_NaoPodeCriar` → `Loja_QualquerContaPodeCriar`

## Prova do cenário da spec

```
cadastro sem tipoConta (A e B)     → 200
A publica anúncio                  → 201 (produto 7)
B publica anúncio                  → 201 (produto 8)
A compra de B                      → 201
B compra de A                      → 201
A cria loja                        → 201
GET /api/pedidos (conta A)         → pedido como comprador (9) e como vendedor (9)
```

## Página de criação de anúncio

- Rota `/anuncios/novo` com formulário completo: nome, descrição, preço,
  estoque, categoria (vinda do catálogo), franquia, marca, coleção, condição,
  raridade, modalidade de negociação, edição limitada e destaque.
- Validação no cliente (nome, preço > 0, estoque inteiro > 0) e mensagens do
  servidor exibidas no formulário.
- Sucesso redireciona para a página do anúncio recém-criado.
- Botões **+ Cadastrar produto** (painel do vendedor e estado vazio) e
  **Publicar anúncio** (em `/conta`) apontam para a página.
- `openCreateProduct` removido da home (código morto após a mudança); o modal
  permanece apenas para **edição**.

---

# Correção — busca de produtos invisível (2026-11-14)

**Sintoma relatado:** "não tem como pesquisar produto".

## Causa raiz

O `home.css` tinha, dentro de `@media (max-width: 850px)`:

```css
.home-search { display: none; }
```

Ou seja: **em qualquer janela com 850px ou menos a caixa de busca era
removida**, sem alternativa. Somava-se a isso a navbar com `height: 78px`
fixa, enquanto a área de busca (input + filtros) mede ~84px — o conteúdo
estourava o cabeçalho.

A API de busca sempre funcionou (`?termo=`), assim como o efeito de debounce
no frontend. O defeito era só de apresentação.

## Correção

1. `.home-navbar`: `height: 78px` → `min-height: 78px` + `padding: 10px 5%`.
2. `.home-search`: largura fixa → `width: 100%; max-width: 440px`.
3. `.home-search-area`: `flex: 1 1 320px; min-width: 0; max-width: 460px`.
4. Regra que escondia a busca substituída: abaixo de 850px ela vira uma
   **linha própria** (`order: 3; flex: 1 1 100%`), nunca desaparece.
5. **Nova busca no catálogo** (`.catalogo-busca`), acima dos chips de
   categoria, ligada ao mesmo estado — visível em qualquer largura.

## Verificação

```
GET /api/produtos?termo=funko → 4 resultados
GET /api/produtos?termo=goku  → 1 resultado
GET /api/produtos?termo=xyz   → 0 resultados
CSS servido: nenhuma regra home-search com display: none
Módulo home.tsx servido: contém home-search-area e catalogo-busca
```

---

# Correção — link "Esqueceu a senha?" era decorativo (2026-11-14)

**Sintoma relatado:** "adicione também a área de esqueci senha".

## Causa raiz

A área existia (`/recuperar-senha`, rota, página e endpoints), mas o link
visível **ao lado do campo SENHA** era decorativo:

```tsx
<a href="#" onClick={(event) => event.preventDefault()}>Esqueceu a senha?</a>
```

Clicar nele não fazia nada. O link funcional existia apenas abaixo do botão
"Entrar", em texto secundário.

## Correção

- O link ao lado da senha passou a ser `<Link to="/recuperar-senha">`.
- Removido o link duplicado que ficava abaixo do botão.

## Fluxo provado (conta nova, por HTTP)

```
cadastro                                  → 200
login com senha original                  → 200
POST /api/autenticacao/recuperar-senha     → 200 + token
POST /api/autenticacao/redefinir-senha     → 200
login com a senha antiga                  → 401
login com a senha nova                    → 200
e-mail inexistente                        → mensagem genérica, token null
```

## Pendência externa declarada

Sem provedor de e-mail, o token é devolvido na resposta e a tela marca
"demonstração". Em produção o mesmo token iria por e-mail.

---

# Ajuste — barra de pesquisa única (2026-11-14)

A correção anterior adicionou uma segunda busca dentro do catálogo, como
redundância. Por decisão do dono do produto, ficou **apenas a do cabeçalho**.

- Removido o bloco `.catalogo-busca` de `home.tsx`.
- Removidas as regras `.catalogo-busca` de `home.css`.
- Mantida a correção de responsividade: a busca do cabeçalho nunca é ocultada;
  abaixo de 850px ela ocupa uma linha própria (`order: 3; flex: 1 1 100%`).

Verificação: `grep` confirma 1 ocorrência do campo de busca; nenhuma regra
`home-search` com `display: none`; build e typecheck limpos.
