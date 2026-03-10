using System.Reflection;

namespace Pr2.ModulesAndDi.Core;

/// <summary>
/// Ядро системы модулей.
/// Обнаруживает модули в сборке через отражение и строит порядок запуска
/// с учётом зависимостей (топологическая сортировка алгоритмом Кана).
/// Ядро ничего не знает о деталях конкретных модулей — только работает с IAppModule.
/// </summary>
public static class ModuleCatalog
{
    /// <summary>
    /// Текущая версия контракта модуля.
    /// Модули с другой версией не будут загружены.
    /// </summary>
    public const int SupportedContractVersion = 1;

    /// <summary>
    /// Обнаруживает все реализации IAppModule в указанной сборке через отражение.
    /// </summary>
    public static IReadOnlyDictionary<string, IAppModule> DiscoverFromAssembly(Assembly assembly)
    {
        var modules = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => typeof(IAppModule).IsAssignableFrom(t))
            .Select(t => (IAppModule)Activator.CreateInstance(t)!)
            .ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

        return modules;
    }

    /// <summary>
    /// Строит порядок запуска модулей с учётом зависимостей.
    /// Алгоритм Кана: топологическая сортировка ориентированного графа.
    /// Если граф содержит цикл, выбрасывает ModuleLoadException с понятным сообщением.
    /// Также проверяет совместимость версий контракта.
    /// </summary>
    public static IReadOnlyList<IAppModule> BuildExecutionOrder(
        IReadOnlyDictionary<string, IAppModule> all,
        IReadOnlyCollection<string> enabledNames)
    {
        // 1. Собрать включённые модули и проверить, что они существуют
        var enabled = new Dictionary<string, IAppModule>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in enabledNames)
        {
            if (!all.TryGetValue(name, out var module))
                throw new ModuleLoadException(
                    $"Модуль не найден, имя модуля '{name}'. " +
                    $"Доступные модули: {string.Join(", ", all.Keys)}");

            enabled[name] = module;
        }

        // 2. Проверить совместимость версий контракта
        foreach (var module in enabled.Values)
        {
            if (module.ContractVersion != SupportedContractVersion)
                throw new ModuleLoadException(
                    $"Модуль '{module.Name}' использует версию контракта {module.ContractVersion}, " +
                    $"но приложение поддерживает только версию {SupportedContractVersion}. " +
                    $"Обновите модуль или используйте совместимую версию приложения.");
        }

        // 3. Проверить, что все зависимости присутствуют среди включённых модулей
        foreach (var module in enabled.Values)
        {
            foreach (var req in module.Requires)
            {
                if (!enabled.ContainsKey(req))
                    throw new ModuleLoadException(
                        $"Не хватает модуля для зависимости: модуль '{module.Name}' требует '{req}', " +
                        $"но этот модуль не включён. Добавьте '{req}' в список модулей.");
            }
        }

        // 4. Топологическая сортировка (алгоритм Кана)
        // Строим граф: ребро req -> module означает "req должен идти перед module"
        var indegree = enabled.Values.ToDictionary(m => m.Name, _ => 0, StringComparer.OrdinalIgnoreCase);
        var edges = enabled.Values.ToDictionary(m => m.Name, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var module in enabled.Values)
        {
            foreach (var req in module.Requires)
            {
                edges[req].Add(module.Name);
                indegree[module.Name] += 1;
            }
        }

        // Начинаем с модулей без зависимостей
        var queue = new Queue<string>(
            indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var result = new List<IAppModule>();

        while (queue.Count > 0)
        {
            var name = queue.Dequeue();
            result.Add(enabled[name]);

            foreach (var to in edges[name])
            {
                indegree[to] -= 1;
                if (indegree[to] == 0)
                    queue.Enqueue(to);
            }
        }

        // Если не все модули обработаны — есть цикл
        if (result.Count != enabled.Count)
        {
            var stuck = indegree
                .Where(kv => kv.Value > 0)
                .Select(kv => kv.Key)
                .ToArray();
            var list = string.Join(", ", stuck);
            throw new ModuleLoadException(
                $"Обнаружена циклическая зависимость модулей. " +
                $"Проблемные модули: {list}. " +
                $"Цикл означает, что модули зависят друг от друга напрямую или через цепочку. " +
                $"Разорвите цикл, убрав одну из зависимостей.");
        }

        return result;
    }
}
