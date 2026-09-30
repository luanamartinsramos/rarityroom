# RarityRoom --- Backend

API REST desenvolvida em **ASP.NET Core** para o projeto acadêmico
RarityRoom, uma plataforma de compra e venda de itens colecionáveis e
produtos geek.

## Tecnologias

-   C#
-   ASP.NET Core
-   .NET 10
-   REST API
-   JSON
-   Swagger / OpenAPI
-   Autenticação por Cookies
-   CORS
-   Repositórios em memória

## Funcionalidades

### Autenticação

-   Cadastro de usuário
-   Login
-   Logout
-   Consulta do usuário autenticado
-   Autenticação por cookies
-   Autorização de endpoints protegidos

### Produtos

-   Listagem de produtos
-   Busca por ID
-   Cadastro
-   Atualização
-   Remoção
-   Proteção das rotas com autenticação

## Endpoints

### Autenticação

  -----------------------------------------------------------------------------------
  Método                  Endpoint                            Descrição
  ----------------------- ----------------------------------- -----------------------
  POST                    `/api/autenticacao/cadastrar`       Cadastra um usuário

  POST                    `/api/autenticacao/entrar`          Realiza login

  POST                    `/api/autenticacao/sair`            Realiza logout

  GET                     `/api/autenticacao/usuário-atual`   Retorna o usuário
                                                              autenticado
  -----------------------------------------------------------------------------------

### Produtos

  Método   Endpoint               Descrição
  -------- ---------------------- ---------------------
  GET      `/api/Produtos`        Lista os produtos
  GET      `/api/Produtos/{id}`   Busca um produto
  POST     `/api/Produtos`        Cadastra um produto
  PUT      `/api/Produtos/{id}`   Atualiza um produto
  DELETE   `/api/Produtos/{id}`   Remove um produto

## Modelo de produto


{
  "nome": "Funko Spider-Man",
  "descricao": "Item colecionável",
  "preco": 3500,
  "estoque": 2,
  "categoria": "Marvel",
  "marca": "Funko",
  "colecao": "Marvel",
  "condicao": "Novo",
  "imagemUrl": "https://exemplo.com/imagem.jpg"
}


## Como executar

### Pré-requisitos

-   .NET SDK 10
-   Git

### Clonar


git clone https://github.com/Jeorgia-Canto/Rarityroom.git
cd Rarityroom


### Restaurar dependências


dotnet restore


### Compilar


dotnet build


### Executar


dotnet run


A API utiliza a porta:


http://localhost:5000


## Swagger

Com o backend em execução:


http://localhost:5000/swagger


O Swagger permite visualizar e testar os endpoints da API.

## Integração com o frontend

O frontend React/Vite utiliza:


http://localhost:5173
```

O backend possui CORS configurado para permitir a comunicação entre as
aplicações.

Como a autenticação utiliza cookies, as requisições do frontend devem
utilizar:


fetch("http://localhost:5000/api/Produtos", {
  credentials: "include"
});


## Autenticação

As rotas de produtos utilizam:


[Authorize]


Portanto, é necessário estar autenticado para acessá-las.

O cookie de autenticação utilizado pela aplicação é:


RarityRoom.Auth


O cookie é configurado como `HttpOnly`.

## CORS

O backend permite requisições originadas de:


http://localhost:5173


Também são permitidas credenciais para o funcionamento da autenticação
por cookies.

## Estrutura


RarityRoom/
├── Controllers/
│   ├── AutenticacaoController.cs
│   └── ProdutosController.cs
├── Modelos/
├── Repositorios/
├── Servicos/
├── Program.cs
└── RarityRoom.csproj


## Armazenamento atual

Nesta versão, os dados são armazenados em memória através dos
repositórios:


RepositorioUsuarioMemoria
RepositorioProdutoMemoria


Isso permite realizar testes de autenticação e operações CRUD durante a
execução da API.

Como os dados estão em memória, eles são perdidos quando o backend é
encerrado ou reiniciado.

## Fluxo


React + TypeScript
        |
        | HTTP / JSON
        v
ASP.NET Core Web API
        |
        +-- Autenticação
        |
        +-- Autorização
        |
        +-- Produtos
                |
                v
       Repositórios em memória


## Status

-   [x] API REST
-   [x] Cadastro de usuários
-   [x] Login
-   [x] Logout
-   [x] Autenticação por cookies
-   [x] Autorização
-   [x] CRUD de produtos
-   [x] CORS
-   [x] Swagger
-   [x] Integração com frontend React
-   [ ] Persistência em banco de dados

## Projeto acadêmico

O RarityRoom é um projeto acadêmico desenvolvido para aplicar conceitos
de desenvolvimento web, arquitetura cliente-servidor, APIs REST,
autenticação, CRUD e integração entre frontend e backend.
