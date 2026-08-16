using Xunit;

// Las pruebas de vistas WPF comparten System.Windows.Application, que es
// única por AppDomain. Ejecutarlas en paralelo produce fallos no deterministas.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
