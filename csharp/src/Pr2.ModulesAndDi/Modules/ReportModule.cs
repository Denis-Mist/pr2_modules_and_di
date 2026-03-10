using Microsoft.Extensions.DependencyInjection;
using Pr2.ModulesAndDi.Core;
using Pr2.ModulesAndDi.Services;

namespace Pr2.ModulesAndDi.Modules;

/// <summary>
/// Модуль формирования отчёта.
/// Зависит от Core и Export — отчёт формируется после экспорта.
/// Демонстрирует: один модуль может зависеть от другого бизнес-модуля,
/// а не только от инфраструктурного Core.
/// </summary>
public sealed class ReportModule : IAppModule
{
    public string Name => "Report";
    public int ContractVersion => 1;
    public IReadOnlyCollection<string> Requires => new[] { "Core", "Export" };

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IAppAction, ReportAction>();
    }

    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        => Task.CompletedTask;

    private sealed class ReportAction : IAppAction
    {
        private readonly IClock _clock;
        private readonly IStorage _storage;

        public ReportAction(IClock clock, IStorage storage)
        {
            _clock = clock;
            _storage = storage;
        }

        public string Title => "Формирование итогового отчёта";

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var items = _storage.GetAll();
            Console.WriteLine($"  [Report] Время формирования: {_clock.Now:HH:mm:ss}");
            Console.WriteLine($"  [Report] Всего записей в хранилище: {items.Count}");
            foreach (var item in items)
                Console.WriteLine($"  [Report]   - {item}");
            return Task.CompletedTask;
        }
    }
}
