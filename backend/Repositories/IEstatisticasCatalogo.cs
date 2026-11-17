using RarityRoom.Modelos;

namespace RarityRoom.Repositorios;

/// <summary>
/// Indicadores reais do catálogo: unidades vendidas (somente pedidos com
/// pagamento aprovado e não cancelados) e média das avaliações cadastradas.
/// </summary>
public interface IEstatisticasCatalogo
{
    Dictionary<int, IndicadoresProduto> ObterIndicadores();
}
