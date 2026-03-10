namespace Pr2.ModulesAndDi.Core;

/// <summary>
/// Ошибка загрузки или запуска модулей.
/// Выбрасывается при отсутствии нужного модуля,
/// при цикле зависимостей или несовместимости версий контракта.
/// </summary>
public sealed class ModuleLoadException : Exception
{
    public ModuleLoadException(string message)
        : base(message)
    {
    }
}
