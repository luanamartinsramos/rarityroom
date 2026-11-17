# 🛍️ RarityRoom

O **RarityRoom** é uma plataforma de compra e venda de itens colecionáveis
do universo geek. O projeto foi desenvolvido com o objetivo de criar uma
experiência moderna para usuários que desejam cadastrar, visualizar e
gerenciar produtos colecionáveis.

O projeto possui um **frontend desenvolvido em React + TypeScript** e um
**backend desenvolvido em ASP.NET Core**, organizados em um único
repositório (monorepo).


## Integrantes

| Nome completo | Matrícula |
|---|---|
| Luana Martins Ramos | 06014936 |
| Jeorgia Luiza da Rosa Canto | 06014796 |
| Pedro Henrique Canto da Silva | 06013162 |
| Lucas Ribeiro Dias | 06015364 |
| Miguel Souza dos Santos | 06014237 |

**Disciplina:** Design e Programação Orientados a Objetos — Prof. André Campos (AV2).

## Documentação técnica da AV2

Tudo em [`docs/av2/`](docs/av2/), com índice em
[`docs/av2/evidencias.json`](docs/av2/evidencias.json):

| Documento | Conteúdo |
|---|---|
| [`especificacao.md`](docs/av2/especificacao.md) | 3 fluxos, 5 regras de negócio, estados e critérios de aceitação |
| [`requisitos.md`](docs/av2/requisitos.md) | requisitos funcionais e regras complementares |
| [`modelo-de-classes.md`](docs/av2/modelo-de-classes.md) | modelo de classes (Mermaid) conferido com o código |
| [`decisoes-de-modelagem.md`](docs/av2/decisoes-de-modelagem.md) | decisões com comparação de alternativas |
| [`code-review.md`](docs/av2/code-review.md) | revisão de uma alteração funcional do histórico Git |
| [`excecoes.md`](docs/av2/excecoes.md) | situações de falha, propagação e tratamento |
| [`debugging.md`](docs/av2/debugging.md) | defeito reproduzido, hipóteses, técnica, causa e correção |
| [`testes.md`](docs/av2/testes.md) | suíte automatizada e rastreabilidade |
| [`especificacao-dscode.md`](docs/av2/especificacao-dscode.md) | especificação para agente + verificação do resultado |
| [`auditoria-final.md`](docs/av2/auditoria-final.md) | os 40 itens da rubrica, evidências e pendências |

---

## ✨ Sobre o projeto

O RarityRoom foi desenvolvido como um projeto acadêmico e prático para
aplicar conceitos de desenvolvimento web moderno, integração entre
frontend e backend, autenticação de usuários, gerenciamento de produtos e
organização de uma aplicação em arquitetura separada por responsabilidades.

A plataforma permite que usuários tenham uma área própria para acessar sua
conta e gerenciar produtos disponíveis para venda.

A proposta visual busca trazer uma identidade inspirada no universo geek,
com uma interface moderna, escura e focada na apresentação dos produtos.

---

## 🚀 Funcionalidades

### 👤 Autenticação

- Cadastro de usuários
- Login
- Logout
- Identificação do usuário autenticado
- Proteção de páginas que exigem autenticação

### 📦 Produtos

- Cadastro de produtos
- Visualização de produtos
- Edição de produtos
- Exclusão de produtos
- Informações como:
  - Nome
  - Descrição
  - Preço
  - Estoque
  - Categoria
  - Marca
  - Coleção
  - Condição
  - Imagem

### 🛒 Experiência da plataforma

- Página inicial personalizada
- Busca de produtos
- Filtro por categoria
- Favoritos
- Carrinho
- Área de pedidos
- Interface responsiva

---

# 💻 Como baixar e executar

Pré-requisitos: **Git**, **.NET SDK 10** e **Node.js 20+**.

## 1. Clonar

```bash
git clone https://github.com/luanamartinsramos/rarityroom.git
cd rarityroom
```

