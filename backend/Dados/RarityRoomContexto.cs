using Microsoft.EntityFrameworkCore;
using RarityRoom.Modelos;

namespace RarityRoom.Dados;

/// <summary>
/// Mapeamento relacional do domínio. As entidades continuam sendo as donas das
/// regras; o contexto apenas traduz estado de objeto em linhas.
/// </summary>
public class RarityRoomContexto : DbContext
{
    public RarityRoomContexto(DbContextOptions<RarityRoomContexto> opcoes)
        : base(opcoes)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Produto> Produtos => Set<Produto>();

    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<Proposta> Propostas => Set<Proposta>();

    public DbSet<Favorito> Favoritos => Set<Favorito>();

    public DbSet<Pedido> Pedidos => Set<Pedido>();

    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();

    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    public DbSet<Loja> Lojas => Set<Loja>();

    public DbSet<Endereco> Enderecos => Set<Endereco>();

    public DbSet<RecuperacaoSenha> RecuperacoesSenha => Set<RecuperacaoSenha>();

    protected override void OnModelCreating(ModelBuilder construtor)
    {
        ConfigurarUsuario(construtor);
        ConfigurarCategoria(construtor);
        ConfigurarProduto(construtor);
        ConfigurarProposta(construtor);
        ConfigurarFavorito(construtor);
        ConfigurarPedido(construtor);
        ConfigurarAvaliacao(construtor);
        ConfigurarNotificacao(construtor);
        ConfigurarLoja(construtor);
        ConfigurarEndereco(construtor);
        ConfigurarRecuperacaoSenha(construtor);
    }

    private static void ConfigurarUsuario(ModelBuilder construtor)
    {
        construtor.Entity<Usuario>(entidade =>
        {
            entidade.ToTable("usuarios");
            entidade.HasKey(usuario => usuario.Id);

            entidade.Property(usuario => usuario.Nome)
                .IsRequired()
                .HasMaxLength(120);

            entidade.Property(usuario => usuario.Email)
                .IsRequired()
                .HasMaxLength(200);

            // Unicidade do e-mail é regra de negócio (RN13); o índice garante
            // no banco o que o serviço já valida.
            entidade.HasIndex(usuario => usuario.Email).IsUnique();

            entidade.Property(usuario => usuario.SenhaHash)
                .IsRequired()
                .HasMaxLength(300);

            entidade.Property(usuario => usuario.Ativo).IsRequired();
            entidade.Property(usuario => usuario.CriadoEm).IsRequired();

            entidade.Property(usuario => usuario.TipoConta)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            entidade.Property(usuario => usuario.FotoUrl).HasMaxLength(500);

            entidade.Property(usuario => usuario.Administrador)
                .IsRequired()
                .HasDefaultValue(false);
        });
    }

    private static void ConfigurarCategoria(ModelBuilder construtor)
    {
        construtor.Entity<Categoria>(entidade =>
        {
            entidade.ToTable("categorias");
            entidade.HasKey(categoria => categoria.Id);

            entidade.Property(categoria => categoria.Nome)
                .IsRequired()
                .HasMaxLength(60);

            entidade.Property(categoria => categoria.Slug)
                .IsRequired()
                .HasMaxLength(60);

            entidade.HasIndex(categoria => categoria.Slug).IsUnique();
        });
    }

    private static void ConfigurarProduto(ModelBuilder construtor)
    {
        construtor.Entity<Produto>(entidade =>
        {
            entidade.ToTable("produtos");
            entidade.HasKey(produto => produto.Id);

            entidade.Property(produto => produto.UsuarioId).IsRequired();

            entidade.Property(produto => produto.Nome)
                .IsRequired()
                .HasMaxLength(120);

            entidade.Property(produto => produto.Descricao).HasMaxLength(2000);

            entidade.Property(produto => produto.Preco)
                .IsRequired()
                .HasColumnType("TEXT");

            entidade.Property(produto => produto.Estoque).IsRequired();

            entidade.Property(produto => produto.Categoria)
                .IsRequired()
                .HasMaxLength(60);

            entidade.Property(produto => produto.Marca).HasMaxLength(80);
            entidade.Property(produto => produto.Colecao).HasMaxLength(80);
            entidade.Property(produto => produto.Condicao).HasMaxLength(80);
            entidade.Property(produto => produto.ImagemUrl).HasMaxLength(500);

            entidade.Property(produto => produto.CriadoEm).IsRequired();

            // Enums como texto: o arquivo .db fica legível e o valor numérico
            // não vira contrato.
            entidade.Property(produto => produto.Estado)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            entidade.Property(produto => produto.Modalidade)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            entidade.HasIndex(produto => produto.UsuarioId);
            entidade.HasIndex(produto => produto.Categoria);
        });
    }

