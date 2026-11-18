# Testes automatizados e rastreabilidade

> Critérios: **F01** (sucesso e violação das cinco regras, ≥ 10 cenários),
> **F02** (invariante violada e objeto válido), **F03** (integração pela API
> com regra e persistência), **F04** (build e testes a partir de clone limpo),
> **F05** (rastreabilidade fluxos × regras × implementação × testes).

## 1. Como executar a partir de um clone limpo (F04)

Pré-requisito: **.NET SDK 10** (`dotnet --version` → `10.0.x`). Não é preciso
banco instalado: cada teste cria um SQLite temporário e aplica as migrações
sozinho.

```bash
git clone https://github.com/luanamartinsramos/rarityroom.git
cd rarityroom
dotnet build backend          # Compilação com êxito. 0 Aviso(s), 0 Erro(s)
dotnet test tests/RarityRoom.Tests
```

Resultado registrado em 2026-11-18 (SDK 10.0.404, Linux):

```
Aprovado!  – Com falha: 0, Aprovado: 245, Ignorado: 0, Total: 245
```

O projeto de testes fica **fora** de `backend/` de propósito: o SDK Web
compila todo `**/*.cs` da pasta, e uma pasta de testes dentro do backend
entraria no build da aplicação.

## 2. Estratégia

| Nível | Foco | Banco | Exemplos |
|---|---|---|---|
| Domínio | invariantes, estados, regras de uma instância | nenhum | `ProdutoTransicoesTests`, `PedidoTests`, `EstoqueTests` |
| Contrato polimórfico | cada estratégia isolada | nenhum | `EstrategiasTests` |
| Serviço | orquestração, autorização, regras entre instâncias | dublês em memória (`Fakes/`) | `ServicoPropostasTests`, `ServicoAnunciosTests` |
| Repositório | mapeamento e consultas reais | SQLite real | `RepositorioEfTests` |
| API (integração) | HTTP → serviço → domínio → SQLite | SQLite em arquivo temporário, app completo via `WebApplicationFactory` (`FabricaApi`) | `FluxoApiTests`, `MarketplaceApiTests`, `FalhasApiTests`, `DepuracaoEstoqueTests` |

## 3. Testes das cinco regras: sucesso e violação (F01)

São **22 cenários** nomeados, todos aprovados. Cada regra tem pelo menos 2 de
sucesso e 2 de violação.

| Regra | Sucesso | Violação |
|---|---|---|
| **R1** Só o proprietário altera o anúncio | `ValidarPropriedade_QuandoEhDono_NaoDeveFalhar`; `Atualizar_PeloDono_DeveAlterar` | `Atualizar_PorOutroUsuario_NaoDeveAlterarDados`; `Remover_PorOutroUsuario_DeveNegar`; `Terceiro_NaoPodeEditarAnuncioAlheio` (API, 403) |
| **R2** Estado só muda por transição permitida | `Reservar_DeDisponivel_DeveReservar`; `Reativar_DePausado_DeveVoltarParaDisponivel` | `CancelarReserva_DePausado_DeveFalhar`; `Reservar_DeVendido_DeveFalhar`; `Falha01_VenderAnuncioPausado_Responde409_EEstadoContinuaPausado` (API) |
| **R3** Aceite depende da modalidade | `OfertaNoLimiteExato_DeveSerAceitaPorAceitaOferta`; `Aceitar_MesmaOferta_DeveVariarPorModalidade` (Liquidação → 200) | `OfertaDe200EmPrecoDe249_90_DeveVariarPorModalidade` (PrecoFixo recusa); `Aceitar_MesmaOferta_DeveVariarPorModalidade` (PrecoFixo → 409) |
| **R4** Ninguém negocia o próprio anúncio | `Construtor_DeveIniciarPendente` (proposta em anúncio alheio); `Fluxo_Completo_CompraEAvaliacao` (compra de anúncio alheio) | `Criar_NoProprioAnuncio_DeveFalhar`; `Construtor_NoProprioAnuncio_DeveFalhar`; `Pedido_NoProprioAnuncio_DeveRetornar400` (API) |
| **R5** Estoque entre 0 e 100.000 | `BaixarEstoque_DeveDecrementar_EAoZerarFicaVendido`; `ReporEstoque_DeAnuncioEsgotado_VoltaADisponivel`; `CancelarPedidoPago_DevolveEstoque` (API) | `BaixarEstoque_AcimaDoDisponivel_DeveFalharSemAlterar`; `ReporEstoque_NoTeto_NaoEstouraNemFicaNegativo`; `Anuncio_ComEstoqueAcimaDoTeto_EhRecusado` (API); `EstornoDeCompraPaga_ComEstoqueNoLimite_NaoDeixaEstoqueNegativo` (API) |

