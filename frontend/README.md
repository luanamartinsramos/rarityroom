# RarityRoom

Projeto acadêmico de uma plataforma para **compra, venda e descoberta de
itens colecionáveis geek**.

O RarityRoom é dividido em **Frontend** e **Backend**, que se comunicam
através de uma API REST.

---

# 🎨 Frontend

O Frontend foi desenvolvido utilizando **React + TypeScript + Vite**.

Ele é responsável pela interface que o usuário utiliza para acessar a
plataforma, realizar cadastro e login, visualizar produtos e gerenciar
seus produtos.

## Tecnologias

- React
- TypeScript
- Vite
- CSS
- React Router
- React Hot Toast

## Funcionalidades

### 🔐 Autenticação

O sistema possui telas para:

- Cadastro de usuários
- Login
- Logout
- Verificação do usuário autenticado
- Proteção de páginas que precisam de autenticação

A autenticação é realizada através do Backend utilizando cookies.

### 🛍️ Produtos

O Frontend permite:

- Visualizar produtos
- Cadastrar produtos
- Editar produtos
- Excluir produtos
- Informar preço, estoque, categoria, marca e coleção
- Adicionar imagem aos produtos

As operações de produtos são realizadas através da API do Backend.

### 🏠 Home

A página principal apresenta:

- Saudação ao usuário
- Área de cadastro de produtos
- Produtos cadastrados
- Informações relacionadas aos pedidos
- Navegação pela plataforma

### 🔔 Notificações

O projeto utiliza **React Hot Toast** para apresentar mensagens ao usuário,
como confirmações de ações e avisos de erro.



# 📁 Estrutura do Frontend


frontend/
├── src/
│   ├── components/
│   ├── data/
│   ├── pages/
│   │   ├── home/
│   │   ├── login/
│   │   └── register/
│   ├── routes/
│   │   ├── AppRoutes.tsx
│   │   └── ProtectedRoute.tsx
│   ├── services/
│   │   └── produtosService.ts
│   ├── App.tsx
│   ├── index.css
│   └── main.tsx
├── public/
└── package.json