    private static void ConfigurarProposta(ModelBuilder construtor)
    {
        construtor.Entity<Proposta>(entidade =>
        {
            entidade.ToTable("propostas");
            entidade.HasKey(proposta => proposta.Id);

            entidade.Property(proposta => proposta.ProdutoId).IsRequired();
            entidade.Property(proposta => proposta.ProponenteId).IsRequired();

            entidade.Property(proposta => proposta.Valor)
                .IsRequired()
                .HasColumnType("TEXT");

            entidade.Property(proposta => proposta.Mensagem).HasMaxLength(500);

            entidade.Property(proposta => proposta.Estado)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            entidade.Property(proposta => proposta.CriadaEm).IsRequired();

            entidade.HasIndex(proposta => proposta.ProdutoId);
        });
    }

    private static void ConfigurarFavorito(ModelBuilder construtor)
    {
        construtor.Entity<Favorito>(entidade =>
        {
            entidade.ToTable("favoritos");
            entidade.HasKey(favorito => favorito.Id);

            entidade.Property(favorito => favorito.UsuarioId).IsRequired();
            entidade.Property(favorito => favorito.ProdutoId).IsRequired();
            entidade.Property(favorito => favorito.CriadoEm).IsRequired();

            // Sem duplicata por par (usuário, anúncio) — RN06 no banco.
            entidade
                .HasIndex(favorito => new { favorito.UsuarioId, favorito.ProdutoId })
                .IsUnique();
        });
    }

    private static void ConfigurarPedido(ModelBuilder construtor)
    {
        construtor.Entity<Pedido>(entidade =>
        {
            entidade.ToTable("pedidos");
            entidade.HasKey(pedido => pedido.Id);

            entidade.Property(pedido => pedido.ProdutoId).IsRequired();
            entidade.Property(pedido => pedido.CompradorId).IsRequired();
            entidade.Property(pedido => pedido.VendedorId).IsRequired();

            // Snapshot: nome e preço congelados no momento da compra.
            entidade.Property(pedido => pedido.NomeProduto)
                .IsRequired()
                .HasMaxLength(120);

            entidade.Property(pedido => pedido.PrecoUnitario)
                .IsRequired()
                .HasColumnType("TEXT");

            entidade.Property(pedido => pedido.Estado)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            entidade.Property(pedido => pedido.StatusPagamento)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            entidade.Property(pedido => pedido.FormaPagamento)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // Endereço do checkout guardado no próprio pedido (snapshot).
            entidade.OwnsOne(pedido => pedido.Entrega, entrega =>
            {
                entrega.Property(item => item.Cep)
                    .HasColumnName("EntregaCep")
                    .HasMaxLength(9)
                    .IsRequired();

                entrega.Property(item => item.Logradouro)
                    .HasColumnName("EntregaLogradouro")
                    .HasMaxLength(120)
                    .IsRequired();

                entrega.Property(item => item.Numero)
                    .HasColumnName("EntregaNumero")
                    .HasMaxLength(10)
                    .IsRequired();

                entrega.Property(item => item.Complemento)
                    .HasColumnName("EntregaComplemento")
                    .HasMaxLength(80);

                entrega.Property(item => item.Bairro)
                    .HasColumnName("EntregaBairro")
                    .HasMaxLength(80)
                    .IsRequired();

                entrega.Property(item => item.Cidade)
                    .HasColumnName("EntregaCidade")
                    .HasMaxLength(80)
                    .IsRequired();

                entrega.Property(item => item.Estado)
                    .HasColumnName("EntregaEstado")
                    .HasMaxLength(2)
                    .IsRequired();
            });

            entidade.Property(pedido => pedido.CriadoEm).IsRequired();

            entidade.HasIndex(pedido => pedido.ProdutoId);
            entidade.HasIndex(pedido => pedido.CompradorId);
            entidade.HasIndex(pedido => pedido.VendedorId);
        });
    }

