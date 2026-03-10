using Microsoft.Extensions.DependencyInjection;
using Pr2.ModulesAndDi.Core;
using Pr2.ModulesAndDi.Services;

namespace Pr2.ModulesAndDi.Modules;

/// <summary>
/// Модуль уведомлений.
/// Зависит от Core. При инициализации подписывается на события шины
/// и выводит уведомления при поступлении данных или завершении экспорта.
/// Показывает: модули могут взаимодействовать через шину событий без прямых ссылок друг на друга.
/// </summary>
public sealed class NotificationModule : IAppModule
{
    public string Name => "Notification";
    public int ContractVersion => 1;
    public IReadOnlyCollection<string> Requires => new[] { "Core" };

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IAppAction, NotificationAction>();
    }

    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var bus = serviceProvider.GetRequiredService<IEventBus>();

        bus.Subscribe("export.done", path =>
            Console.WriteLine($"  [Notification] 📢 Уведомление: экспорт завершён, файл: {path}"));

        return Task.CompletedTask;
    }

    private sealed class NotificationAction : IAppAction
    {
        private readonly IStorage _storage;

        public NotificationAction(IStorage storage) => _storage = storage;

        public string Title => "Проверка уведомлений";

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var count = _storage.GetAll().Count;
            Console.WriteLine($"  [Notification] Всего данных для уведомления: {count} записей.");
            return Task.CompletedTask;
        }
    }
}
