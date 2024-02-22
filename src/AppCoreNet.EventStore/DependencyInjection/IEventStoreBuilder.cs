using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace AppCoreNet.Extensions.DependencyInjection;

public interface IEventStoreBuilder
{
    IServiceCollection Services { get; }
}