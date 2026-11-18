# Depuração

> Critérios: **E01** (defeito reproduzível com evidência), **E02** (condições,
> esperado × observado), **E03** (hipóteses e investigação), **E04** (técnica de
> depuração comprovada), **E05** (causa, correção e eliminação), **E06** (antes
> e depois + comparação de técnicas).

## DEF-01 — Estorno de compra paga deixa o estoque negativo

Defeito **real**. Foi introduzido no commit `78cae8b` (o mesmo revisado em
[`code-review.md`](code-review.md)), apontado no teste de segurança e
corrigido no commit `116d245`. A investigação abaixo foi refeita do zero em
2026-11-18 sobre a versão **`166c21d`**, a última antes da correção, numa
`git worktree` separada.

### 1. Reprodução (E01)

**Teste de reprodução:**
`tests/RarityRoom.Tests/Api/DepuracaoEstoqueTests.cs` →
`EstornoDeCompraPaga_ComEstoqueNoLimite_NaoDeixaEstoqueNegativo`. Ele percorre
o caminho real (HTTP → `ServicoPedidos` → `Produto` → SQLite) e escreve no
log do teste os valores observados em cada passo.

**Como reproduzir:**

```bash
git worktree add ../rr-antes 166c21d
cp tests/RarityRoom.Tests/Api/DepuracaoEstoqueTests.cs ../rr-antes/tests/RarityRoom.Tests/Api/
cd ../rr-antes
dotnet test tests/RarityRoom.Tests --filter DepuracaoEstoqueTests --logger "trx;LogFileName=antes.trx"
```

**Evidência (saída do teste em `166c21d`):**

```
Com falha! – Com falha: 1, Aprovado: 0, Total: 1

[1] compra paga        → estoque = 1
[2] PUT estoque=2147483647 → HTTP 200; estoque = 2147483647
[3] cancelar pedido pago → HTTP 200; estoque = -2147483648
Mensagem da falha: Estoque negativo após estorno: -2147483648
```

O defeito é determinístico: ocorre em 100% das execuções.

### 2. Condições, esperado e observado (E02)

| | |
|---|---|
| **Condições de reprodução** | (1) vendedor anuncia com estoque 2; (2) comprador compra 1 unidade e o PIX simulado é aprovado (estoque 1); (3) vendedor edita o anúncio para estoque `2147483647` (`int.MaxValue`); (4) comprador cancela o pedido pago, o que dispara o estorno e devolve 1 unidade |
| **Comportamento esperado** (R5, CA-03.3) | o estorno é aceito e o estoque continua válido: **≥ 0** e dentro de um limite razoável |
| **Comportamento observado** | o estorno responde `200`, mas o estoque gravado é **-2147483648**. O anúncio fica com estoque negativo, o que a regra R5 proíbe, e nenhuma camada reclama |
| **Ambiente** | .NET SDK 10.0.404, SQLite, Linux; versão `166c21d` |

### 3. Hipóteses e investigação (E03)

| # | Hipótese | Como testar | Resultado |
|---|---|---|---|
| **H1** | A soma `Estoque += quantidade` em `Produto.ReporEstoque` estoura o `int` (aritmética sem verificação) | registrar o valor antes e depois da soma; repetir a soma em contexto `checked` | **confirmada** |
| H2 | O valor é corrompido na gravação (conversão do EF Core / SQLite) | registrar o valor que o repositório recebe para gravar e compará-lo com o valor em memória | **descartada** |
| H3 | O cancelamento repõe o estoque mais de uma vez ou com a quantidade errada | contar as chamadas de `ReporEstoque` e o parâmetro recebido | **descartada** |

**Redução do espaço de busca.** O estoque só é escrito em três pontos:
`AplicarDados` (cadastro e edição), `BaixarEstoque` e `ReporEstoque`. O passo
[2] mostra que a edição gravou exatamente `2147483647`, e o valor só fica
errado depois do passo [3], o cancelamento. Isso deixa como suspeitos
`ReporEstoque` e a gravação que vem logo depois (H1, H2, H3).

### 4. Técnica aplicada: rastreamento por log (E04)

Na worktree de `166c21d`, a execução foi instrumentada com três pontos de
log temporários, que gravam num arquivo de rastreamento:

```diff
// backend/Models/Produto.cs — ReporEstoque
+   File.AppendAllText(trace, $"[TRACE ReporEstoque] produto={Id} estoqueAntes={Estoque} quantidade={quantidade} somaEmLong={(long)Estoque + quantidade}\n");
    Estoque += quantidade;
+   File.AppendAllText(trace, $"[TRACE ReporEstoque] produto={Id} estoqueDepois={Estoque} (em memória, antes de gravar)\n");

// backend/Repositories/RepositorioProdutoEf.cs — Atualizar
+   File.AppendAllText(trace, $"[TRACE Repositorio.Atualizar] produto={produto.Id} estoqueAGravar={produto.Estoque}\n");
```

**Rastro obtido** (mesmo teste de reprodução):

