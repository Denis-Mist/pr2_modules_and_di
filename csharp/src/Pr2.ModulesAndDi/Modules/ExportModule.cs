using Microsoft.Extensions.DependencyInjection;
using Pr2.ModulesAndDi.Core;
using Pr2.ModulesAndDi.Services;

namespace Pr2.ModulesAndDi.Modules;

/// <summary>
/// Модуль экспорта данных в файл.
/// Зависит от Core и Validation (данные должны быть проверены до экспорта).
/// После записи публикует событие на шину — другие модули могут реагировать.
/// </summary>
public sealed class ExportModule : IAppModule
{
    public string Name => "Export";
    public int ContractVersion => 1;
    public IReadOnlyCollection<string> Requires => new[] { "Core", "Validation" };

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IAppAction, ExportAction>();
    }

    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        => Task.CompletedTask;

    private sealed class ExportAction : IAppAction
    {
        private readonly IStorage _storage;
        private readonly IClock _clock;
        private readonly IEventBus _bus;

        public ExportAction(IStorage storage, IClock clock, IEventBus bus)
        {
            _storage = storage;
            _clock = clock;
            _bus = bus;
        }

        public string Title => "Экспорт данных в файл";

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var lines = _storage.GetAll();
            var path = Path.Combine(AppContext.BaseDirectory, "export.txt");

            var header = $"# Экспорт от {_clock.Now:yyyy-MM-dd HH:mm:ss zzz}";
            var content = new[] { header }.Concat(lines);

            await File.WriteAllLinesAsync(path, content, cancellationToken);

            Console.WriteLine($"  [Export] Записано {lines.Count} строк в файл {path}");
            _bus.Publish("export.done", path);
        }
    }
}
