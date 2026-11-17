# Revisão de código — alteração `1b32db1` (checkout e pagamento simulado)

## 1. Alteração selecionada (C01)

| | |
|---|---|
| Commit | `1b32db1` — *feat(pedidos): checkout a partir do carrinho e pagamento simulado com revalidação de estoque* |
| Branch / PR | `marketplace-completo`, PR #1 (incorporado à `main` com *merge commit*, preservando o hash) |
| Como ver o diff | `git show 1b32db1` ou `git diff 1b32db1^ 1b32db1` |
| Ponto de comparação | `1b32db1^` = `ba564d2` (*feat(pagamento): validação do cartão simulado (Luhn, validade, CVV, CPF, parcelas)*) — versão em que o pedido era de um único anúncio e o cliente informava se o pagamento era aprovado |

```
backend/Controller/PagamentosController.cs |  60 ++++
backend/Controller/PedidosController.cs    |  28 +-
backend/Services/IServicoPedidos.cs        |  16 +-
backend/Services/ServicoPedidos.cs         | 506 ++++++++++++++++++++++++-----
4 files changed, 512 insertions(+), 98 deletions(-)
```

## 2. Requisito que a alteração implementa (C02)

Regras **RN4** e **RN5** de `especificacao.md` e o requisito de segurança
"não permitir manipular valores ou a aprovação pelo frontend":

| Requisito | Antes (`1b32db1^`) | Depois (`1b32db1`) | Prova no diff |
|---|---|---|---|
| Estoque só baixa com pagamento aprovado (RN5) | `Concluir` chamava `produto.Vender()`: o anúncio inteiro virava `Vendido` na conclusão, sem decrementar estoque | `Aprovar` chama `produto.BaixarEstoque(item.Quantidade)` para cada item | `-        produto.Vender();` / `+            produto.BaixarEstoque(item.Quantidade);` |
| Revalidar estoque na finalização (RN4) | nenhuma validação de quantidade | `Finalizar` e `Aprovar` chamam `regras.Problema(...)` para cada item | métodos `Finalizar` e `Aprovar` |
| Aprovação decidida pelo servidor | `[HttpPost("{id:int}/pagar")] Pagar(int id, ModeloPagamento modelo)` com `modelo.Aprovado` vindo do cliente | rota removida; aprovação só por `PagamentosController` (PIX/boleto simulados, cartão validado) | `-    [HttpPost("{id:int}/pagar")]` |
| Um pedido por anúncio bloqueava outros compradores | `ObterPendentePorProduto(produtoId) != null` → 409 | removido; concorrência tratada na aprovação | `-        if (repositorioPedido.ObterPendentePorProduto(produtoId) != null)` |

Conclusão: o código implementa o comportamento pedido — verificado pelos
testes da seção 4.

## 3. Inventário dos arquivos modificados (C03)

| Arquivo | Finalidade na alteração | No escopo? |
|---|---|---|
| `ServicoPedidos.cs` | checkout a partir do carrinho (`Finalizar`), aprovação com baixa de estoque (`Aprovar`), PIX/boleto/cartão simulados, expiração, cancelamento | sim |
| `IServicoPedidos.cs` | novo contrato do serviço (remove `Criar`/`Pagar`, adiciona `Finalizar`, `ObterCompra`, `SimularConfirmacao`, `PagarComCartao`, `SimularExpiracao`, `CancelarCompra`) | sim |
| `PedidosController.cs` | `POST /api/pedidos` passa a receber `ModeloCheckout` (sem produto/preço/quantidade); remove `/pagar` | sim |
| `PagamentosController.cs` (novo) | rotas da compra simulada (`/api/pagamentos/{id}`…) | sim |

**Mudanças fora do escopo estrito** identificadas no diff:

1. `ServicoPedidos.Cancelar` passou a **devolver estoque** (`ReporEstoque`) ao
   cancelar pedido pago. Não é checkout, mas é consequência obrigatória de
   RN5 (se o estoque baixa no pagamento, o estorno precisa devolvê-lo).
   Aceitável, porém deveria estar num commit próprio.
2. Textos das notificações mudaram (passam a citar `pedido.Codigo`) —
   cosmético, sem impacto em regra.
3. **Omissão:** o commit não altera `ServicoAdmin.cs`, que usava
   `Pedido.PrecoUnitario` (removido no commit anterior `f3b7cab`). Ver
   impacto transitivo abaixo.