## 2. Backend (API ASP.NET Core — http://localhost:5000)

```bash
cd backend
dotnet restore
dotnet build
dotnet run
```

**Banco de dados:** não é preciso preparar nada à mão. Na primeira execução o
backend cria o arquivo SQLite `backend/rarityroom.db` e aplica todas as
migrações de `backend/Dados/Migracoes` (`Database.Migrate()`), além de cadastrar
as categorias e a conta de administrador `admin@rarityroom.com` / `admin123`.
Para começar do zero, pare o backend e apague `rarityroom.db`.

## 3. Frontend (React + Vite — http://localhost:5173)

Em outro terminal, a partir da raiz do repositório:

```bash
cd frontend
npm install
npm run dev
```

Frontend e backend precisam estar rodando ao mesmo tempo. Em desenvolvimento o
frontend chama a API em `http://localhost:5000`.

## 4. Testes automatizados

A partir da raiz do repositório:

```bash
cd tests/RarityRoom.Tests
dotnet test
```

Os testes de integração sobem a API em memória com um banco SQLite
temporário — não usam nem alteram o `rarityroom.db` de desenvolvimento.

# 📖 Swagger

O Backend possui Swagger para visualizar e testar os endpoints da API.

Com o Backend em execução, acesse:

http://localhost:5000/swagger

---

# 🧱 Arquitetura do Backend

O backend evoluiu de um CRUD para um domínio com regras próprias.

```
backend/
├── Controller/     entrada HTTP (não decide regra)
├── Services/       orquestração e autorização
├── Models/         entidades, enums, exceções e estratégias
│   ├── Enums/      EstadoAnuncio, OperacaoAnuncio, EstadoProposta, ModalidadeNegociacao
│   ├── Excecoes/   ExcecaoDominio + 5 tipos
│   └── Negociacao/ IEstrategiaNegociacao + 3 implementações
├── Repositories/   persistência com EF Core
├── Dados/          DbContext e migrações versionadas
└── Middleware/     tradução de exceção em ProblemDetails
```

## Classes de domínio

| Classe | Papel |
|---|---|
| `Usuario` | conta, com senha apenas em hash PBKDF2 |
| `Produto` | anúncio: dono, preço, estoque e ciclo de vida |
| `Categoria` | catálogo, com slug normalizado |
| `Proposta` | oferta de compra com ciclo próprio |
| `Favorito` | vínculo usuário ↔ anúncio |
| `Pedido` | compra com snapshot de nome e preço |
| `Avaliacao` | nota de 1 a 5 do comprador sobre o vendedor |

**Polimorfismo em uso:** `IEstrategiaNegociacao` define o contrato; `PrecoFixo`
(100% do valor), `AceitaOferta` (85%) e `Liquidacao` (70%) são implementações
reais. Uma mesma oferta de R$ 200 sobre um anúncio de R$ 249,90 é recusada no
preço fixo e aceita na liquidação.

# 🧪 Testes

```bash
cd tests/RarityRoom.Tests
dotnet test
```

Resultado esperado: **235 testes aprovados, 0 falhas** (detalhes em `docs/av2/testes.md`). A suíte
`Api/MarketplaceApiTests.cs` cobre, pela API HTTP real, pesquisa, as 7
ordenações, carrinho e limites de estoque, checkout, PIX/boleto/cartão
simulados, cancelamento, expiração, compras simultâneas do mesmo estoque,
pedidos duplicados e as regras de segurança.

Os testes ficam fora de `backend/` porque o SDK Web compila todo `*.cs` da
pasta do projeto — uma pasta de testes dentro do backend entraria no build da
aplicação.

# 🔌 Endpoints principais

