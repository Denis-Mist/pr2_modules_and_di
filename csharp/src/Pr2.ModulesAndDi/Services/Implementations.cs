using System.Collections.Concurrent;

namespace Pr2.ModulesAndDi.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

public sealed class InMemoryStorage : IStorage
{
    private readonly ConcurrentQueue<string> _values = new();

    public void Add(string value) => _values.Enqueue(value);
    public IReadOnlyList<string> GetAll() => _values.ToArray();
}

/// <summary>
/// Реализация шины событий в памяти.
/// Синглтон: одна шина на всё приложение, зарегистрированная через контейнер DI.
/// Демонстрирует время жизни Singleton — все подписчики работают с одним экземпляром.
/// </summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<string, List<Action<string>>> _handlers = new();

    public void Publish(string eventName, string payload)
    {
        if (_handlers.TryGetValue(eventName, out var list))
        {
            foreach (var h in list)
                h(payload);
        }
    }

    public void Subscribe(string eventName, Action<string> handler)
    {
        _handlers.GetOrAdd(eventName, _ => new List<Action<string>>()).Add(handler);
    }
}

/// <summary>
/// Transient-реализация: каждый раз создаётся новый экземпляр.
/// Используется для демонстрации разницы времён жизни объектов.
/// </summary>
public sealed class TransientRequestContext
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;
}
