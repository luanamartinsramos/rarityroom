# Debugging

A **seção A** documenta a investigação completa de um defeito reproduzível,
com as evidências salvas em `docs/av2/evidencias/depuracao/`. A **seção B**
mantém o registro histórico de defeitos e episódios anteriores.

# A. Investigação principal — 500 ao finalizar compra com dois vendedores

## A1. Defeito e evidência do comportamento incorreto (E01)

**Defeito real**, observado durante o desenvolvimento do checkout. O teste
`CarrinhoComDoisVendedores_GeraUmPedidoPorVendedor` falhou com HTTP 500. A
correção entrou no commit `1b32db1`. Para registrar evidências reproduzíveis,
o defeito foi **reintroduzido de forma controlada** numa cópia isolada do
repositório (`git worktree`), trocando uma única linha de
`ServicoPedidos.Finalizar`:

```diff
-                    entrega with { },
+                    entrega,
```

Evidência do comportamento incorreto:
- resposta HTTP 500 em
  [`02-hipoteses-e-diagnostico.txt`](evidencias/depuracao/02-hipoteses-e-diagnostico.txt);
- exceção completa no log do servidor em
  [`01-log-servidor-com-defeito.txt`](evidencias/depuracao/01-log-servidor-com-defeito.txt).

## A2. Reprodução, esperado e observado (E02)

**Condições de reprodução** — script
[`reproduzir.sh`](evidencias/depuracao/reproduzir.sh), com o backend em
`http://localhost:5055` e um banco SQLite vazio:

1. cadastrar e entrar com três contas: Ana e Carlos (vendedores) e Bruno (comprador);
2. Ana anuncia "Item da Ana" (R$ 10) e Carlos anuncia "Item do Carlos" (R$ 20);
3. Bruno coloca **um item de cada vendedor** no carrinho;
4. Bruno faz `POST /api/pedidos` com endereço válido e forma `Pix`.

| | Resultado |
|---|---|
| **Esperado** (FL3, decisão DA1) | 201 Created; uma compra `Pendente` de R$ 30 com **2 pedidos**, um para cada vendedor |
| **Observado** | 500 `erro-interno`; nenhuma compra criada |

## A3. Hipóteses e evidências (E03)

| Hipótese | Teste realizado | Evidência | Resultado |
|---|---|---|---|
| H1 — o endereço é inválido para o EF (tamanho, campo nulo) | mesmo endereço, carrinho com **um** vendedor | `HTTP 201` em `02-hipoteses…` | **descartada** |
| H2 — conflito de concorrência no estoque | ler o tipo da exceção no log | `System.InvalidOperationException` (não `DbUpdateConcurrencyException`); além disso, o checkout não grava estoque | **descartada** |
| H3 — a **mesma instância** de `DadosEntrega` (tipo *owned* do EF) está sendo atribuída aos dois pedidos; ao salvar o segundo, o EF tenta trocar o dono (`PedidoId`) da entrega já rastreada | (a) stack trace aponta `RepositorioPedidoEf.Adicionar` chamado de `ServicoPedidos.cs:line 192`, o laço `foreach (var grupo …)` que cria um pedido por vendedor; (b) mensagem: `The property 'DadosEntrega.PedidoId' is part of a key and so cannot be modified`; (c) log de diagnóstico mostra **o mesmo objeto** nos dois pedidos: `[diag] vendedor=2 entrega#=63665143` / `[diag] vendedor=3 entrega#=63665143` | **confirmada** |

A investigação reduziu o espaço de busca em etapas: primeiro a camada (a
exceção sai do repositório, não do domínio), depois a iteração (só o segundo
pedido falha) e por fim o objeto (identidade compartilhada).

## A4. Técnicas de depuração aplicadas (E04)

1. **Análise de logs:** o `MiddlewareExcecoes` registra a exceção completa
   com `LogError`. O log de produção (`01-log-servidor-com-defeito.txt`)
   trouxe o tipo, a mensagem do EF e o stack trace até a linha do serviço.
2. **Rastreamento da execução pelo stack trace:**
   `PedidosController.Finalizar` → `ServicoPedidos.Finalizar` (linha 125) →
   `UnidadeDeTrabalhoEf.Executar` → lambda de `Finalizar` (linha 192) →
   `RepositorioPedidoEf.Adicionar` (linha 59) → `SaveChanges`.
3. **Inspeção de variável por log temporário:** antes de cada `Adicionar`
   foi impresso o *hash* de identidade do objeto de endereço
   (`RuntimeHelpers.GetHashCode(entrega)`). O mesmo valor nos dois pedidos
   provou o compartilhamento. O log temporário existiu só na cópia isolada e
   não foi para o repositório.
4. **Inspeção do estado persistido:** contagem das tabelas após a falha
   (`pagamentos 0`, `pedidos 0`, `itens_pedido 0`, `itens_carrinho 2`). A
   transação foi desfeita e o carrinho preservado.

## A5. Causa raiz, correção e eliminação (E05)

