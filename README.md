# 🛍️ RarityRoom

O **RarityRoom** é uma plataforma de compra e venda de itens colecionáveis
do universo geek. O projeto foi desenvolvido com o objetivo de criar uma
experiência moderna para usuários que desejam cadastrar, visualizar e
gerenciar produtos colecionáveis.

O projeto possui um **frontend desenvolvido em React + TypeScript** e um
**backend desenvolvido em ASP.NET Core**, organizados em um único
repositório (monorepo).


## Integrantes

- Luana Martins Ramos - 06014936
- Jeorgia Luiza da Rosa Canto - 06014796
- Pedro Henrique Canto da Silva - 06013162
- Lucas Ribeiro Dias - 06015364
- Miguel Souza dos Santos - 06014237

## 📚 Documentação da AV2

Toda a evidência técnica da AV2 está em [`docs/av2/`](docs/av2/), com índice
por critério (A01–G02) em [`docs/av2/evidencias.json`](docs/av2/evidencias.json)
e a correspondência completa em [`docs/av2/auditoria-final.md`](docs/av2/auditoria-final.md).

| Documento | Critérios |
|---|---|
| [`requisitos.md`](docs/av2/requisitos.md) — 3 fluxos, 5 regras centrais, estados e critérios de aceitação | A03, A05, G01 |
| [`modelo-de-classes.md`](docs/av2/modelo-de-classes.md) — diagrama Mermaid, multiplicidades, composição, invariantes | B01–B07 |
| [`decisoes-de-modelagem.md`](docs/av2/decisoes-de-modelagem.md) — alternativas comparadas | B08 |
| [`code-review.md`](docs/av2/code-review.md) — revisão do commit `78cae8b` | C01–C06 |
| [`excecoes.md`](docs/av2/excecoes.md) — três situações de falha | D01–D04 |
| [`debugging.md`](docs/av2/debugging.md) — defeito DEF-01 reproduzido, investigado e corrigido | E01–E06 |
| [`testes.md`](docs/av2/testes.md) — testes e matriz de rastreabilidade | F01–F05 |
| [`especificacao-dscode.md`](docs/av2/especificacao-dscode.md) — especificação para o agente | G02 |

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

O Frontend e o Backend estão dentro do mesmo repositório.

Para executar o projeto localmente, é necessário ter instalado:

- Git
- Node.js
- .NET SDK 10

## 1. Clonar o repositório

No terminal:


git clone https://github.com/luanamartinsramos/rarityroom.git

Depois, entre na pasta do projeto:

cd rarityroom
🎨 Frontend

O Frontend foi desenvolvido utilizando:

React
TypeScript
Vite
CSS
React Router
React Hot Toast

Ele é responsável pela interface da aplicação e pela interação do usuário
com a plataforma.

# 2. Entrar na pasta do Frontend
cd frontend
3. Instalar as dependências
npm install

Esse comando instala todas as dependências necessárias para executar o
Frontend.

# 4. Executar o Frontend
npm run dev

O Frontend ficará disponível normalmente em:

http://localhost:5173


# ⚙️ Backend

O Backend foi desenvolvido utilizando:

C#
ASP.NET Core
.NET 10
REST API
JSON
Swagger
Autenticação por Cookies
CORS

Ele é responsável pela autenticação dos usuários e pelas operações
relacionadas aos produtos.

# 5. Abrir o Backend

Abra outro terminal e volte para a pasta principal do projeto:

cd ..

Depois:

cd backend
# 6. Restaurar as dependências
dotnet restore

Esse comando restaura as dependências necessárias do projeto .NET.

# 7. Compilar o Backend
dotnet build

Esse comando verifica se o Backend está compilando corretamente.

# 8. Executar o Backend
dotnet run

O Backend ficará disponível em:

http://localhost:5000

# 🔗 Comunicação entre Frontend e Backend

O Frontend e o Backend funcionam separadamente, mas se comunicam através de
uma API REST.



Para utilizar a aplicação completa localmente, o Frontend e o Backend
devem estar executando ao mesmo tempo.

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

Resultado esperado: **245 testes aprovados, 0 falhas**. A suíte
`Api/MarketplaceApiTests.cs` cobre, pela API HTTP real, pesquisa, as 7
ordenações, carrinho e limites de estoque, checkout, PIX/boleto/cartão
simulados, cancelamento, expiração, compras simultâneas do mesmo estoque,
pedidos duplicados e as regras de segurança. `Api/SegurancaApiTests.cs`
reproduz os ataques encontrados no teste de segurança (tomada de conta pela
recuperação de senha, sessão de conta suspensa, força bruta, upload
disfarçado etc.) e confirma que não funcionam mais.

Os testes ficam fora de `backend/` porque o SDK Web compila todo `*.cs` da
pasta do projeto — uma pasta de testes dentro do backend entraria no build da
aplicação.

# 🔌 Endpoints principais

