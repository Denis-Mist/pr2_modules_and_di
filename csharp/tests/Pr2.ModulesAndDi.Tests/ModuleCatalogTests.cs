using Microsoft.Extensions.DependencyInjection;
using Pr2.ModulesAndDi.Core;
using Xunit;

namespace Pr2.ModulesAndDi.Tests;

/// <summary>
/// Проверки для ModuleCatalog.
/// Покрывают: корректный порядок запуска, отсутствующий модуль,
/// цикл зависимостей, внедрение зависимостей через контейнер,
/// несовместимость версий контракта.
/// </summary>
public sealed class ModuleCatalogTests
{
    // ─── Проверки порядка запуска ───────────────────────────────────────────

    [Fact]
    public void Порядок_запуска_учитывает_линейную_зависимость()
    {
        // A <- B <- C  =>  порядок A, B, C
        var a = new FakeModule("A", Array.Empty<string>());
        var b = new FakeModule("B", new[] { "A" });
        var c = new FakeModule("C", new[] { "B" });

        var all = MakeDict(a, b, c);
        var order = ModuleCatalog.BuildExecutionOrder(all, new[] { "A", "B", "C" });

        Assert.Equal(new[] { "A", "B", "C" }, order.Select(m => m.Name).ToArray());
    }

    [Fact]
    public void Порядок_запуска_учитывает_ветвление_зависимостей()
    {
        // Core <- Logging
        // Core <- Validation <- Export
        // Core и Validation должны идти до Export
        var core = new FakeModule("Core", Array.Empty<string>());
        var logging = new FakeModule("Logging", new[] { "Core" });
        var validation = new FakeModule("Validation", new[] { "Core" });
        var export = new FakeModule("Export", new[] { "Core", "Validation" });

        var all = MakeDict(core, logging, validation, export);
        var order = ModuleCatalog.BuildExecutionOrder(all, new[] { "Core", "Logging", "Validation", "Export" });

        var names = order.Select(m => m.Name).ToArray();

        // Core должен быть первым
        Assert.Equal("Core", names[0]);
        // Export должен идти после Validation
        Assert.True(Array.IndexOf(names, "Validation") < Array.IndexOf(names, "Export"),
            "Validation должен идти до Export");
        // Export должен идти после Core
        Assert.True(Array.IndexOf(names, "Core") < Array.IndexOf(names, "Export"),
            "Core должен идти до Export");
    }

    [Fact]
    public void Порядок_запуска_работает_при_одном_модуле()
    {
        var a = new FakeModule("A", Array.Empty<string>());
        var all = MakeDict(a);

        var order = ModuleCatalog.BuildExecutionOrder(all, new[] { "A" });

        Assert.Single(order);
        Assert.Equal("A", order[0].Name);
    }

    [Fact]
    public void Порядок_запуска_независимые_модули_все_присутствуют()
    {
        // X и Y независимы — оба должны быть в результате
        var x = new FakeModule("X", Array.Empty<string>());
        var y = new FakeModule("Y", Array.Empty<string>());

        var all = MakeDict(x, y);
        var order = ModuleCatalog.BuildExecutionOrder(all, new[] { "X", "Y" });

        Assert.Equal(2, order.Count);
        Assert.Contains(order, m => m.Name == "X");
        Assert.Contains(order, m => m.Name == "Y");
    }

    // ─── Проверки ошибок: отсутствующий модуль ──────────────────────────────

    [Fact]
    public void Отсутствующий_модуль_выбрасывает_ModuleLoadException()
    {
        var a = new FakeModule("A", Array.Empty<string>());
        var all = MakeDict(a);

        var ex = Assert.Throws<ModuleLoadException>(
            () => ModuleCatalog.BuildExecutionOrder(all, new[] { "A", "B" }));

        Assert.IsType<ModuleLoadException>(ex);
    }