**Causa raiz:** `DadosEntrega` é um *owned type* mapeado com `OwnsOne`. Para
o EF, cada instância pertence a **um único** pedido (a chave dela é o
`PedidoId`). O laço reutilizava o mesmo objeto `entrega`, recebido do
controller, para todos os pedidos do checkout. O problema não estava na
linha que falha (`SaveChanges`), mas na **identidade compartilhada** criada
no serviço: "não confundir causa com localização".

**Correção (menor correção coerente):** cada pedido recebe uma cópia do
*record* (`entrega with { }`), com os mesmos valores e outra instância. O
comentário no código explica o motivo:

```csharp
// Cada pedido guarda a própria cópia do endereço (o EF não
// permite compartilhar a mesma instância entre pedidos).
entrega with { },
```

**Comportamento eliminado:** o mesmo roteiro devolve `HTTP 201 | pedidos: 2`
e `erros no log: 0` (`03-verificacao-antes-depois.txt`).

## A6. Verificação antes/depois e comparação de técnicas (E06)

| | Antes (defeito) | Depois (corrigido) |
|---|---|---|
| Teste de regressão `CarrinhoComDoisVendedores_GeraUmPedidoPorVendedor` | **Com falha: 1** — resposta 500 | **Aprovado: 1** |
| Roteiro HTTP `reproduzir.sh` | 500, nenhuma compra | 201, 2 pedidos |
| Log do servidor | `fail: … InvalidOperationException` | 0 `fail:` |
| Suíte completa | — | todos aprovados (não regressão) |

Fonte: [`03-verificacao-antes-depois.txt`](evidencias/depuracao/03-verificacao-antes-depois.txt).
O teste de regressão permanece na suíte e impede o retorno do defeito.

**Comparação: análise de logs × breakpoint no depurador**

| Critério | Análise de logs + diagnóstico temporário (usada) | Breakpoint no `foreach` de `Finalizar` (alternativa) |
|---|---|---|
| Ambiente | funciona com o servidor rodando normalmente e em CI | exige IDE com depurador anexado ao processo |
| Reprodutibilidade | a evidência fica salva em arquivo e anexada ao repositório | a observação é efêmera (só quem depurou viu) |
| Custo para confirmar H3 | uma linha de log temporária e uma nova execução | inspecionar `entrega` nas duas iterações e comparar com *Make Object ID*; mais rápido para quem já está no depurador |
| Risco | o log temporário precisa ser removido (feito: só na cópia isolada) | nenhum código alterado |
| Profundidade | mostra o que foi registrado | permite explorar o *change tracker* do EF ao vivo |

**Recomendação:** para este defeito, análise de logs. O stack trace do log
de produção já localizava a linha e a mensagem do EF sugeria a hipótese; o
log temporário apenas confirmou a identidade. O breakpoint seria preferível
se o stack trace não existisse (falha silenciosa) ou se fosse preciso
navegar pelo estado interno do EF.

---

# B. Registro histórico

## 1. Método

1. Ler o código do caminho suspeito **antes** de alterar qualquer coisa.
2. Reproduzir o comportamento por HTTP, com `curl`, para ver o efeito real.
3. Identificar a camada dona do defeito.
4. Corrigir nessa camada.
5. Provar com teste automatizado e nova chamada HTTP.

---

## Caso 1 — Anúncio criado com id duplicado

**Sintoma esperado.** Após excluir um anúncio, o próximo criado reaproveita um
id que já existe.

**Causa raiz.** `ProdutosController.Criar` gerava o identificador a partir da
contagem da coleção:

```csharp
int id = repositorioProduto.ObterTodos().Count + 1;
```

Com ids `{1, 2, 3}` a contagem é 3. Excluindo o id 2, o conjunto fica `{1, 3}`
e a contagem passa a 2 — o próximo anúncio recebe `id = 3`, que já existe. O
`DELETE /api/produtos/3` seguinte apaga o anúncio errado.

**Correção.** O identificador passou a ser gerado pelo repositório, com
contador próprio, dentro de região crítica:

```csharp
lock (trava)
{
    produto.DefinirId(++ultimoId);
    produtos.Add(produto);
}
```

**Prova.** Teste `Adicionar_AposExclusao_NaoDeveReutilizarId`. Verificação HTTP
real: criados 1, 2 e 3; excluído o 2; o anúncio seguinte recebeu **id 4** e o
catálogo ficou `{1, 3, 4}` — sem colisão.

---

## Caso 2 — Terceiro edita e exclui anúncio alheio

**Sintoma esperado.** Qualquer usuário autenticado altera o anúncio de outro,
porque `PUT` e `DELETE` exigiam apenas `[Authorize]`.

**Causa raiz.** `Produto` não possuía campo de proprietário. Sem dono no
modelo, não existe verificação possível na camada correta — o controller só
sabia "há alguém logado".

**Correção.** `UsuarioId` obrigatório no construtor e duas operações de
domínio:

```csharp
produto.ValidarPropriedade(solicitanteId); // lança AcessoNegadoException
produto.ValidarExclusao(solicitanteId);    // + bloqueia anúncio vendido
```

