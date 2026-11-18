# Revisão de código — commit `78cae8b`

> Critérios: **C01** (alteração funcional com diff verificável), **C02**
> (relação com regra de negócio), **C03** (inventário e escopo), **C04**
> (impacto com verificação), **C05** (propriedades de POO), **C06** (decisão
> fundamentada).

## 1. Alteração selecionada (C01)

| | |
|---|---|
| **Commit** | `78cae8b2a8d2611c23819d5736be72c3db914651` |
| **Mensagem** | `feat(domínio): baixa de estoque só na aprovação e reposição no estorno` |
| **Data** | 2026-11-17 |
| **Ponto de comparação** | `78cae8b^` (pai, `1d2da32`) → `78cae8b` |
| **Como reproduzir o diff** | `git show 78cae8b` ou `git diff 78cae8b^ 78cae8b -- backend/Models/Produto.cs` |
| **Tamanho** | 1 arquivo, +57 linhas, 0 remoções |

```
$ git show --stat 78cae8b
 backend/Models/Produto.cs | 57 +++++++++++++++++++++++++++++++++++++++++++++++
 1 file changed, 57 insertions(+)
```

Trecho central do diff:

```diff
+    public void BaixarEstoque(int quantidade)
+    {
+        if (quantidade <= 0)
+            throw new ValidacaoException("A quantidade deve ser maior que zero.");
+
+        if (Estado != EstadoAnuncio.Disponivel && Estado != EstadoAnuncio.Reservado)
+            throw new ConflitoException($"\"{Nome}\" não está mais disponível para compra.");
+
+        if (quantidade > Estoque)
+            throw new ConflitoException($"Estoque insuficiente para \"{Nome}\": restam {Estoque} unidade(s).");
+
+        Estoque -= quantidade;
+
+        if (Estoque == 0)
+            Estado = EstadoAnuncio.Vendido;
+        else if (Estado == EstadoAnuncio.Reservado)
+            Estado = EstadoAnuncio.Disponivel;
+    }
+
+    public void ReporEstoque(int quantidade)
+    {
+        if (quantidade <= 0)
+            throw new ValidacaoException("A quantidade deve ser maior que zero.");
+
+        Estoque += quantidade;
+
+        if (Estado == EstadoAnuncio.Vendido)
+            Estado = EstadoAnuncio.Disponivel;
+    }
```

(Formatação compactada aqui; o diff original tem chaves em linhas próprias.)

## 2. Requisito que a alteração implementa (C02)

A alteração implementa a regra **R5** de [`requisitos.md`](requisitos.md): o
estoque fica sempre entre 0 e o teto. Ela também sustenta o fluxo **FL-03**
(comprar, pagar e estornar), especificamente estes critérios de aceitação:

- **CA-03.1**: a baixa acontece **na aprovação do pagamento**, e o estorno
  devolve as unidades. Antes deste commit, o estoque não era baixado nem na
  compra nem no pagamento: o anúncio só virava `Vendido` quando o vendedor
  concluía o pedido (`produto.Vender()` em `ServicoPedidos.Concluir`, linha
  173 de `78cae8b^`). Não havia baixa por quantidade nem devolução no estorno.
- **CA-03.2**: pedir mais do que existe falha com `409` sem alterar o estoque.

**Inspeção orientada pelo requisito.**

| Exigência da R5 / FL-03 | Linha do diff que atende | Situação |
|---|---|---|
| Não baixar acima do disponível | `if (quantidade > Estoque) throw ConflitoException` | atendida |
| Quantidade positiva | `if (quantidade <= 0) throw ValidacaoException` (nos dois métodos) | atendida |
| Só vende anúncio comprável | `if (Estado != Disponivel && Estado != Reservado) throw` | atendida |
| Esgotar → `Vendido`; estorno de esgotado → `Disponivel` | os dois `if` sobre `Estado` | atendida |
| **Estoque nunca acima do teto / sem estouro** | `Estoque += quantidade;` (sem limite) | **não atendida** (ver §5) |

## 3. Inventário dos arquivos modificados (C03)