## 4. Invariante violada e objeto válido (F02)

| Teste | Invariante atacada | Prova de que o objeto continua válido |
|---|---|---|
| `BaixarEstoque_AcimaDoDisponivel_DeveFalharSemAlterar` | estoque ≥ 0 (baixar 4 de 3) | `Assert.Throws<ConflitoException>` **e** `Assert.Equal(3, produto.Estoque)` |
| `CancelarReserva_DePausado_DeveFalhar` | transição só por par permitido | `Assert.Throws<TransicaoInvalidaException>` **e** `Assert.Equal(EstadoAnuncio.Pausado, produto.Estado)` |
| `Atualizar_PorOutroUsuario_NaoDeveAlterarDados` | só o dono altera | exceção **e** nome/preço originais preservados |
| `Falha01_VenderAnuncioPausado_Responde409_EEstadoContinuaPausado` | transição proibida, pela API | estado e estoque **relidos do banco** inalterados |

## 5. Integração pela API com regra e persistência (F03)

`Falha01_VenderAnuncioPausado_Responde409_EEstadoContinuaPausado`
(`tests/RarityRoom.Tests/Api/FalhasApiTests.cs`):

1. **Operação pela API:** `POST /api/produtos` e `PATCH /api/produtos/{id}/estado`
   com cookie de sessão real, no app completo sobre SQLite.
2. **Regra aplicada:** R2. `Pausar` é aceito e `Vender` a partir de `Pausado`
   é rejeitado com `409 transicao-invalida`.
3. **Persistência verificada:** um `GET` posterior (outra requisição, outro
   escopo do `DbContext`) lê do banco `estado = Pausado` e `estoque = 3`.

Outros testes de integração com persistência: `CancelarPedidoPago_DevolveEstoque`
(R5: estoque 1 → 0 → 1 relido pela API),
`EstornoDeCompraPaga_ComEstoqueNoLimite_NaoDeixaEstoqueNegativo` (R5) e
`Fluxo_Completo_CompraEAvaliacao` (FL-03 completo).

## 6. Matriz de rastreabilidade (F05)

### 6.1 Fluxos × regras

| Fluxo | R1 | R2 | R3 | R4 | R5 |
|---|:---:|:---:|:---:|:---:|:---:|
| **FL-01** Publicar e gerenciar anúncio | ● | ● | | | ● |
| **FL-02** Negociar por proposta | ● | ● | ● | ● | |
| **FL-03** Comprar, pagar e estornar | | | | ● | ● |

### 6.2 Regra → implementação → testes → critério de aceitação

| Regra | Implementação (arquivo → membro) | Teste de domínio/serviço | Teste de API | Critério de aceitação |
|---|---|---|---|---|
| R1 | `Models/Produto.cs` → `ValidarPropriedade`, `ValidarExclusao`; chamados por `Services/ServicoAnuncios.cs` → `Atualizar`, `Remover`, `AlterarEstado` | `ProdutoPropriedadeTests`, `Atualizar_PorOutroUsuario_NaoDeveAlterarDados` | `Terceiro_NaoPodeEditarAnuncioAlheio` | CA-01.3 |
| R2 | `Models/Produto.cs` → `Transicoes`, `AplicarOperacao` | `ProdutoTransicoesTests` (12) | `Falha01_VenderAnuncioPausado_Responde409_EEstadoContinuaPausado` | CA-01.2 |
| R3 | `Models/Negociacao/*` → `Aceita`, `ValorMinimo`; `Services/ServicoPropostas.cs` → `Aceitar`, `ObterEstrategia` | `EstrategiasTests`, `Aceitar_MesmaOferta_DeveVariarPorModalidade` | — | CA-02.1 |
| R4 | `Services/ServicoPropostas.cs` → `Criar`; `Models/Pedido.cs` → construtor | `Criar_NoProprioAnuncio_DeveFalhar`, `Construtor_NoProprioAnuncio_DeveFalhar` | `Pedido_NoProprioAnuncio_DeveRetornar400` | CA-02.3 |
| R5 | `Models/Produto.cs` → `BaixarEstoque`, `ReporEstoque`, `EstoqueValido`, `EstoqueMaximo`; `Services/ServicoPedidos.cs` → aprovação e `Cancelar` | `EstoqueTests` (5), `ReporEstoque_NoTeto_NaoEstouraNemFicaNegativo` | `CancelarPedidoPago_DevolveEstoque`, `EstornoDeCompraPaga_ComEstoqueNoLimite_NaoDeixaEstoqueNegativo` | CA-03.1, CA-03.2, CA-03.3 |