**Prova.** `ProdutoPropriedadeTests` e `ServicoAnunciosTests`. Verificação HTTP
real com dois usuários: o segundo recebeu **403 `acesso-negado`** em `PUT`,
`DELETE` e `PATCH`.

---

## Caso 3 — Colisão de transições indexadas só pelo destino

**Origem.** Risco identificado no desenho, antes de virar defeito: se a tabela
de transições fosse indexada apenas pelo estado de destino, `Reativar` e
`CancelarReserva` (ambas levam a `Disponivel`) seriam a mesma chave, e
`Reativar()` sobre um anúncio reservado cancelaria a reserva em silêncio.

**Decisão.** A tabela foi construída com chave `(origem, operação)`:

```csharp
[(EstadoAnuncio.Reservado, OperacaoAnuncio.CancelarReserva)] = EstadoAnuncio.Disponivel,
[(EstadoAnuncio.Pausado,    OperacaoAnuncio.Reativar)]       = EstadoAnuncio.Disponivel,
```

**Prova.** `Reativar_DeReservado_NaoDeveCancelarReserva` — a operação lança
`TransicaoInvalidaException` e o estado permanece `Reservado`.

---

## Casos de depuração durante a implementação

Falhas reais enfrentadas e resolvidas durante o trabalho.

### E-01 — `MSB3027`: não foi possível copiar o executável

**Sintoma.**
`error MSB3027: não foi possível copiar "...apphost.exe" para "...RarityRoom.exe". Número de novas tentativas 10 excedido. O arquivo é bloqueado por: "RarityRoom (26816)"`

**Causa.** O servidor de testes anterior continuava em execução e mantinha o
`.exe` aberto; o `dotnet build` não consegue sobrescrever o binário em uso.

**Correção.** Encerrar o processo antes de compilar:
`taskkill /F /IM RarityRoom.exe`. Nenhuma alteração de código foi necessária —
o erro não era do projeto.

### E-02 — `CS1061`: `HttpContext` não contém `SignInAsync`

**Sintoma.** Erro de compilação nos dois pontos de login/logout.

**Causa.** Ao reescrever `AutenticacaoController.cs`, a diretiva
`using Microsoft.AspNetCore.Authentication;` foi perdida. Os métodos
`SignInAsync`/`SignOutAsync` são métodos de extensão desse namespace.

**Correção.** Reintroduzir o `using`. Lição: ao reescrever um arquivo inteiro,
comparar a lista de `using` com a versão anterior.

### E-03 — `CS0246`: tipo `Favorito` não encontrado (teste)

**Sintoma.** Três erros no arquivo de teste de `Favorito`.

**Causa.** O arquivo de teste não importava `RarityRoom.Modelos`, apenas
`RarityRoom.Modelos.Excecoes`.

**Correção.** Adicionar o `using` faltante.

### E-04 — `CS1739`: parâmetro `solicitanteId` inexistente em `Listar`

**Sintoma.** Apontado pelo compilador no teste de propostas.

**Causa.** O parâmetro de `ServicoPropostas.Listar` chama-se `usuarioId`; o
teste usou o nome de outro método.

**Correção.** Ajustar a chamada. O compilador impediu que o erro chegasse ao
teste.

### E-05 — Aviso `xUnit2031`

**Sintoma.** `Do not use a Where clause to filter before calling Assert.Single.`

**Causa.** `Assert.Single(lista.Where(...))` filtra antes de contar, o que
esconde a intenção.

**Correção.** Usar a sobrecarga com predicado: `Assert.Single(lista, p => ...)`.
Meta do projeto: build sem avisos.

### E-06 — `Value cannot be null (Parameter 'path1')` no NuGet

**Sintoma.** `dotnet build` falhava antes de qualquer compilação, apenas na
restauração.

**Causa.** O shell usado não herdava `APPDATA` e `ProgramFiles`; o NuGet
resolve caminhos a partir delas. Verificado por tentativa e erro: repor as duas
variáveis faz o restore funcionar.

**Correção.** Executar com o ambiente explícito:
`env APPDATA=".../AppData/Roaming" ProgramFiles="C:/Program Files" dotnet build`.

### E-07 — Heredoc truncado pelo shell

**Sintoma.** `syntax error: unexpected end of file` ao gravar um arquivo
grande de uma vez.

**Causa.** O comando excedia o limite de tamanho aceito pelo shell.

**Correção.** Gravar em blocos sequenciais (`cat >` seguido de `cat >>`) e
conferir a contagem de linhas ao final. Nenhum arquivo parcial foi compilado.

## Lições

1. Erro de cópia de binário é ambiente, não código — verificar processos antes
   de suspeitar da implementação.
2. Reescrever arquivo inteiro custa `using` esquecido; comparar com o original.
3. O compilador pega erro de nome de parâmetro; o teste pega erro de regra.
   São camadas diferentes de proteção.
4. Defeito de identificador e de autorização nasce de **modelo incompleto**
   (sem dono, sem contador), não de uma linha mal escrita.