```
[TRACE Repositorio.Atualizar] produto=1 estoqueAGravar=1
[TRACE Repositorio.Atualizar] produto=1 estoqueAGravar=2147483647
[TRACE ReporEstoque] produto=1 estoqueAntes=2147483647 quantidade=1 somaEmLong=2147483648
[TRACE ReporEstoque] produto=1 estoqueDepois=-2147483648 (em memória, antes de gravar)
[TRACE Repositorio.Atualizar] produto=1 estoqueAGravar=-2147483648
```

**Leitura do rastro:**

- **H3 descartada:** há **uma** chamada a `ReporEstoque`, com `quantidade=1`,
  que é o correto para um pedido de 1 unidade.
- **H2 descartada:** o valor já está negativo **em memória**
  (`estoqueDepois=-2147483648`), antes de chegar ao repositório. O repositório
  grava exatamente o que recebeu.
- **H1 confirmada:** a soma matemática é `2147483648` (`somaEmLong`), um a
  mais que `int.MaxValue`, e o campo `int` volta para o menor valor.

**Experimento de confirmação de H1** (programa isolado de 5 linhas):

```
unchecked: 2147483647 + 1 = -2147483648
checked: OverflowException: Arithmetic operation resulted in an overflow.
```

O C# compila com aritmética `unchecked` por padrão. O estouro acontece em
silêncio, e por isso nenhuma exceção apareceu.

Os pontos de log foram removidos depois da investigação. Eles nunca
entraram no repositório.

### 5. Causa raiz e correção (E05)

**Causa raiz.** Não é "uma linha com `+=`" (isso é a *localização*). A causa
é que a invariante do estoque **não tinha teto**, e o método novo
`ReporEstoque` escrevia o campo sem passar pela validação que já existia em
`EstoqueValido`. Com estoque ilimitado (até `int.MaxValue`), qualquer
reposição podia ultrapassar o tipo.

**Correção** (commit `116d245`, `backend/Models/Produto.cs`), a menor
correção coerente nos dois pontos da causa:

```diff
+    public const int EstoqueMaximo = 100_000;
 ...
     // ReporEstoque
-    Estoque += quantidade;
+    // Soma em long e limita ao teto: nunca estoura nem fica negativo.
+    Estoque = (int)Math.Min((long)Estoque + quantidade, EstoqueMaximo);
 ...
     // EstoqueValido — usado pelo construtor e por Atualizar
+    if (estoque > EstoqueMaximo)
+        throw new ValidacaoException("O estoque deve ser de no máximo 100.000 unidades.");
```

**Eliminação do comportamento incorreto.** O mesmo teste de reprodução,
sem nenhuma alteração, executado na `main`:

```
Aprovado! – Com falha: 0, Aprovado: 1, Total: 1

[1] compra paga        → estoque = 1
[2] PUT estoque=2147483647 → HTTP 400; estoque = 1
[3] cancelar pedido pago → HTTP 200; estoque = 2
```

A edição para um valor absurdo agora é recusada (`400`), e o estorno devolve
a unidade corretamente (1 → 2).

### 6. Antes × depois e comparação de técnicas (E06)

**Verificação antes e depois** (mesmo teste, mesmo cenário):

| Passo | `166c21d` (antes) | `main` (depois) |
|---|---|---|
| Editar estoque para `int.MaxValue` | `200`, aceito | `400 dados-invalidos` |
| Estorno de 1 unidade | estoque **-2147483648** | estoque **2** |
| Resultado do teste | **falha** | **aprovado** |

**Não regressão:** o teste de reprodução ficou permanente na suíte, junto com
`ReporEstoque_NoTeto_NaoEstouraNemFicaNegativo` (domínio: repor
`int.MaxValue` sobre o teto mantém 100.000) e
`Anuncio_ComEstoqueAcimaDoTeto_EhRecusado` (API). Suíte completa após a
correção: **245 aprovados, 0 falhas**.

**Comparação: rastreamento por log × breakpoint com inspeção de variáveis**

| Critério | Rastreamento por log (usado) | Breakpoint + inspeção (alternativa) |
|---|---|---|
| Ver o valor **antes e depois** da soma e na gravação | sim, os três pontos numa mesma execução | sim, parando em cada ponto |
| Separar H1 de H2 (memória × banco) | direto: o rastro mostra a ordem dos eventos | possível, mas exige parar no repositório e lembrar o valor anterior |
| Execução dentro do servidor ASP.NET com `WebApplicationFactory` | funciona sem configuração | exige anexar o depurador ao processo de teste (`netcoredbg` / IDE) |
| Evidência que pode ser guardada e mostrada a terceiros | **o próprio rastro** (colado acima) | screenshots ou anotações manuais |
| Repetição | reexecuta o teste e compara a saída | refazer os passos de inspeção a cada execução |
| Custo / intrusão | altera o código (temporariamente) e exige limpeza | não altera o código |
| Explorar o que não foi previsto | fraco: só mostra o que foi registrado | **forte**: inspeciona qualquer variável na hora |