## 4. Impacto e verificação (C04)

| Tipo | Impacto | Verificação |
|---|---|---|
| Contrato HTTP | **quebra**: `POST /api/pedidos/{id}/pagar` deixa de existir; `POST /api/pedidos` muda o corpo (sem `produtoId`) e a resposta (compra com lista de pedidos) | frontend adaptado em `pedidosService.ts`; teste `Seguranca_PrecoEQuantidadeEnviadosPeloNavegadorSaoIgnorados` confirma que `/pagar` não responde 200 |
| Contrato do serviço | `IServicoPedidos` trocou `Criar`/`Pagar` por 6 operações | único consumidor é o controller (busca no código) |
| Dependências | construtor de `ServicoPedidos` passou de **4 para 9** dependências (repositórios de pagamento e carrinho, unidade de trabalho, `RegrasCompra`, `TimeProvider`) | registradas em `Program.cs` no commit `09d4a32` |
| Comportamento | `Concluir` não muda mais o anúncio; o estoque muda no pagamento | `Pix_EstoqueSoBaixaDepoisDaAprovacao`, `Cartao_AprovadoGuardaSoFinal_ERecusadoNaoMexeNoEstoque`, `CancelarEExpirar_NaoBaixamEstoque` |
| Concorrência | baixa de estoque dentro de transação | `ComprasSimultaneas_DoMesmoEstoque_SoUmaEhAprovada` |
| **Transitivo** | o projeto **não compila** em `1b32db1`: `ServicoAdmin.cs(47,39): error CS1061: 'Pedido' não contém uma definição para "PrecoUnitario"` | verificado executando `dotnet build` com o repositório em `1b32db1` (1 erro); em `09d4a32` → **0 erros** |

Verificação final no estado atual da `main`: `dotnet test` — todos os testes
aprovados (ver `auditoria-final.md`).

## 5. Avaliação pelas propriedades de POO (C05)

| Propriedade | Avaliação | Evidência |
|---|---|---|
| **Encapsulamento** — positivo | a decisão "pagamento aprovado" saiu do cliente (`modelo.Aprovado`) e passou para o domínio/serviço; o estado de `Pagamento` só muda por `Aprovar/Recusar/Cancelar/Expirar` | diff do `PedidosController` |
| **Encapsulamento** — problema | `Aprovar` lê `pedido.Itens`, que em `f3b7cab` era `List<ItemPedido>` pública: qualquer serviço poderia `pedido.Itens.Add(...)` e quebrar `Total = soma dos itens` | `Pedido.cs` em `1b32db1` |
| **Coesão** — problema | `ServicoPedidos` (585 linhas em `1b32db1`) passou a cuidar de checkout, pagamento simulado (PIX, boleto, cartão, expiração) **e** do ciclo do pedido (envio, entrega, conclusão, cancelamento) | tamanho e lista de métodos públicos |
| **Acoplamento** | subiu de 4 para 9 dependências, mas todas por interface (`IRepositorio*`, `IUnidadeDeTrabalho`), o que mantém o serviço testável | construtor |
| **Responsabilidades** — positivo | a validação de compra foi extraída para `RegrasCompra`, usada também pelo carrinho: uma regra, um lugar | `regras.Problema(...)` em `Finalizar` e `Aprovar` |

## 6. Decisão técnica (C06)

**Decisão: CORRIGIR e então ACEITAR.**

Fundamentação (fatos → julgamento):

1. *Fato:* os requisitos RN4/RN5 e "aprovação pelo servidor" estão
   implementados e verificados por testes (seção 4). → justifica aceitar o
   comportamento.
2. *Fato:* `Pedido.Itens` mutável permite violar a invariante do total. →
   **correção obrigatória**, aplicada no commit `3a61de9`
   (*itens do pedido expostos só para leitura*), com o teste
   `Itens_NaoPodemSerAlteradosDeFora_PreservandoOTotal`.
3. *Fato:* o commit não compila isoladamente (erro CS1061 em `ServicoAdmin`).
   → correção já presente em `09d4a32`; registrado como lição de processo:
   commits devem compilar sozinhos.
4. *Julgamento:* a queda de coesão de `ServicoPedidos` não quebra regra nem
   teste. → **não bloqueia**; registrada como pendência (extrair um
   `ServicoPagamentos`) em `auditoria-final.md`.
