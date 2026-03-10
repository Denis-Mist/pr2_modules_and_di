using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pr2.ModulesAndDi.Core;

// ─────────────────────────────────────────────────────────────────────────────
// Точка входа. Ядро приложения.
// Ядро не знает о деталях модулей — только работает с контрактом IAppModule.
// ─────────────────────────────────────────────────────────────────────────────

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    // 1. Читаем конфигурацию — список включённых модулей
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .Build();

    var enabled = configuration.GetSection("Modules").Get<string[]>()
        ?? throw new InvalidOperationException("Секция 'Modules' отсутствует в appsettings.json");

    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine("  Система расширяемых модулей (Практическое занятие 2)");
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine($"Включённые модули из конфигурации: {string.Join(", ", enabled)}");
    Console.WriteLine();

    // 2. Обнаружение модулей через отражение — ядро не знает об их именах заранее
    var discovered = ModuleCatalog.DiscoverFromAssembly(Assembly.GetExecutingAssembly());
    Console.WriteLine($"Обнаружено модулей в сборке: {discovered.Count} ({string.Join(", ", discovered.Keys)})");

    // 3. Построение порядка запуска с учётом зависимостей
    //    Если модуль отсутствует или есть цикл — будет понятное сообщение об ошибке
    var ordered = ModuleCatalog.BuildExecutionOrder(discovered, enabled);
    Console.WriteLine($"Порядок запуска: {string.Join(" → ", ordered.Select(m => m.Name))}");
    Console.WriteLine();

    // 4. Регистрация служб: каждый модуль регистрирует свои службы в контейнере DI
    var services = new ServiceCollection();
    foreach (var module in ordered)
    {
        Console.WriteLine($"[DI] Регистрация служб модуля '{module.Name}'...");
        module.RegisterServices(services);
    }
    Console.WriteLine();

    // 5. Построение контейнера DI — после этого добавлять регистрации нельзя
    var provider = services.BuildServiceProvider();

    // 6. Инициализация: каждый модуль получает уже готовый контейнер
    Console.WriteLine("Инициализация модулей:");
    foreach (var module in ordered)
    {
        Console.WriteLine($"  Инициализация '{module.Name}'...");
        await module.InitializeAsync(provider, cts.Token);
    }
    Console.WriteLine();

    // 7. Выполнение действий, зарегистрированных модулями
    var actions = provider.GetServices<IAppAction>().ToArray();
    Console.WriteLine($"Запуск {actions.Length} действий модулей:");
    Console.WriteLine("───────────────────────────────────────────────────────");

    foreach (var action in actions)
    {
        Console.WriteLine($"► {action.Title}");
        await action.ExecuteAsync(cts.Token);
        Console.WriteLine();
    }

    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine("  Все модули успешно завершили работу.");
    Console.WriteLine("═══════════════════════════════════════════════════════");
}
catch (ModuleLoadException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine($"\n[ОШИБКА МОДУЛЯ] {ex.Message}");
    Console.ResetColor();
    return 1;
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nРабота прервана пользователем.");
    return 0;
}

return 0;
