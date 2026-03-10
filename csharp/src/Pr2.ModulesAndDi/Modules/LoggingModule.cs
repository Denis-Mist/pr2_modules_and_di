using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pr2.ModulesAndDi.Core;
using Pr2.ModulesAndDi.Services;

namespace Pr2.ModulesAndDi.Modules;

/// <summary>
/// Модуль журналирования.
/// Зависит от Core. Добавляет журналирование в консоль и подписывается
/// на события шины, чтобы записывать их в журнал.
/// Демонстрирует: ядро не меняется — поведение программы меняется за счёт модуля.
/// </summary>
public sealed class LoggingModule : IAppModule
{
    public string Name => "Logging";
    public int ContractVersion => 1;
    public IReadOnlyCollection<string> Requires => new[] { "Core" };

    public void RegisterServices(IServiceCollection services)
    {
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<IAppAction, LoggingAction>();
    }

    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        // При инициализации подписываемся на все события шины
        var bus = serviceProvider.GetRequiredService<IEventBus>();
        var logger = serviceProvider.GetRequiredService<ILogger<LoggingModule>>();

        bus.Subscribe("data.added", payload =>
            logger.LogInformation("[EventBus] Данные добавлены: {Payload}", payload));
        bus.Subscribe("export.done", payload =>
            logger.LogInformation("[EventBus] Экспорт завершён: {Payload}", payload));

        logger.LogDebug("Модуль журналирования инициализирован и подписан на события шины.");
        return Task.CompletedTask;
    }

    private sealed class LoggingAction : IAppAction
    {
        private readonly ILogger<LoggingAction> _logger;
        private readonly TransientRequestContext _ctx1;
        private readonly TransientRequestContext _ctx2;

        // Transient внедряется через конструктор — контейнер создаёт новый экземпляр при каждом разрешении
        public LoggingAction(
            ILogger<LoggingAction> logger,
            TransientRequestContext ctx1,
            TransientRequestContext ctx2)
        {
            _logger = logger;
            _ctx1 = ctx1;
            _ctx2 = ctx2;
        }

        public string Title => "Проверка журнала событий и времён жизни объектов";

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Сообщение из модуля журналирования.");

            // Доказательство разных времён жизни: два Transient — два разных Id
            _logger.LogInformation(
                "Transient ctx1.Id = {Id1}, ctx2.Id = {Id2}, одинаковые? {Same}",
                _ctx1.Id, _ctx2.Id, _ctx1.Id == _ctx2.Id);

            return Task.CompletedTask;
        }
    }
}
