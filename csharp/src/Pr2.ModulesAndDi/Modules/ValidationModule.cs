using Microsoft.Extensions.DependencyInjection;
using Pr2.ModulesAndDi.Core;
using Pr2.ModulesAndDi.Services;

namespace Pr2.ModulesAndDi.Modules;

/// <summary>
/// Модуль правил проверки данных.
/// Зависит от Core. Добавляет данные в хранилище и публикует событие на шину.
/// Ядро не знает о логике проверки — она вся инкапсулирована в этом модуле.
/// </summary>
public sealed class ValidationModule : IAppModule
{
    public string Name => "Validation";
    public int ContractVersion => 1;
    public IReadOnlyCollection<string> Requires => new[] { "Core" };

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IAppAction, ValidationAction>();
    }

    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        => Task.CompletedTask;

    private sealed class ValidationAction : IAppAction
    {
        private readonly IStorage _storage;
        private readonly IEventBus _bus;

        // Зависимости внедряются контейнером — модуль не создаёт их вручную
        public ValidationAction(IStorage storage, IEventBus bus)
        {
            _storage = storage;
            _bus = bus;
        }

        public string Title => "Проверка правил данных";

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var candidates = new[] { "пример", "ок", "данные из файла", "x" };

            foreach (var value in candidates)
            {
                if (value.Length < 3)
                {
                    Console.WriteLine($"  [Validation] Значение '{value}' отклонено: слишком короткое (длина {value.Length}, минимум 3)");
                    continue;
                }

                _storage.Add(value);
                _bus.Publish("data.added", value);
                Console.WriteLine($"  [Validation] Значение '{value}' принято и добавлено в хранилище.");
            }

            return Task.CompletedTask;
        }
    }
}
