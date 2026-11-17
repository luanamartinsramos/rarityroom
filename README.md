# 🛍️ RarityRoom

O **RarityRoom** é uma plataforma de compra e venda de itens colecionáveis
do universo geek. O projeto foi desenvolvido com o objetivo de criar uma
experiência moderna para usuários que desejam cadastrar, visualizar e
gerenciar produtos colecionáveis.

O projeto possui um **frontend desenvolvido em React + TypeScript** e um
**backend desenvolvido em ASP.NET Core**, organizados em um único
repositório (monorepo).


# 👥 Alunos

- Luana Martins Ramos - 06014936
- Jeorgia Luiza da Rosa Canto - 06014796
- Pedro Henrique Canto da Silva - 06013162
- Lucas Ribeiro Dias - 06015364
- Miguel Souza dos Santos - 06014237

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

Resultado esperado: **135 testes aprovados, 0 falhas** (9 deles de integração HTTP).

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
| GET | `/api/produtos?termo=| GET | `/api/produtos` | pública |categoria=| GET | `/api/produtos` | pública |estado=` | pública (busca) |
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
| GET/POST | `/api/pedidos` | cookie |
| POST | `/api/pedidos/{id}/concluir` | cookie (vendedor) |
| POST | `/api/pedidos/{id}/cancelar` | cookie |
| POST | `/api/avaliacoes` | cookie (comprador) |
| GET | `/api/avaliacoes/vendedor/{id}` | pública |
| POST | `/api/produtos/{id}/imagem` | cookie (dono) |
| POST | `/api/propostas/{id}/aceitar` \| `/recusar` \| `/cancelar` | cookie |

Estados de erro seguem o padrão `ProblemDetails` com um campo `codigo`:
`dados-invalidos`, `acesso-negado`, `nao-encontrado`, `conflito`,
`transicao-invalida`.

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