    private static void ConfigurarAvaliacao(ModelBuilder construtor)
    {
        construtor.Entity<Avaliacao>(entidade =>
        {
            entidade.ToTable("avaliacoes");
            entidade.HasKey(avaliacao => avaliacao.Id);

            entidade.Property(avaliacao => avaliacao.ProdutoId).IsRequired();

            entidade.Property(avaliacao => avaliacao.AutorId).IsRequired();
            entidade.Property(avaliacao => avaliacao.VendedorId).IsRequired();
            entidade.Property(avaliacao => avaliacao.Nota).IsRequired();
            entidade.Property(avaliacao => avaliacao.Comentario).HasMaxLength(500);
            entidade.Property(avaliacao => avaliacao.CriadaEm).IsRequired();

            // Uma compra, uma avaliação — garantido também no banco.
            entidade.HasIndex(avaliacao => avaliacao.PedidoId).IsUnique();
            entidade.HasIndex(avaliacao => avaliacao.VendedorId);
            entidade.HasIndex(avaliacao => avaliacao.ProdutoId);
        });
    }

    private static void ConfigurarNotificacao(ModelBuilder construtor)
    {
        construtor.Entity<Notificacao>(entidade =>
        {
            entidade.ToTable("notificacoes");
            entidade.HasKey(notificacao => notificacao.Id);

            entidade.Property(notificacao => notificacao.UsuarioId).IsRequired();

            entidade.Property(notificacao => notificacao.Tipo)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(30);

            entidade.Property(notificacao => notificacao.Mensagem)
                .IsRequired()
                .HasMaxLength(300);

            entidade.Property(notificacao => notificacao.Lida).IsRequired();
            entidade.Property(notificacao => notificacao.CriadaEm).IsRequired();

            entidade.HasIndex(notificacao => notificacao.UsuarioId);
        });
    }

    private static void ConfigurarLoja(ModelBuilder construtor)
    {
        construtor.Entity<Loja>(entidade =>
        {
            entidade.ToTable("lojas");
            entidade.HasKey(loja => loja.Id);

            entidade.Property(loja => loja.DonoId).IsRequired();

            entidade.Property(loja => loja.Nome)
                .IsRequired()
                .HasMaxLength(80);

            entidade.Property(loja => loja.Descricao).HasMaxLength(500);
            entidade.Property(loja => loja.FotoUrl).HasMaxLength(500);
            entidade.Property(loja => loja.CriadaEm).IsRequired();

            // Uma loja por vendedor.
            entidade.HasIndex(loja => loja.DonoId).IsUnique();
        });
    }

    private static void ConfigurarEndereco(ModelBuilder construtor)
    {
        construtor.Entity<Endereco>(entidade =>
        {
            entidade.ToTable("enderecos");
            entidade.HasKey(endereco => endereco.Id);

            entidade.Property(endereco => endereco.UsuarioId).IsRequired();
            entidade.Property(endereco => endereco.Cep).IsRequired().HasMaxLength(9);
            entidade.Property(endereco => endereco.Logradouro).IsRequired().HasMaxLength(120);
            entidade.Property(endereco => endereco.Numero).IsRequired().HasMaxLength(20);
            entidade.Property(endereco => endereco.Complemento).HasMaxLength(80);
            entidade.Property(endereco => endereco.Bairro).IsRequired().HasMaxLength(80);
            entidade.Property(endereco => endereco.Cidade).IsRequired().HasMaxLength(80);
            entidade.Property(endereco => endereco.Estado).IsRequired().HasMaxLength(2);
            entidade.Property(endereco => endereco.Principal).IsRequired();
            entidade.Property(endereco => endereco.CriadoEm).IsRequired();

            entidade.HasIndex(endereco => endereco.UsuarioId);
        });
    }

    private static void ConfigurarRecuperacaoSenha(ModelBuilder construtor)
    {
        construtor.Entity<RecuperacaoSenha>(entidade =>
        {
            entidade.ToTable("recuperacoes_senha");
            entidade.HasKey(recuperacao => recuperacao.Id);

            entidade.Property(recuperacao => recuperacao.UsuarioId).IsRequired();
            entidade.Property(recuperacao => recuperacao.Token)
                .IsRequired()
                .HasMaxLength(64);

            entidade.Property(recuperacao => recuperacao.ExpiraEm).IsRequired();
            entidade.Property(recuperacao => recuperacao.Usada).IsRequired();

            entidade.HasIndex(recuperacao => recuperacao.Token).IsUnique();
        });
    }
}