**Recomendação.** Para este defeito, determinístico e com suspeitos já
reduzidos a três pontos de escrita, o **rastreamento por log** foi
superior: em uma execução separou "erro de cálculo" de "erro de gravação" e
deixou uma evidência que pode ser auditada. O **breakpoint** seria a escolha
melhor se o espaço de busca ainda estivesse aberto (não se soubesse *onde* o
estoque mudava), porque permite explorar o estado sem decidir antes o que
registrar. Uma terceira alternativa, **`git bisect`** com o teste de
reprodução como critério, localizaria o commit que introduziu o defeito
(`78cae8b`), mas não explicaria *por que* o valor fica negativo.

---

## Registros históricos de depuração

Casos investigados na primeira fase do projeto (AV1 → AV2). Mantidos como
registro; o caso que atende aos critérios E01–E06 é o DEF-01 acima.

### Caso 1 — Anúncio criado com id duplicado

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

### Caso 2 — Terceiro edita e exclui anúncio alheio

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

### Caso 3 — Colisão de transições indexadas só pelo destino

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

### Erros de build e ambiente durante a implementação

Falhas reais enfrentadas e resolvidas durante o trabalho.

#### E-01 — `MSB3027`: não foi possível copiar o executável

**Sintoma.**
`error MSB3027: não foi possível copiar "...apphost.exe" para "...RarityRoom.exe". Número de novas tentativas 10 excedido. O arquivo é bloqueado por: "RarityRoom (26816)"`

**Causa.** O servidor de testes anterior continuava em execução e mantinha o
`.exe` aberto; o `dotnet build` não consegue sobrescrever o binário em uso.

**Correção.** Encerrar o processo antes de compilar:
`taskkill /F /IM RarityRoom.exe`. Nenhuma alteração de código foi necessária —
o erro não era do projeto.

#### E-02 — `CS1061`: `HttpContext` não contém `SignInAsync`

**Sintoma.** Erro de compilação nos dois pontos de login/logout.

**Causa.** Ao reescrever `AutenticacaoController.cs`, a diretiva
`using Microsoft.AspNetCore.Authentication;` foi perdida. Os métodos
`SignInAsync`/`SignOutAsync` são métodos de extensão desse namespace.

**Correção.** Reintroduzir o `using`. Lição: ao reescrever um arquivo inteiro,
comparar a lista de `using` com a versão anterior.

#### E-03 — `CS0246`: tipo `Favorito` não encontrado (teste)

**Sintoma.** Três erros no arquivo de teste de `Favorito`.

**Causa.** O arquivo de teste não importava `RarityRoom.Modelos`, apenas
`RarityRoom.Modelos.Excecoes`.

**Correção.** Adicionar o `using` faltante.

#### E-04 — `CS1739`: parâmetro `solicitanteId` inexistente em `Listar`

**Sintoma.** Apontado pelo compilador no teste de propostas.

**Causa.** O parâmetro de `ServicoPropostas.Listar` chama-se `usuarioId`; o
teste usou o nome de outro método.

**Correção.** Ajustar a chamada. O compilador impediu que o erro chegasse ao
teste.

#### E-05 — Aviso `xUnit2031`

**Sintoma.** `Do not use a Where clause to filter before calling Assert.Single.`

**Causa.** `Assert.Single(lista.Where(...))` filtra antes de contar, o que
esconde a intenção.

**Correção.** Usar a sobrecarga com predicado: `Assert.Single(lista, p => ...)`.
Meta do projeto: build sem avisos.

#### E-06 — `Value cannot be null (Parameter 'path1')` no NuGet

**Sintoma.** `dotnet build` falhava antes de qualquer compilação, apenas na
restauração.

**Causa.** O shell usado não herdava `APPDATA` e `ProgramFiles`; o NuGet
resolve caminhos a partir delas. Verificado por tentativa e erro: repor as duas
variáveis faz o restore funcionar.

**Correção.** Executar com o ambiente explícito:
`env APPDATA=".../AppData/Roaming" ProgramFiles="C:/Program Files" dotnet build`.

#### E-07 — Heredoc truncado pelo shell

**Sintoma.** `syntax error: unexpected end of file` ao gravar um arquivo
grande de uma vez.

**Causa.** O comando excedia o limite de tamanho aceito pelo shell.

**Correção.** Gravar em blocos sequenciais (`cat >` seguido de `cat >>`) e
conferir a contagem de linhas ao final. Nenhum arquivo parcial foi compilado.

### Lições

1. Erro de cópia de binário é ambiente, não código — verificar processos antes
   de suspeitar da implementação.
2. Reescrever arquivo inteiro custa `using` esquecido; comparar com o original.
3. O compilador pega erro de nome de parâmetro; o teste pega erro de regra.
   São camadas diferentes de proteção.
4. Defeito de identificador e de autorização nasce de **modelo incompleto**
   (sem dono, sem contador), não de uma linha mal escrita.
