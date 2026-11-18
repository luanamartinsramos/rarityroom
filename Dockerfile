# Imagem única: o backend ASP.NET serve a API e o frontend React compilado,
# na mesma origem (sem CORS e com o cookie de login funcionando).

# 1) Frontend
FROM node:22-alpine AS frontend
WORKDIR /front
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# 2) Backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY backend/RarityRoom.csproj ./
RUN dotnet restore
COPY backend/ ./
RUN dotnet publish RarityRoom.csproj -c Release -o /app/publish --no-restore

# 3) Execução
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend /app/publish ./
COPY --from=frontend /front/dist ./wwwroot

# Banco SQLite, imagens enviadas e chaves de sessão ficam em /data. No plano
# gratuito do Render essa pasta é apagada a cada reinício; com um disco
# persistente montado em /data, os dados sobrevivem.
ENV ASPNETCORE_ENVIRONMENT=Production \
    Armazenamento__Pasta=/data
RUN mkdir -p /data && chown "$APP_UID" /data

# Usuário sem privilégios, já existente na imagem oficial.
USER $APP_UID

EXPOSE 8080

# O Render informa a porta na variável PORT.
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet RarityRoom.dll"]
