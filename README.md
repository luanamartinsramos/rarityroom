
# 🛍️ RarityRoom

O **RarityRoom** é uma plataforma de compra e venda de itens colecionáveis do universo geek. O projeto foi desenvolvido com o objetivo de criar uma experiência moderna para usuários que desejam cadastrar, visualizar e gerenciar produtos colecionáveis.

O projeto possui um **frontend desenvolvido em React + TypeScript** e um **backend desenvolvido em ASP.NET Core**, organizados em um único repositório (monorepo).


# Alunos:

Luana Martins Ramos - 06014936
Jeorgia Luiza da Rosa Canto - 06014796
Pedro Henrique Canto da Silva - 06013162
Lucas Ribeiro Dias - 06015364
Miguel Souza dos Santos - 06014237


---

## ✨ Sobre o projeto

O RarityRoom foi desenvolvido como um projeto acadêmico e prático para aplicar conceitos de desenvolvimento web moderno, integração entre frontend e backend, autenticação de usuários, gerenciamento de produtos e organização de uma aplicação em arquitetura separada por responsabilidades.

A plataforma permite que usuários tenham uma área própria para acessar sua conta e gerenciar produtos disponíveis para venda.

A proposta visual busca trazer uma identidade inspirada no universo geek, com uma interface moderna, escura e focada na apresentação dos produtos.

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

## 🏗️ Estrutura do projeto

O projeto utiliza uma estrutura de **monorepo**, mantendo frontend e backend no mesmo repositório:


rarityroom/
│
├── frontend/
│   ├── src/
│   ├── public/
│   ├── package.json
│   └── ...
│
├── backend/
│   ├── Controllers/
│   ├── Modelos/
│   ├── Repositorios/
│   ├── Servicos/
│   ├── Program.cs
│   └── RarityRoom.csproj
│
└── README.md