| Arquivo | Finalidade da mudança | Dentro do escopo? |
|---|---|---|
| `backend/Models/Produto.cs` | adiciona `BaixarEstoque` e `ReporEstoque` à entidade dona do estoque | **sim**: é exatamente a regra da mensagem do commit |

**Mudanças fora do escopo:** nenhuma no diff. O commit não mexe em
formatação, nomes ou outros métodos.

**O que falta no commit, e onde chegou depois:**

| Ausência | Commit que completou | Consequência |
|---|---|---|
| Nenhum chamador: os métodos entram como código ainda não usado | `1b32db1` (`ServicoPedidos`: `BaixarEstoque` na aprovação, `ReporEstoque` no cancelamento pago) | o commit sozinho não muda comportamento observável pela API |
| Nenhum teste | `4bd1f9b` (`BaixarEstoque_DeveDecrementar_EAoZerarFicaVendido`, `BaixarEstoque_AcimaDoDisponivel_DeveFalharSemAlterar`, `ReporEstoque_DeAnuncioEsgotado_VoltaADisponivel`) | o diff não traz prova executável junto |

Separar domínio, serviço e testes em commits diferentes facilita ler cada
diff. Em compensação, `78cae8b` não pode ser verificado sozinho, e por isso a
verificação do §4 é feita sobre a `main`.

## 4. Impacto da alteração e verificação (C04)

**Impacto local.** Dois métodos públicos novos em `Produto`. Nenhum contrato
existente muda de assinatura.

**Impacto transitivo (rastreado com `grep -rn "BaixarEstoque\|ReporEstoque" backend tests`):**

| Dependente | Como é afetado |
|---|---|
| `ServicoPedidos` (linha ~370) | chama `BaixarEstoque(item.Quantidade)` ao confirmar o pagamento, dentro da transação |
| `ServicoPedidos.Cancelar` (linha ~497) | chama `ReporEstoque(item.Quantidade)` no estorno de pedido pago |
| `RegrasCompra` | **contrato implícito**: `BaixarEstoque` aceita `Reservado`, então quem impede um terceiro de comprar um item reservado é `RegrasCompra` (`case EstadoAnuncio.Reservado when PropostaAceitaDe(...) == null`), não o método revisado |
| Máquina de estados (`Produto.Transicoes`) | **segundo caminho de mudança de estado**: os dois métodos alteram `Estado` diretamente, sem passar pela tabela (ver §5) |
| Concorrência | duas aprovações simultâneas leem o mesmo estoque; quem protege é a concorrência otimista do EF (`DbUpdateConcurrencyException` → `409 concorrencia`) |

**Verificação executada (2026-11-18, `main`):**

```
$ dotnet test --filter "FullyQualifiedName~BaixarEstoque|FullyQualifiedName~ReporEstoque|
                        FullyQualifiedName~CancelarPedidoPago|FullyQualifiedName~DepuracaoEstoque|
                        FullyQualifiedName~Estoque"
Aprovado! – Com falha: 0, Aprovado: 17, Total: 17
```

Entre os 17: `Pix_EstoqueSoBaixaDepoisDaAprovacao` (a baixa só ocorre na
aprovação), `CancelarEExpirar_NaoBaixamEstoque`,
`CancelarPedidoPago_DevolveEstoque`,
`ComprasSimultaneas_DoMesmoEstoque_SoUmaEhAprovada` e
`PropostaAceita_PermiteComprarPeloValorDaProposta` (o vencedor compra o item
reservado e o anúncio volta a `Disponivel` com o estoque restante).

**Impacto negativo encontrado na revisão.** Executado contra o código do
commit (versão `166c21d`, anterior à correção), o cenário "estoque editado
para `int.MaxValue` e depois estorno de 1 unidade" deixa o estoque em
**-2147483648**. A investigação completa está em
[`debugging.md`](debugging.md) (DEF-01).

## 5. Avaliação por propriedades de POO (C05)

