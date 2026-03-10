using Microsoft.Extensions.DependencyInjection;
using Pr2.ModulesAndDi.Core;
using Pr2.ModulesAndDi.Services;

namespace Pr2.ModulesAndDi.Modules;

/// <summary>
/// Базовый модуль приложения.
/// Регистрирует фундаментальные службы: часы, хранилище и шину событий.
/// Не имеет зависимостей — должен загружаться первым.
/// Демонстрирует два времени жизни:
///   - IClock, IStorage, IEventBus — Singleton (один экземпляр на всё приложение)
///   - TransientRequestContext — Transient (новый экземпляр при каждом запросе)
/// </summary>
public sealed class CoreModule : IAppModule
{
    public string Name => "Core";
    public int ContractVersion => 1;
    public IReadOnlyCollection<string> Requires => Array.Empty<string>();

    public void RegisterServices(IServiceCollection services)
    {
        // Singleton: один экземпляр живёт всё время работы приложения
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IStorage, InMemoryStorage>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();

        // Transient: новый экземпляр создаётся при каждом обращении к контейнеру
        services.AddTransient<TransientRequestContext>();
    }

    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        // Базовый модуль не выполняет действий при инициализации,
        // он только подготавливает инфраструктуру для остальных модулей
        return Task.CompletedTask;
    }
}
