# Debugging

Registro dos defeitos investigados, com sintoma, causa e correção.

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