**Encapsulamento — atendido com uma falha.** A regra de estoque fica dentro
de `Produto`: `Estoque` tem `private set`, e serviços não fazem
`produto.Estoque -= n`. **Porém** a invariante "estoque ≥ 0" já existia em
`EstoqueValido`, chamada por `AplicarDados` no construtor e em `Atualizar`.
`ReporEstoque` grava o campo diretamente com `+=` e **não reutiliza** essa
validação. A entidade passou a ter duas portas de escrita do estoque, e só uma
delas protege a invariante. É dessa assimetria que nasce o estouro.

**Coesão e responsabilidade — atendida.** Baixar e repor estoque é
responsabilidade de quem detém o estoque. Colocar isso em `ServicoPedidos`
deixaria o serviço manipular o estado interno do anúncio (Cap. 11,
Responsabilidades). Os métodos são curtos, com uma intenção cada.

**Acoplamento ao ciclo de estados — ponto de atenção.** Os dois métodos
alteram `Estado` sem passar por `AplicarOperacao`/`Transicoes`. As transições
induzidas por estoque (→ `Vendido`, `Vendido` → `Disponivel`) não aparecem na
tabela, que deixa de ser a fonte única da R2. Isso é aceitável porque são
transições de **sistema**, não operações do dono (a tabela modela o que o
usuário pode pedir), mas precisa estar documentado. Foi documentado em
`requisitos.md` §5 e em `decisoes-de-modelagem.md` DM-A ("Custo aceito").

## 6. Decisão técnica (C06)

**Decisão: ACEITAR COM CORREÇÃO.**

| Base | Fato verificado |
|---|---|
| Requisito | R5 e CA-03.1/03.2 atendidos pela baixa condicionada e pela reposição (§2) |
| Código | encapsulamento e coesão corretos; uma escrita sem validação (§5) |
| Verificação | 17 testes relacionados aprovados na `main`; defeito reproduzido na versão sem correção (-2147483648) |

**Correção exigida:** a reposição não pode exceder o teto nem estourar `int`, e
a invariante de teto precisa valer também no cadastro e na edição.

**Correção aplicada:** commit `116d245`:

```diff
+    public const int EstoqueMaximo = 100_000;
 ...
-        Estoque += quantidade;
+        // Soma em long e limita ao teto: nunca estoura nem fica negativo.
+        Estoque = (int)Math.Min((long)Estoque + quantidade, EstoqueMaximo);
 ...
+        if (estoque > EstoqueMaximo)
+            throw new ValidacaoException("O estoque deve ser de no máximo 100.000 unidades.");
```

Testes de regressão: `ReporEstoque_NoTeto_NaoEstouraNemFicaNegativo`,
`Anuncio_ComEstoqueAcimaDoTeto_EhRecusado` e
`EstornoDeCompraPaga_ComEstoqueNoLimite_NaoDeixaEstoqueNegativo` (todos
aprovados).

**Não exigido (registrado):** passar as transições induzidas por estoque pela
tabela `Transicoes`. Isso exigiria operações internas na tabela de
transições, o que confunde o que o dono pode pedir com o que o sistema faz.
Fica como decisão consciente, documentada em DM-A.

---

## Anexo — achados de revisão anteriores (registro histórico)

Revisão inicial do código herdado da AV1, feita antes da reestruturação. Os
números de linha e o código "antes" referem-se à versão original.

| ID | Achado | Correção | Evidência atual |
|---|---|---|---|
| C-01 | `Produto.Atualizar` copiava campos sem validar | validação única em `AplicarDados` | `ProdutoValidacaoTests` |
| C-02 | anúncio sem dono; autorização só por `[Authorize]` | `UsuarioId` + `ValidarPropriedade` | `ProdutoPropriedadeTests` |
| C-03 | id derivado de `Count + 1` | id gerado pelo banco | `Adicionar_AposExclusao_NaoDeveReutilizarId` |
| C-05 | `InvalidOperationException` traduzida em cada controller | `ConflitoException` + middleware central | [`excecoes.md`](excecoes.md) |
| C-08 | decisão por modalidade em condicional | `IEstrategiaNegociacao` | DM-B |
| C-09 | categoria como texto cru | `Categoria` com slug | `CategoriaTests` |
