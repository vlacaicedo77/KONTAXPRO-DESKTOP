namespace KONTAXPRO.Application.Models.Productos;

public enum ProductoCatalogoKpi
{
    Todos,
    StockBajo,
    SinStock,
    PorCaducar
}

public enum ProductoCatalogoEstado
{
    Activos,
    Inactivos,
    Todos
}

public enum ProductoCatalogoOrden
{
    Codigo,
    Producto,
    Categoria,
    Unidad,
    Stock,
    CostoPromedio,
    PrecioBase,
    Estado
}

public sealed class ProductoCatalogoQuery
{
    public long EmpresaId { get; init; }
    public string? Busqueda { get; init; }
    public ProductoCatalogoKpi Kpi { get; init; } = ProductoCatalogoKpi.Todos;
    public ProductoCatalogoEstado Estado { get; init; } = ProductoCatalogoEstado.Activos;
    public ProductoCatalogoOrden Orden { get; init; } = ProductoCatalogoOrden.Producto;
    public bool OrdenDescendente { get; init; }
    public int Pagina { get; init; } = 1;
    public int TamanoPagina { get; init; } = 25;
}

public sealed class ProductoCatalogoResultadoDto
{
    public IReadOnlyList<ProductoListadoDto> Items { get; init; } = [];
    public int TotalItems { get; init; }
    public int Pagina { get; init; } = 1;
    public int TamanoPagina { get; init; } = 25;
    public int TotalPaginas { get; init; }
    public ProductoCatalogoKpisDto Kpis { get; init; } = new();
}

public sealed class ProductoCatalogoKpisDto
{
    public int Productos { get; init; }
    public int StockBajo { get; init; }
    public int SinStock { get; init; }
    public int PorCaducar { get; init; }
}
