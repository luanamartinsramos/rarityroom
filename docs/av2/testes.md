# Testes

## 1. Como executar

```bash
cd tests/RarityRoom.Tests
dotnet test
```

Saída esperada: `Aprovado! — Com falha: 0, Aprovado: 104, Total: 104`.

O projeto de testes fica **fora** de `backend/` de propósito: o SDK Web compila
`**/*.cs`, e uma pasta de testes dentro do backend entraria no build da
aplicação.

## 2. Estratégia

Pirâmide com o peso no domínio — a camada onde as regras vivem:

| Nível | Foco | Exemplos |
|---|---|---|
| Domínio | invariantes, estados, autorização | `ProdutoTransicoesTests`, `PropostaTests` |
| Repositório | identidade, concorrência, cópia defensiva | `RepositorioProdutoTests` |
| Serviço | orquestração, autorização por dono, catálogo | `ServicoPropostasTests` |
| Contrato | polimorfismo efetivamente usado | `EstrategiasTests` |

Sem mocks: os repositórios em memória são rápidos e reais, o que evita testar
o dublê em vez do comportamento.

## 3. Distribuição

| Arquivo | Casos | O que cobre |
|---|---:|---|
| `Dominio/UsuarioTests.cs` | 13 | nome, e-mail, hash, senha |
| `Dominio/ProdutoTransicoesTests.cs` | 12 | ciclo de vida completo + colisão |
| `Servicos/ServicoAnunciosTests.cs` | 11 | CRUD, dono, estado, catálogo |
| `Dominio/ProdutoValidacaoTests.cs` | 10 | formação do anúncio |
| `Dominio/CategoriaTests.cs` | 9 | slug, unicidade |
| `Servicos/ServicoAutenticacaoTests.cs` | 9 | cadastro, login, duplicidade |
| `Dominio/PropostaTests.cs` | 7 | valor, transições, proponente |
| `Negociacao/EstrategiasTests.cs` | 6 | limites de cada modalidade |
| `Servicos/ServicoPropostasTests.cs` | 6 | aceite por modalidade, cascata |
| `Dominio/ProdutoPropriedadeTests.cs` | 5 | autorização por dono |
| `Repositorios/RepositorioProdutoTests.cs` | 5 | id, concorrência, cópia |
| `Servicos/ServicoFavoritosTests.cs` | 5 | favorito próprio, duplicata |
| `Dominio/FavoritoTests.cs` | 3 | invariantes do vínculo |
| `Servicos/ServicoAnunciosCategoriaTests.cs` | 3 | categoria obrigatória |
| `Repositorios/RepositorioEfTests.cs` | 9 | persistência real (EF Core + SQLite) |
| `Api/FluxoApiTests.cs` | 24 | HTTP real: auth, compra, avaliação, upload |
| `Dominio/PedidoTests.cs` | 12 | snapshot, estados, autorização |
| `Dominio/AvaliacaoTests.cs` | 7 | nota, autoria, pedido concluído |
| `Dominio/BuscaTextoTests.cs` | 6 | normalização de acento e caixa |
| `Servicos/ServicoAnunciosBuscaTests.cs` | 12 | termo, categoria e estado no serviço |
| **Total** | **174** | |

## 4. Casos que provam regressões específicas

| Defeito | Teste |
|---|---|
| Id duplicado após exclusão | `Adicionar_AposExclusao_NaoDeveReutilizarId` |
| Id duplicado sob concorrência | `Adicionar_EmParalelo_DeveGerarIdsUnicos` (200 threads) |
| Terceiro edita anúncio alheio | `Atualizar_PorOutroUsuario_DeveNegar` |
| Terceiro exclui anúncio alheio | `Remover_PorOutroUsuario_DeveNegar` |
| Reativar cancela reserva | `Reativar_DeReservado_NaoDeveCancelarReserva` |
| Lista interna vazada | `ObterTodos_DeveDevolverCopiaDaLista` |
| Preço inválido aceito | `Construtor_PrecoNaoPositivo_DeveFalhar` |

## 5. Rastreabilidade requisito → teste

| Requisito | Teste |
|---|---|
| RN01 formação do anúncio | `ProdutoValidacaoTests` + `ServicoAnunciosCategoriaTests` |
| RN02 autorização por dono | `ProdutoPropriedadeTests` |
| RN03 anúncio vendido imutável | `Atualizar_AnuncioVendido_DeveFalhar` |
| RN04 transições por (origem, operação) | `ProdutoTransicoesTests` |
| RN05 venda sem estoque | `Vender_SemEstoque_DeveFalhar` |
| RN06 favorito próprio/duplicado | `FavoritoTests` + `ServicoFavoritosTests` |
| RN07 categoria única | `CategoriaTests` |
| RN08 proposta no próprio anúncio | `Criar_NoProprioAnuncio_DeveFalhar` |
| RN09 anúncio indisponível | `Criar_EmAnuncioIndisponivel_DeveFalhar` |
| RN10 aceite por modalidade | `Aceitar_MesmaOferta_DeveVariarPorModalidade` |
| RN11 cascata de recusa | `Aceitar_DeveRecusarAsDemaisPendentes` |
| RN12 cancelamento pelo proponente | `Cancelar_DeOutroUsuario_DeveNegar` |
| RN13 e-mail único | `Cadastrar_EmailDuplicadoComCaixaDiferente_DeveConflitar` |
| RN14 senha com hash | `Cadastrar_NaoDeveGuardarSenhaEmTextoPuro` |

## 6. Dublês e persistência real

A suíte tem duas camadas distintas de banco:

| Camada | Onde | Por quê |
|---|---|---|
| Dublês em memória | `tests/RarityRoom.Tests/Fakes` | rapidez: a maioria dos testes sem abrir conexão |
| SQLite real | `RepositorioEfTests` (9 casos) | exercita o mapeamento e as consultas de produção |
| API completa | `FluxoApiTests` (9 casos) | sobe o app inteiro por `WebApplicationFactory` |

O SQLite em memória usa a mesma migração e o mesmo `DbContext` da aplicação,
então um erro de mapeamento aparece no teste antes de aparecer no servidor.

## 7. Limitações declaradas

- Os testes de integração usam **SQLite em arquivo temporário**, não o banco de
  desenvolvimento. Não há execução contra PostgreSQL ou SQL Server.
- O upload é validado por **tamanho e tipo de conteúdo declarado**; não há
  inspeção do conteúdo binário da imagem.
- Não há teste de carga nem de concorrência no banco (o SQLite serializa
  escritas por natureza).