| Método | Rota | Autenticação |
|---|---|---|
| POST | `/api/autenticacao/cadastrar` | pública |
| POST | `/api/autenticacao/entrar` | pública |
| POST | `/api/autenticacao/sair` | cookie |
| GET | `/api/autenticacao/usuário-atual` | cookie |
| GET | `/api/produtos?termo=&categoria=&ordem=&pagina=&tamanhoPagina=` | pública (busca; total em `X-Total-Count`) |
| GET | `/api/produtos/meus` | cookie (anúncios do usuário, qualquer estado) |
| GET | `/api/produtos/{id}` | pública |
| POST | `/api/produtos` | cookie |
| PUT | `/api/produtos/{id}` | cookie (só o dono) |
| PATCH | `/api/produtos/{id}/estado` | cookie (só o dono) |
| DELETE | `/api/produtos/{id}` | cookie (só o dono) |
| GET | `/api/categorias` | pública |
| POST | `/api/categorias` | cookie |
| GET | `/api/favoritos` | cookie |
| POST/DELETE | `/api/favoritos/{produtoId}` | cookie |
| GET/POST | `/api/propostas` | cookie |
| GET/POST/PUT/DELETE | `/api/carrinho`, `/api/carrinho/itens/{produtoId}` | cookie |
| GET | `/api/pedidos`, `/api/pedidos/{id}` | cookie (só participantes) |
| POST | `/api/pedidos` (checkout a partir do carrinho) | cookie |
| GET | `/api/pagamentos/{id}` | cookie (só o comprador) |
| POST | `/api/pagamentos/{id}/simular-confirmacao` \| `/cartao` \| `/simular-expiracao` \| `/cancelar` | cookie (só o comprador) |
| POST | `/api/pedidos/{id}/enviar` \| `/entregar` | cookie (vendedor \| comprador) |
| POST | `/api/pedidos/{id}/concluir` | cookie (vendedor) |
| POST | `/api/pedidos/{id}/cancelar` | cookie |
| POST | `/api/avaliacoes` | cookie (comprador) |
| GET | `/api/avaliacoes/vendedor/{id}` | pública |
| POST | `/api/produtos/{id}/imagem` | cookie (dono) |
| POST | `/api/propostas/{id}/aceitar` \| `/recusar` \| `/cancelar` | cookie |

Estados de erro seguem o padrão `ProblemDetails` com um campo `codigo`:
`dados-invalidos`, `acesso-negado`, `nao-encontrado`, `conflito`,
`transicao-invalida`.

# 🛒 Fluxo de compra e pagamento simulado

1. O comprador adiciona produtos ao carrinho (catálogo ou página do produto).
   O carrinho fica no banco, vinculado à conta, e **não** reserva estoque.
2. No checkout o servidor valida itens, preços e estoque, cria a compra
   "aguardando pagamento" e **um pedido por vendedor**. O carrinho continua
   intacto.
3. Na página de pagamento o comprador conclui a simulação. Só na aprovação o
   estoque é baixado (em transação, com controle de concorrência) e os itens
   saem do carrinho.

> ⚠️ **Nenhum pagamento é real.** Não há gateway, banco ou API financeira.
> O QR Code/“copia e cola” do PIX e a linha digitável do boleto são
> fictícios (banco 999) e não têm validade. Do cartão, só os 4 últimos
> dígitos, a bandeira e as parcelas são gravados.

| Forma | Como simular |
|---|---|
| PIX | Botão **Simular pagamento confirmado** (ou **Simular expiração**). Prazo: 30 min. |
| Boleto | Botão **Simular pagamento confirmado** (ou **Simular vencimento**). Vencimento: 3 dias. Pode ser impresso. |
| Cartão | `4111 1111 1111 1111` → aprovado · `4000 0000 0000 0002` → recusado · `4000 0000 0000 9995` → recusado por limite. Qualquer outro número válido (Luhn) é aprovado. Validade futura, CVV com 3 dígitos (4 no Amex). |

Estados do pagamento: aguardando pagamento, aprovado, recusado, expirado,
compra cancelada e estornado (pedido pago cancelado antes do envio; as
unidades voltam ao estoque).

