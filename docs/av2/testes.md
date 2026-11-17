# Testes automatizados

## 1. Como executar (a partir de um clone limpo)

```bash
git clone https://github.com/luanamartinsramos/rarityroom.git
cd rarityroom/tests/RarityRoom.Tests
dotnet test
```

Pré-requisito: .NET SDK 10. Não é preciso preparar banco: os testes de
integração sobem a API com `WebApplicationFactory` e um arquivo SQLite
temporário por teste (`Api/FabricaApi.cs`), aplicando as mesmas migrações da
aplicação. Saída esperada: **235 aprovados, 0 falhas**.

O projeto de testes fica fora de `backend/` porque o SDK Web compila todo
`**/*.cs` da pasta do projeto.

## 2. Estratégia

| Nível | Foco | Exemplos |
|---|---|---|
| Domínio | invariantes, estados, regras dentro das entidades | `ProdutoTransicoesTests`, `EstoqueTests`, `PedidoTests`, `PagamentoTests` |
| Serviço | orquestração e autorização com dublês em memória (`Fakes/`) | `ServicoPropostasTests`, `ServicoAnunciosTests` |
| Repositório | mapeamento EF Core contra SQLite real | `RepositorioEfTests` |
| Integração HTTP | pipeline completo: controller → serviço → domínio → banco → middleware | `MarketplaceApiTests`, `FluxoApiTests`, `FalhasEInvariantesApiTests`, `ReabastecerApiTests` |

## 3. Distribuição (235 casos)

| Classe de teste | Casos | Foco |
|---|---:|---|
| `Api/FluxoApiTests` | 24 | autenticação, anúncio, compra, avaliação, upload, admin, loja, perfil |
| `Api/MarketplaceApiTests` | 23 | busca, 7 ordenações, carrinho, estoque, PIX/boleto/cartão, concorrência, idempotência, segurança |
| `Dominio/PedidoTests` | 19 | itens, código único, snapshot, transições, encapsulamento dos itens, estorno |
| `Dominio/UsuarioTests` | 13 | nome, e-mail, hash, senha |
| `Dominio/ProdutoTransicoesTests` | 12 | ciclo de vida do anúncio |
| `Servicos/ServicoAnunciosBuscaTests` | 12 | busca e filtros no serviço |
| `Servicos/ServicoAnunciosTests` | 11 | CRUD, dono, estado |
| `Dominio/ProdutoValidacaoTests` | 10 | formação do anúncio |
| `Dominio/ValidadorCartaoTests` | 9 | Luhn, validade, CVV, CPF, parcelas, cartões de recusa |
| `Dominio/CategoriaTests` | 9 | slug e unicidade |
| `Repositorios/RepositorioEfTests` | 9 | persistência real |
| `Servicos/ServicoAutenticacaoTests` | 9 | cadastro, login, duplicidade |
| `Dominio/AvaliacaoTests` | 8 | nota, autoria, item do pedido |
| `Dominio/PropostaTests` | 7 | valor, transições, proponente |
| `Dominio/ProdutoReabastecerTests` | 6 | RN-REAB R1–R4 |
| `Dominio/BuscaTextoTests` | 6 | acento e caixa |
| `Negociacao/EstrategiasTests` | 6 | polimorfismo das modalidades |
| `Servicos/ServicoPropostasTests` | 6 | aceite por modalidade, cascata |
| `Dominio/EstoqueTests` | 5 | baixa e reposição de estoque |
| `Dominio/ProdutoPropriedadeTests` | 5 | autorização por dono |
| `Repositorios/RepositorioProdutoTests` | 5 | dublê em memória |
| `Servicos/ServicoFavoritosTests` | 5 | favorito próprio e duplicado |
| `Api/FalhasEInvariantesApiTests` | 4 | transição proibida no banco; 3 situações de falha |
| `Dominio/PagamentoTests` | 4 | PIX/boleto fictícios, expiração, transição única |
| `Dominio/FavoritoTests` | 3 | vínculo usuário–anúncio |
| `Servicos/ServicoAnunciosCategoriaTests` | 3 | categoria obrigatória |
| `Api/ReabastecerApiTests` | 2 | critérios de aceitação da especificação DsCode |

(`EstoqueTests`, `PagamentoTests` e `ValidadorCartaoTests` estão no arquivo
`Dominio/PedidoTests.cs`.)

## 4. Itens da rubrica cobertos

| Item | Onde |
|---|---|
| **F01** — sucesso e violação para as 5 regras (≥ 10 cenários) | matriz em `especificacao.md` §4: 10 cenários de sucesso e 17 de violação |
| **F02** — tentar violar invariante e confirmar estado válido | `BaixarEstoque_AcimaDoDisponivel_DeveFalharSemAlterar` (estoque continua 3), `Itens_NaoPodemSerAlteradosDeFora_PreservandoOTotal` (total continua 100), `Reativar_DeReservado_NaoDeveCancelarReserva`, `R3_AnuncioReservado_EhRecusadoSemAlterarNada` |
| **F03** — integração pela API com regra e persistência | `Pix_EstoqueSoBaixaDepoisDaAprovacao` (aplica RN5 pela API e relê o estoque em outra requisição, com outro `DbContext`, lido do banco), `TransicaoProibida_ViaApi_EhRejeitadaENaoAlteraOBanco`, `CA1_DonoReabasteceAnuncioEsgotado_VoltaAoCatalogo` |
| **F04** — compila e testa a partir de clone limpo | seção 1; resultado registrado em `auditoria-final.md` |
| **D04** — três situações de falha | `FalhasEInvariantesApiTests` (S1, S2, S3) |

## 5. Regressões protegidas

| Defeito | Teste |
|---|---|
| 500 ao finalizar compra com dois vendedores (`debugging.md` §A) | `CarrinhoComDoisVendedores_GeraUmPedidoPorVendedor` |
| Id reutilizado após exclusão | `Adicionar_AposExcluirUltimo_NaoDeveReutilizarId` |
| Terceiro edita anúncio alheio | `Seguranca_CompradorNaoEditaNemExcluiAnuncioDeTerceiro` |
| Reativar cancelava reserva | `Reativar_DeReservado_NaoDeveCancelarReserva` |
| Cliente decidia a aprovação do pagamento | `Seguranca_PrecoEQuantidadeEnviadosPeloNavegadorSaoIgnorados` |
| Venda dupla da última unidade | `ComprasSimultaneas_DoMesmoEstoque_SoUmaEhAprovada` |

## 6. Limitações

- Integração apenas contra SQLite (não há execução contra PostgreSQL/SQL Server).
- O frontend não tem testes automatizados no repositório. O fluxo pela
  interface foi verificado manualmente com navegador automatizado, sem script
  versionado.
- Upload validado por tamanho e tipo declarado, sem inspecionar o conteúdo
  binário.
