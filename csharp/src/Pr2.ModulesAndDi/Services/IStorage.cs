namespace Pr2.ModulesAndDi.Services;

/// <summary>Хранилище строковых значений.</summary>
public interface IStorage
{
    void Add(string value);
    IReadOnlyList<string> GetAll();
}

/// <summary>
/// Шина событий для межмодульного взаимодействия.
/// Позволяет модулям публиковать и подписываться на события
/// без прямой зависимости друг от друга.
/// </summary>
public interface IEventBus
{
    void Publish(string eventName, string payload);
    void Subscribe(string eventName, Action<string> handler);
}