### 6.3 Fluxo → ponta a ponta

| Fluxo | Endpoints | Teste ponta a ponta |
|---|---|---|
| FL-01 | `POST/PUT /api/produtos`, `PATCH /api/produtos/{id}/estado` | `Falha01_VenderAnuncioPausado_Responde409_EEstadoContinuaPausado`, `Terceiro_NaoPodeEditarAnuncioAlheio` |
| FL-02 | `POST /api/propostas`, `POST /api/propostas/{id}/aceitar` | `PropostaAceita_PermiteComprarPeloValorDaProposta` |
| FL-03 | `POST /api/carrinho/itens`, `POST /api/pedidos`, `POST /api/pagamentos/{id}/simular-confirmacao`, `POST /api/pedidos/{id}/cancelar` | `Fluxo_Completo_CompraEAvaliacao`, `CancelarPedidoPago_DevolveEstoque` |

## 7. Distribuição da suíte (245 casos)

| Classe de teste | Casos | Classe de teste | Casos |
|---|---:|---|---:|
| `Api.FluxoApiTests` | 24 | `Dominio.CategoriaTests` | 9 |
| `Api.MarketplaceApiTests` | 23 | `Dominio.ValidadorCartaoTests` | 9 |
| `Dominio.PedidoTests` | 18 | `Dominio.AvaliacaoTests` | 8 |
| `Dominio.UsuarioTests` | 17 | `Dominio.PropostaTests` | 7 |
| `Api.SegurancaApiTests` | 15 | `Servicos.ServicoPropostasTests` | 6 |
| `Servicos.ServicoAnunciosBuscaTests` | 12 | `Negociacao.EstrategiasTests` | 6 |
| `Dominio.ProdutoTransicoesTests` | 12 | `Dominio.BuscaTextoTests` | 6 |
| `Servicos.ServicoAnunciosTests` | 11 | `Dominio.EstoqueTests` | 5 |
| `Dominio.ProdutoValidacaoTests` | 10 | `Dominio.ProdutoPropriedadeTests` | 5 |
| `Servicos.ServicoAutenticacaoTests` | 9 | `Servicos.ServicoFavoritosTests` | 5 |
| `Repositorios.RepositorioEfTests` | 9 | `Repositorios.RepositorioProdutoTests` | 5 |
| `Dominio.PagamentoTests` | 4 | `Servicos.ServicoAnunciosCategoriaTests` | 3 |
| `Dominio.FavoritoTests` | 3 | `Api.FalhasApiTests` | 3 |
| `Api.DepuracaoEstoqueTests` | 1 | **Total** | **245** |

(Contagem por `dotnet test --list-tests`; `[Theory]` conta um caso por linha
de dados.)

## 8. Limitações declaradas

- Os testes de integração usam **SQLite em arquivo temporário**. Não há
  execução contra outro SGBD.
- Pagamentos são **simulados**: não há teste contra gateway real, porque não
  existe integração real.
- Não há teste de carga. A concorrência é verificada por cenários específicos
  (`ComprasSimultaneas_DoMesmoEstoque_SoUmaEhAprovada`), não por volume.
- O frontend não tem testes automatizados. A verificação do frontend é
  `npm run build` e `npm run lint`.
