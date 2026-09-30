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
