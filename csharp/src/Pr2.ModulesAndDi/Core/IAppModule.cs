using Microsoft.Extensions.DependencyInjection;

namespace Pr2.ModulesAndDi.Core;

/// <summary>
/// Контракт модуля расширения.
/// Каждый модуль имеет имя, список требуемых модулей и два шага жизненного цикла:
/// регистрация служб и инициализация.
/// Ядро приложения работает только с этим контрактом и ничего не знает о деталях модулей.
/// </summary>
public interface IAppModule
{
    /// <summary>Уникальное имя модуля.</summary>
    string Name { get; }

    /// <summary>Версия контракта модуля для проверки совместимости.</summary>
    int ContractVersion { get; }

    /// <summary>Имена модулей, которые должны быть загружены раньше этого модуля.</summary>
    IReadOnlyCollection<string> Requires { get; }

    /// <summary>
    /// Шаг 1: регистрация служб в контейнере внедрения зависимостей.
    /// Вызывается до построения контейнера.
    /// </summary>
    void RegisterServices(IServiceCollection services);

    /// <summary>
    /// Шаг 2: инициализация модуля после построения контейнера.
    /// Здесь можно получать зависимости из контейнера и выполнять стартовые действия.
    /// </summary>
    Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
