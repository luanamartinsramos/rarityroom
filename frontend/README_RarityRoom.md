# RarityRoom

Projeto acadêmico de uma plataforma para **compra, venda e descoberta de itens colecionáveis geek**.

## 🛠️ Tecnologias

- React
- TypeScript
- Vite
- CSS
- Git e GitHub

## 📁 Estrutura

```text
src/
├── components/
├── pages/
├── App.tsx
└── main.tsx
```

## 🚀 Como executar

Clone o projeto:

```bash
git clone URL_DO_REPOSITORIO
```

Entre na pasta:

```bash
cd RarityRoom
```

Instale as dependências:

```bash
npm install
```

Execute:

```bash
npm run dev
```

---

# 👥 Git e GitHub

Cada integrante deve trabalhar em uma **branch própria**.

```text
main
├── feature/login
├── feature/home
├── feature/cadastro
└── feature/produtos
```

## 🌱 Começar uma tarefa

```bash
git checkout main
git pull
git checkout -b feature/nome-da-tarefa
```

Agora faça seu código normalmente.

## 💾 Quando terminar

```bash
git add .
git commit -m "feat: descrição da alteração"
git push -u origin feature/nome-da-tarefa
```

## 🔀 Pull Request

No GitHub:

```text
Pull Requests
→ New Pull Request
→ main ← feature/nome-da-tarefa
→ Create Pull Request
→ Revisão
→ Merge
```

## 🔄 Depois do Merge

```bash
git checkout main
git pull
```

## 📌 Resumo

```text
Criar branch
    ↓
Fazer código
    ↓
git add .
    ↓
git commit
    ↓
git push
    ↓
Pull Request
    ↓
Revisão
    ↓
Merge
    ↓
git pull
```

**Regra principal:** não trabalhe diretamente na `main`.
