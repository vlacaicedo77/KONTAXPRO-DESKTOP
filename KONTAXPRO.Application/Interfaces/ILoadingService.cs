namespace KONTAXPRO.Application.Interfaces;

public interface ILoadingService
{
    Task<ILoadingScope> ShowAsync(
        string title,
        string message);
}

public interface ILoadingScope : IAsyncDisposable
{
}