# 🌐 Publicação online (Vercel + Render)

A Vercel hospeda só o **frontend**; o **backend** (.NET) roda no **Render**
(plano gratuito, a partir do `backend/Dockerfile`). A Vercel repassa `/api` e
`/uploads` para o Render (`frontend/vercel.json`), então o site funciona como
um domínio só e o login por cookie não precisa de CORS.

1. **Backend (Render):** <https://dashboard.render.com> → **New → Blueprint** →
   escolha este repositório. O `render.yaml` cria o serviço `rarityroom-api`.
   Se o Render der outro endereço que não `https://rarityroom-api.onrender.com`,
   troque-o nos dois `destination` de `frontend/vercel.json`.
2. **Frontend (Vercel):** <https://vercel.com/new> → importe este repositório →
   em **Root Directory** escolha `frontend` → **Deploy**.

Limitações do plano gratuito do Render:
- o backend "dorme" após 15 min sem acesso; o primeiro acesso depois disso
  leva ~1 minuto;
- o SQLite fica no disco temporário do container: **os dados (contas, anúncios,
  pedidos) são apagados quando o serviço reinicia ou é publicado de novo**.
  As categorias e a conta `admin@rarityroom.com` / `admin123` são recriadas
  automaticamente. Para dados permanentes é preciso um disco persistente
  (plano pago) ou outro banco.

Localmente nada muda: o frontend usa `http://localhost:5000` em modo
desenvolvimento (pode ser trocado com a variável `VITE_API_ORIGIN`).

# 📚 Documentação AV2

| Arquivo | Conteúdo |
|---|---|
| `docs/av2/requisitos.md` | requisitos funcionais, regras de negócio e critérios de aceitação |
| `docs/av2/modelo-de-classes.md` | diagrama, responsabilidades e colaboração |
| `docs/av2/decisoes-de-modelagem.md` | 10 decisões com contexto e consequência |
| `docs/av2/code-review.md` | 10 achados de revisão com correção e evidência |
| `docs/av2/debugging.md` | defeitos investigados e episódios de depuração |
| `docs/av2/testes.md` | estratégia, distribuição e rastreabilidade |
| `docs/av2/auditoria-final.md` | 40 critérios com situação e evidência |
| `docs/av2/evidencias.json` | índice legível por máquina |

# 🗄️ Banco de dados

O backend usa **SQLite com EF Core**. O banco é criado automaticamente na
primeira execução, a partir da migração versionada em
`backend/Dados/Migracoes/`.

```bash
cd backend
dotnet run          # aplica a migração e cria rarityroom.db
```

- Arquivo gerado: `backend/rarityroom.db` (ignorado pelo git).
- Tabelas: `usuarios`, `categorias`, `produtos`, `propostas`, `favoritos`, `pedidos`, `avaliacoes`.
- As 7 categorias iniciais são semeadas de forma idempotente na inicialização.
- Para recomeçar do zero, apague o `rarityroom.db` e rode de novo.

Para usar outro caminho de banco, defina a variável de ambiente:

```bash
ConnectionStrings__RarityRoom="Data Source=C:/dados/rarityroom.db" dotnet run
```

# 🖼️ Upload de imagem

```bash
curl -b cookies.txt -F "arquivo=@foto.png;type=image/png" \
  http://localhost:5000/api/produtos/1/imagem
```

Regras: somente o dono envia; JPG, PNG ou WEBP; até 2 MB. O arquivo é gravado
em `backend/wwwroot/uploads/` com nome gerado pelo servidor e servido em
`http://localhost:5000/uploads/<nome>`.

# ⚠️ Limitações conhecidas

- Busca e filtro são feitos no frontend; não há paginação na API.
- Upload valida tamanho e tipo declarado, mas não inspeciona o conteúdo binário.
- Pagamento e envio ficam fora do escopo: o pedido termina na conclusão pelo
  vendedor.
- Não há teste contra PostgreSQL ou SQL Server.