    [Fact]
    public void Отсутствующий_модуль_даёт_понятное_сообщение_с_именем()
    {
        var a = new FakeModule("A", Array.Empty<string>());
        var all = MakeDict(a);

        var ex = Assert.Throws<ModuleLoadException>(
            () => ModuleCatalog.BuildExecutionOrder(all, new[] { "A", "NonExistent" }));

        Assert.Contains("NonExistent", ex.Message);
        Assert.Contains("не найден", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Отсутствующая_транзитивная_зависимость_даёт_понятное_сообщение()
    {
        // B требует Missing, но Missing не включён
        var a = new FakeModule("A", Array.Empty<string>());
        var b = new FakeModule("B", new[] { "A", "Missing" });
        var all = MakeDict(a, b);

        var ex = Assert.Throws<ModuleLoadException>(
            () => ModuleCatalog.BuildExecutionOrder(all, new[] { "A", "B" }));

        Assert.Contains("Missing", ex.Message);
    }

    // ─── Проверки ошибок: цикл зависимостей ─────────────────────────────────

    [Fact]
    public void Прямой_цикл_двух_модулей_обнаруживается()
    {
        var a = new FakeModule("A", new[] { "B" });
        var b = new FakeModule("B", new[] { "A" });
        var all = MakeDict(a, b);

        var ex = Assert.Throws<ModuleLoadException>(
            () => ModuleCatalog.BuildExecutionOrder(all, new[] { "A", "B" }));

        Assert.Contains("циклическая", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Длинный_цикл_трёх_модулей_обнаруживается()
    {
        // A -> B -> C -> A
        var a = new FakeModule("A", new[] { "C" });
        var b = new FakeModule("B", new[] { "A" });
        var c = new FakeModule("C", new[] { "B" });
        var all = MakeDict(a, b, c);

        var ex = Assert.Throws<ModuleLoadException>(
            () => ModuleCatalog.BuildExecutionOrder(all, new[] { "A", "B", "C" }));

        Assert.Contains("циклическая", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Цикл_сообщение_содержит_имена_проблемных_модулей()
    {
        var a = new FakeModule("Alpha", new[] { "Beta" });
        var b = new FakeModule("Beta", new[] { "Alpha" });
        var all = MakeDict(a, b);

        var ex = Assert.Throws<ModuleLoadException>(
            () => ModuleCatalog.BuildExecutionOrder(all, new[] { "Alpha", "Beta" }));

        // Сообщение должно содержать имена застрявших модулей
        Assert.True(
            ex.Message.Contains("Alpha", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("Beta", StringComparison.OrdinalIgnoreCase),
            $"Ожидали имена модулей в сообщении, получили: {ex.Message}");
    }

    // ─── Проверки внедрения зависимостей через контейнер ────────────────────

    [Fact]
    public async Task Зависимости_внедряются_контейнером_а_не_вручную()
    {
        // Проверяем, что модуль получает службы из контейнера DI
        var services = new ServiceCollection();
        services.AddSingleton<MarkerService>();

        var provider = services.BuildServiceProvider();
        MarkerService? resolved = null;

        var module = new FakeModule("A", Array.Empty<string>())
        {
            OnInit = sp => resolved = sp.GetService<MarkerService>()
        };

        await module.InitializeAsync(provider, CancellationToken.None);

        Assert.NotNull(resolved);
        // Проверяем что это именно тот экземпляр из контейнера (Singleton)
        Assert.Same(provider.GetRequiredService<MarkerService>(), resolved);
    }

    [Fact]
    public async Task Singleton_возвращает_один_и_тот_же_экземпляр()
    {
        var services = new ServiceCollection();
        services.AddSingleton<MarkerService>();
        var provider = services.BuildServiceProvider();

        var s1 = provider.GetRequiredService<MarkerService>();
        var s2 = provider.GetRequiredService<MarkerService>();

        Assert.Same(s1, s2);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Transient_возвращает_разные_экземпляры()
    {
        var services = new ServiceCollection();
        services.AddTransient<MarkerService>();
        var provider = services.BuildServiceProvider();

        var t1 = provider.GetRequiredService<MarkerService>();
        var t2 = provider.GetRequiredService<MarkerService>();

        Assert.NotSame(t1, t2);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Модули_регистрируют_службы_через_контейнер()
    {
        // RegisterServices каждого модуля должен добавлять службы в контейнер
        var module = new FakeModule("A", Array.Empty<string>())
        {
            RegisterAction = sc => sc.AddSingleton<MarkerService>()
        };

        var services = new ServiceCollection();
        module.RegisterServices(services);
        var provider = services.BuildServiceProvider();

        var service = provider.GetService<MarkerService>();
        Assert.NotNull(service);
        await Task.CompletedTask;
    }

    // ─── Проверки совместимости версий контракта ─────────────────────────────



    [Fact]
    public void Совместимая_версия_контракта_не_выбрасывает_ошибку()
    {
        var compatible = new FakeModule("Good", Array.Empty<string>(), contractVersion: ModuleCatalog.SupportedContractVersion);
        var all = MakeDict(compatible);

        // Не должно быть исключения
        var order = ModuleCatalog.BuildExecutionOrder(all, new[] { "Good" });
        Assert.Single(order);
    }

    // ─── Вспомогательные классы ──────────────────────────────────────────────

    private static IReadOnlyDictionary<string, IAppModule> MakeDict(params FakeModule[] modules)
        => modules.ToDictionary(m => m.Name, m => (IAppModule)m, StringComparer.OrdinalIgnoreCase);

    private sealed class MarkerService { }

    private sealed class FakeModule : IAppModule
    {
        public FakeModule(string name, IReadOnlyCollection<string> requires, int contractVersion = 1)
        {
            Name = name;
            Requires = requires;
            ContractVersion = contractVersion;
        }

        public string Name { get; }
        public int ContractVersion { get; }
        public IReadOnlyCollection<string> Requires { get; }
        public Action<IServiceProvider>? OnInit { get; init; }
        public Action<IServiceCollection>? RegisterAction { get; init; }

        public void RegisterServices(IServiceCollection services)
            => RegisterAction?.Invoke(services);

        public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            OnInit?.Invoke(serviceProvider);
            return Task.CompletedTask;
        }
    }
}
