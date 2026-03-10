namespace Pr2.ModulesAndDi.Core;

/// <summary>
/// Действие, которое регистрирует модуль.
/// Каждый модуль может зарегистрировать одно или несколько действий,
/// которые будут выполнены после загрузки всех модулей.
/// </summary>
public interface IAppAction
{
    string Title { get; }

    Task ExecuteAsync(CancellationToken cancellationToken);
}