| Método | Rota | Autenticação |
|---|---|---|
| POST | `/api/autenticacao/cadastrar` | pública |
| POST | `/api/autenticacao/entrar` | pública |
| POST | `/api/autenticacao/sair` | cookie |
| GET | `/api/autenticacao/usuario-atual` | cookie |
| POST | `/api/autenticacao/recuperar-senha` \| `/redefinir-senha` | pública (token chega por e-mail simulado) |
| GET | `/api/produtos?termo=&categoria=&ordem=&pagina=&tamanhoPagina=` | pública (busca; total em `X-Total-Count`) |
| GET | `/api/produtos/meus` | cookie (anúncios do usuário, qualquer estado) |
| GET | `/api/produtos/{id}` | pública |
| POST | `/api/produtos` | cookie |
| PUT | `/api/produtos/{id}` | cookie (só o dono) |
| PATCH | `/api/produtos/{id}/estado` | cookie (só o dono) |
| DELETE | `/api/produtos/{id}` | cookie (só o dono) |
| GET | `/api/categorias` | pública |
| POST | `/api/categorias` | cookie (só administrador) |
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
- As 15 categorias iniciais são semeadas de forma idempotente na inicialização.
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

Regras: somente o dono envia; JPG, PNG ou WEBP; até 2 MB; o conteúdo precisa
ter a assinatura binária do formato (um HTML renomeado para `.png` é recusado).
A permissão é checada antes de gravar, então upload recusado não deixa
arquivo no disco. O arquivo é gravado
em `backend/wwwroot/uploads/` com nome gerado pelo servidor e servido em
`http://localhost:5000/uploads/<nome>`.

# ☁️ Publicação (Render)

Frontend e backend são publicados juntos, num único serviço: o backend
ASP.NET serve a API em `/api` e o frontend React compilado nas demais rotas.
Mesma origem = sem CORS e com o cookie de login funcionando em qualquer
navegador. Em desenvolvimento, o proxy do Vite (`frontend/vite.config.ts`)
reproduz isso encaminhando `/api` e `/uploads` para `localhost:5000`.

O `Dockerfile` da raiz monta a imagem (Node compila o frontend, .NET publica
o backend) e o `render.yaml` descreve o serviço.

1. Crie uma conta em <https://render.com> (dá para entrar com o GitHub).
2. No painel: **New +** → **Blueprint** → escolha este repositório.
3. Quando pedir `Admin__Senha`, digite a senha do administrador
   (mínimo 8 caracteres, com letra e número).
4. Clique em **Apply**. O primeiro build leva alguns minutos; depois o site
   fica em `https://rarityroom.onrender.com` (ou nome parecido).

A cada `git push` na `main` o Render publica de novo sozinho.

**Plano gratuito:** o serviço "dorme" após 15 minutos sem acesso (a primeira
visita depois disso demora cerca de 1 minuto) e **o banco e as imagens são
apagados a cada reinício ou deploy**. Para manter os dados, adicione um disco
persistente ao serviço (plano pago) montado em `/data` — o app já guarda
banco, imagens e chaves de sessão nessa pasta (`Armazenamento__Pasta`).

**Vitrine de demonstração.** Como o banco começa vazio a cada deploy, o
`render.yaml` liga `Demonstracao__SemearProdutos=true`: com o catálogo vazio,
o backend cria a conta "Loja RarityRoom" e 7 anúncios com imagens (embutidas
em `backend/Dados/Demonstracao`). Se o serviço não foi criado pelo Blueprint,
adicione essa variável em **Environment** no painel do Render.

Para testar a imagem localmente:

```bash
docker build -t rarityroom .
docker run -p 8080:8080 -e Admin__Senha=SuaSenha123 rarityroom
# abra http://localhost:8080
```

# 🔐 Segurança e conta de administrador

**Administrador.** A senha não fica no código. Ela vem da configuração
`Admin:Senha` (variável de ambiente `Admin__Senha`; o e-mail padrão é
`admin@rarityroom.com`, alterável por `Admin__Email`):

```bash
cd backend
Admin__Senha="UmaSenhaForte123" dotnet run
```

Sem `Admin__Senha`, em desenvolvimento é gerada uma senha aleatória, mostrada
**uma única vez** no terminal (linha `Conta de administrador: ... senha
gerada: ...`). Fora de desenvolvimento nenhum admin é criado. Um banco antigo
com a senha pública `admin123` tem essa senha trocada na inicialização.

**Recuperação de senha.** O código de redefinição nunca volta na resposta da
API: ele é enviado por um "e-mail simulado", que aparece no terminal do
backend como `[E-MAIL SIMULADO] Para: ... token de redefinição de senha: ...`.
A resposta é a mesma exista ou não o e-mail (não revela quem tem conta).

**Outras proteções**

- Senha com no mínimo 8 caracteres, ao menos uma letra e um número.
- 5 senhas erradas seguidas bloqueiam a conta por 15 minutos (HTTP 429), e
  cada IP tem limite de 20 tentativas de autenticação por minuto
  (`Seguranca__LimiteAutenticacaoPorMinuto`).
- A sessão é revalidada a cada requisição: conta suspensa ou senha
  redefinida derruba as sessões abertas na hora.
- Trocar o e-mail exige a senha atual.
- Só administradores criam categorias.
- Estoque limitado a 100.000 unidades por anúncio.
- Respostas com `X-Content-Type-Options: nosniff`; Swagger só em
  desenvolvimento (`dotnet run` usa o perfil de `Properties/launchSettings.json`).

# ⚠️ Limitações conhecidas

- Pagamento e envio ficam fora do escopo: o pedido termina na conclusão pelo
  vendedor.
- O bloqueio de força bruta e o limite por IP ficam em memória: reiniciar o
  servidor zera os contadores, e com vários servidores cada um conta à parte.
- Não há envio real de e-mail; troque `CanalRecuperacaoSenhaLog` por uma
  implementação de verdade antes de publicar.
- Não há teste contra PostgreSQL ou SQL Server.
