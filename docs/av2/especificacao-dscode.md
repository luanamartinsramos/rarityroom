# Especificação para o agente DsCode — "Conta única"

> Critério **G02**: especificação destinada ao DsCode, associada a uma
> alteração funcional, com evidências de inspeção e verificação do resultado.

## 1. Origem e associação

| | |
|---|---|
| **Agente** | DsCode, com o repositório aberto como workspace (ver `management/cross-session/av2-handoff.md`, cabeçalho) |
| **Data da execução** | 2026-11-14 |
| **Alteração funcional** | uma conta passa a comprar **e** vender; deixa de existir o papel exclusivo de comprador ou de vendedor (regra RC-09 de [`requisitos.md`](requisitos.md)) |
| **Registro da execução** | `management/cross-session/av2-handoff.md`, seção "Progresso — Conta única (comprador e vendedor) (2026-11-14)" |
| **Onde o diff está no histórico** | `f8f22e7`: `git show f8f22e7 -- backend/Services/ServicoAutenticacao.cs` mostra `TipoConta.Ambos` no cadastro e o comentário "Toda conta nasce compradora e vendedora". `fef6d8f`: o teste `ContaUnica_PodePublicarEComprar` |

**Transparência sobre a origem do texto.** O pedido original foi feito na
sessão do DsCode e **o texto literal não foi versionado**. A §2 reconstrói a
especificação a partir do registro dessa sessão no handoff: o objetivo ("o
escopo do MVP determina uma única conta"), as mudanças pedidas, o que foi
preservado e o "cenário da spec" provado por HTTP. Ela é formalizada aqui no
formato que o grupo passa a usar para pedidos ao agente. As evidências da §3
são as registradas na execução; a inspeção da §4 foi refeita sobre o código
atual.

---

## 2. Especificação (reconstruída e formalizada)

### Contexto

RarityRoom é um marketplace (ASP.NET Core 10 + EF Core/SQLite no `backend/`,
React no `frontend/`, testes xUnit em `tests/RarityRoom.Tests`). Hoje o
cadastro pergunta "Comprar ou Vender" e grava `Usuario.TipoConta`. Com isso,
`PodeComprar`/`PodeVender` bloqueiam a operação inversa. O escopo do MVP
definido pelo grupo é **uma única conta** que compra e vende.

### Objetivo

Toda conta pode publicar anúncios, comprar e ter loja, sem escolher papel no
cadastro.

### Escopo permitido

- `backend/Services/ServicoAutenticacao.cs` e `IServicoAutenticacao.cs`:
  remover o parâmetro `tipoConta` de `Cadastrar` e criar toda conta como
  `TipoConta.Ambos`.
- `ServicoAnuncios.Criar`, `ServicoPedidos.Criar`, `ServicoLojas.Criar`:
  remover os bloqueios por papel.
- `ModeloCadastro`: remover o campo `tipoConta`.
- Frontend: remover a pergunta de tipo no cadastro e os condicionais
  `podeVender` na home.
- Testes que codificavam a regra antiga: ajustar ou remover, **declarando**
  cada um.

### Fora do escopo (não fazer)

- **Não** remover o enum `TipoConta` nem a coluna no banco: contas antigas
  continuam válidas. Sem migração destrutiva.
- **Não** mexer em autenticação por cookie, hash de senha ou regras de
  anúncio, proposta e pedido que não dependam do papel.
- **Não** alterar as demais regras de negócio, em especial a R4 ("ninguém
  compra ou propõe no próprio anúncio"), que continua valendo.

### Critérios de aceitação

1. **Dado** um cadastro sem `tipoConta`, **quando** a conta é criada,
   **então** a resposta é `200` e a conta é `Ambos`.
2. **Dadas** duas contas A e B recém-criadas, **quando** A publica e B publica,
   **então** ambos recebem `201`.
3. **Quando** A compra de B **e** B compra de A, **então** ambos recebem `201`.
4. **Quando** qualquer conta cria loja, **então** recebe `201`.
5. **Dado** o próprio anúncio, **quando** a conta tenta comprá-lo, **então**
   continua recebendo `400` (R4 preservada).
6. `dotnet build` sem avisos e `dotnet test` 100% verde.

### Plano pedido ao agente

Antes de editar, listar os arquivos que serão tocados e os testes que mudam
de significado. Depois, executar a mudança e provar cada critério de
aceitação por teste automatizado **e** por chamada HTTP real.

---

## 3. Evidências da execução pelo agente (registro de 2026-11-14)

Do handoff, seção "Progresso — Conta única":

**Mudanças declaradas pelo agente:** bloqueios removidos em
`ServicoAnuncios.Criar`, `ServicoPedidos.Criar` e `ServicoLojas.Criar`;
parâmetro `tipoConta` removido de `IServicoAutenticacao.Cadastrar` e do
`ModeloCadastro`; pergunta removida do cadastro; enum mantido como registro
histórico.

**Testes ajustados (declarados, não escondidos):**

| Antes | Depois |
|---|---|
| `ContaDeComprador_NaoPodePublicarAnuncio` | `ContaUnica_PodePublicarEComprar` |
| `ContaDeVendedor_NaoPodeComprar` | removido (a regra deixou de existir) |
| `Loja_ContaDeComprador_NaoPodeCriar` | `Loja_QualquerContaPodeCriar` |

**Prova HTTP registrada pelo agente:**

```
cadastro sem tipoConta (A e B)     → 200
A publica anúncio                  → 201 (produto 7)
B publica anúncio                  → 201 (produto 8)
A compra de B                      → 201
B compra de A                      → 201
A cria loja                        → 201
GET /api/pedidos (conta A)         → pedido como comprador (9) e como vendedor (9)
Validação: build 0/0 · 174 testes
```

## 4. Inspeção do resultado (feita em 2026-11-18, sobre a `main`)

Cada critério de aceitação foi conferido no código atual, sem confiar apenas
no relato do agente.

| Critério | Inspeção | Resultado |
|---|---|---|
| 1 | `ServicoAutenticacao.Cadastrar` cria `new Usuario(nome, email, hash, TipoConta.Ambos)` (linha ~50) | conforme |
| 2, 3 | `grep -rn "PodeVender\|PodeComprar" backend/Services backend/Controller` → **nenhuma ocorrência**: nenhum serviço bloqueia por papel | conforme |
| 2, 3 | `ContaUnica_PodePublicarEComprar`: a mesma conta compra (`201`) e publica (`201`) | **aprovado** |
| 4 | `Loja_QualquerContaPodeCriar` | **aprovado** |
| 5 | `Pedido_NoProprioAnuncio_DeveRetornar400` continua na suíte | **aprovado** (R4 intacta) |
| 6 | `dotnet build backend` → 0 avisos, 0 erros; `dotnet test` → 245/245 | **aprovado** |
| Escopo "não remover o enum" | `Usuario.TipoConta` e o enum `TipoConta` continuam existindo; o frontend exibe o rótulo no perfil | respeitado |
| Testes antigos | `grep` por `ContaDeComprador_NaoPodePublicarAnuncio` e `ContaDeVendedor_NaoPodeComprar` → 0 ocorrências | conforme o declarado |

```
$ dotnet test --filter "FullyQualifiedName~ContaUnica|FullyQualifiedName~Loja_QualquerContaPodeCriar|FullyQualifiedName~ContaNova_DeveTerPedidosVazios"
Aprovado! – Com falha: 0, Aprovado: 3, Total: 3
```

### Achados da inspeção

1. **Código residual:** `Usuario.PodeComprar` e `Usuario.PodeVender` ainda
   existem em `backend/Models/Usuario.cs`, mas nenhum serviço os usa depois
   da mudança. O agente cumpriu o escopo (não remover `TipoConta`), mas deixou
   as duas propriedades derivadas sem uso. **Ação:** remover numa próxima
   alteração. Fica registrado como pendência em
   [`auditoria-final.md`](auditoria-final.md).
2. **Documentação desatualizada pelo agente:** a seção "Comprador, vendedor e
   checkout" do modelo de classes ainda descrevia a "regra de papel" e o
   requisito RF24 ("escolher tipo de conta"). As duas foram **corrigidas
   nesta revisão da documentação** (`modelo-de-classes.md` §8,
   `requisitos.md` RC-09).
3. **Histórico agrupado:** a mudança não ficou em um commit isolado. Ela foi
   publicada dentro de commits temáticos (`f8f22e7` serviços, `fef6d8f`
   testes). Para pedidos futuros ao agente, incluir na especificação: "um
   commit por alteração funcional".

**Conclusão da inspeção:** a alteração atende aos 6 critérios de aceitação e
respeita o escopo proibido. Ficam 2 resíduos (achados 1 e 2), já tratados ou
registrados.
