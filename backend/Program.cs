using Microsoft.AspNetCore.Authentication.Cookies;
using RarityRoom.Repositorios;
using RarityRoom.Servicos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
  options.AddPolicy("Frontend", policy =>
  {
    policy
          .WithOrigins("http://localhost:5173")
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
  });
});

builder.Services.AddSingleton<IRepositorioUsuario,
    RepositorioUsuarioMemoria>();

builder.Services.AddSingleton<IRepositorioProduto,
    RepositorioProdutoMemoria>();

builder.Services.AddSingleton<IHashSenha,
    HashSenha>();

builder.Services.AddSingleton<IServicoAutenticacao,
    ServicoAutenticacao>();

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
      options.Cookie.Name = "RarityRoom.Auth";
      options.Cookie.HttpOnly = true;
      options.Cookie.SameSite = SameSiteMode.Lax;
      options.LoginPath = "/api/autenticacao/entrar";
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();