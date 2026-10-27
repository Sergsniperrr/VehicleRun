# CODE_STYLE.md

## Назначение

Этот файл описывает стиль кода C# и Unity-кода проекта: naming, layout и локальные coding conventions. Архитектура, слои, DI-scope-структура и правила технологий — в `CLAUDE.md` и `README.md`; здесь только то, как код выглядит и читается.

## Общий принцип

Код должен читаться по именам и структуре. Если по названию класса, метода или поля непонятно, зачем оно существует, имя нужно улучшить до реализации.

Оверинжиниринг считается проблемой. Паттерны применяются только тогда, когда они реально упрощают поддержку, тестирование или расширение.

## Кейсы именования

- `PascalCase`: классы, структуры, интерфейсы, enum, свойства, методы, события, public/protected члены, а также `static`, `readonly`, `const` поля.
- `camelCase`: параметры и локальные переменные.
- `_camelCase`: private и internal поля.

## Namespace

Namespace использует `PascalCase` и точку как разделитель и повторяет путь папки от `Runtime/`. Корень проекта — `_Project` (исключение из `PascalCase`, задано `rootNamespace` в asmdef):

```csharp
namespace _Project.UI.Localization
```

## Классы, структуры, интерфейсы, enum

- Интерфейсы имеют префикс `I`.
- Всегда указывать модификатор доступа.
- Один файл — один основной класс; имя файла совпадает с именем класса.
- Суффикс `Flow` в именах классов не используется; входная точка инициализации называется `Bootstrapper`.
- Ролевые суффиксы (`Handler`, `Presenter`, `Service`, `Loader`) допустимы только там, где класс реально играет эту роль.

Пример:

```csharp
public class BoardPresenter
{
}

public interface IScoringService
{
}

public enum LevelState
{
    Waiting,
    Playing,
    UsingBooster,
    Finished
}
```

## Поля

- Не использовать `protected` поля без сильной причины.
- Для Inspector-ссылок предпочитать private serialized поля, а не public.
- Public поля допустимы только в чистых DTO/struct как осознанная data-only модель.

Пример:

```csharp
[SerializeField] private TextMeshProUGUI _timerLabel;

private IScoringService _scoringService;
private const float MinDragThreshold = 1.5f;
```

Не делать так:

```csharp
public TextMeshProUGUI TimerLabel;
```

## Свойства

- Предпочитать auto-properties, если логика не нужна.
- Для read-only доступа к private полю использовать expression-bodied property.

```csharp
public int MatchedCount { get; private set; }
public bool IsPlaying => _state == LevelState.Playing;
```

## Методы

- Метод начинается с глагола или явного действия: `Create`, `Start`, `Stop`, `Calculate`, `Apply`, `Reset`, `Show`, `Hide`.
- Имя метода отражает одно действие; если хочется `And` в имени — метод стоит разделить.
- Статические методы не использовать — логика живёт в экземплярных методах.

Плохо:

```csharp
public void ChangeValue(int value)
{
}
```

Лучше:

```csharp
public void PlaceItem(int slotIndex)
{
}
```

## Параметры и локальные переменные

- Не использовать бессмысленные имена вроде `a`, `b`, `temp`, `data`, если роль не очевидна.
- `var` допустим, когда тип очевиден справа, но предпочтителен явный тип.

## События

- C# events предпочтительнее UnityEvent для доменной и application-логики.
- Окончание `-ing` — процесс, `-ed` — завершённое действие.
- Обработчики событий имеют префикс `On`.

```csharp
public event Action MatchStarted;
public event Action MatchFinished;

private void OnMatchFinished()
{
}
```

Подписка и отписка должны быть симметричны:

- `OnEnable` / `OnDisable`;
- `Awake` или `Start` / `OnDestroy`;
- `IStartable.Start` / `IDisposable.Dispose` для VContainer entry points.

## Порядок членов класса

1. static / readonly / const поля
2. serialized поля и поля с атрибутами
3. обычные поля
4. делегаты
5. конструкторы
6. события
7. свойства
8. Unity lifecycle methods
9. public/internal/protected методы
10. private методы
11. обработчики событий

Внутри группы сортировать по доступу: `public` → `internal` → `protected internal` → `protected` → `private`.

## Скобки, отступы, пробелы

- Открывающая и закрывающая фигурные скобки всегда на отдельной строке.
- Тело `if`, `else`, `for`, `foreach`, `while` всегда в фигурных скобках, не в одну строку.
- Условные операторы и циклы отделяются пустой строкой от соседних логических блоков.
- Отрицание в условиях писать явно: `if (flag == false)`, а не `if (!flag)`.

```csharp
if (_isDragging)
{
    DropItem();
}
else
{
    PickUpItem();
}
```

## Циклы

- По циклу должно быть понятно, когда он завершится.
- `while (true)` запрещён без явной, близкой и читаемой точки выхода.

```csharp
while (enabled)
{
    yield return _waitForFrame;
}
```

## Магические числа

Числа с неочевидным смыслом выносить в `const`, config или ScriptableObject. Исключение: `0` и `1` допустимы в коде без выноса в константу.

Плохо:

```csharp
_timer -= 60;
```

Лучше:

```csharp
private const int BoosterTimeBonusSeconds = 60;

_timer += BoosterTimeBonusSeconds;
```

## Дублирование

- Не копировать одинаковую логику между классами: если изменение нужно в двух местах, логика, вероятно, должна жить в одном сервисе, helper или value object.
- Не создавать абстракцию ради одного использования.

## Unity-specific стиль

- MonoBehaviour отвечает за view, binding, Unity lifecycle и scene references; бизнес-логика живёт в plain C# классах.
- Компоненты, гарантированные `[RequireComponent]`, кэшировать в `Awake` через `GetComponent`; ленивые геттеры компонентов не писать.
- Не использовать `FindObjectOfType`, `GameObject.Find`, `Camera.main` в горячем пути.
- Не хранить важное runtime-состояние в public Inspector-полях.
- Не смешивать UI-логику и rules/scoring.

## VContainer стиль

Правила lifetimes и scope-структуры — в `README.md` (раздел 3) и `CLAUDE.md`. На уровне кода:

- plain C# зависимости получать через constructor injection;
- MonoBehaviour references регистрировать в owning `LifetimeScope`;
- entry points регистрировать через `RegisterEntryPoint` или `UseEntryPoints`;
- интерфейс указывать явно: `Register<IService, Service>(...)` или `Register<Service>().As<IService>()`; `AsImplementedInterfaces()` не использовать.

```csharp
public class RootLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ISavesService, SavesService>(Lifetime.Singleton);
        builder.Register<IRewardedAdService, RewardedAdService>(Lifetime.Singleton);
        builder.RegisterEntryPoint<Bootstrapper>();
    }
}
```

## UniTask стиль

- Async Unity-код писать через `UniTask`; один основной async-подход на весь флоу.
- Всегда прокидывать `CancellationToken` в операции, которые могут пережить объект или scope.
- Не использовать `async void`, кроме Unity/event boundary, где другого варианта нет.
- Для fire-and-forget использовать явный `.Forget()` и обрабатывать ошибки.

```csharp
public async UniTask LoadLevelAsync(int levelIndex, CancellationToken cancellationToken)
{
    await _sceneLoader.LoadAsync(SceneNames.Level, cancellationToken);
}
```

## Платформенный SDK стиль (JTL SDK)

- Прямые вызовы платформенного SDK держать в infrastructure adapter (`Infrastructure/JTL`); UI и gameplay работают через интерфейсы `Core` (`IAdvertisingService`, `IPurchaseService`, `IAnalyticsService`, `ILocalizationService`, интерфейсы хранилищ).
- Прямые `JTLSDK.*` в UI/gameplay — технический долг: при доработке такого файла уносим вызов за контракт `Core`, а не копируем паттерн.
- Не размазывать платформенные `using` (`JTLStudio.SDK`) по проекту.
- Реклама и платежи вызываются из понятного flow, а не из случайных кнопок; паузу и mute на время рекламы и оплаты ставит сам SDK, игра не пишет в `Time.timeScale` и `AudioListener`.
- Детали API JTL SDK — в [документации](https://meepifiev.github.io/com.jtlstudio.sdk/) и [llms.txt](https://meepifiev.github.io/com.jtlstudio.sdk/llms.txt); не угадывать сигнатуры, сверяться с документацией.

## Odin (Sirenix) стиль

- Odin-атрибуты — только на конфигах/редакторских инструментах, не в чистом `Core`.
- Не подменять архитектуру инспектор-магией: сериализуемая модель остаётся читаемой без Odin.

## TextMeshPro стиль

- UI-текст через `TextMeshProUGUI`.
- Не делать расчёты прямо внутри строки вывода.

Плохо:

```csharp
_label.text = (score + bonus * multiplier).ToString();
```

Лучше:

```csharp
int finalScore = CalculateFinalScore(score, bonus, multiplier);
_label.text = finalScore.ToString();
```

## ScriptableObject стиль

- Только authoring-time конфиги, без mutable runtime state уровня.
- Названия SO отражают тип и назначение: `LevelConfig`, `ItemPaletteConfig`, `BoosterConfig`.

## SOLID

- SRP: один класс — одна ответственность.
- OCP: расширение не ломает существующее поведение без причины.
- LSP: наследники не меняют контракт базового класса неожиданно.
- ISP: интерфейсы маленькие и предметные.
- DIP: зависимости передаются через абстракции и DI, а не создаются внутри класса.

SOLID не должен превращаться в фабрики фабрик. Если простой класс решает задачу ясно, он лучше чрезмерной схемы.

## Комментарии

- Комментарии в коде не пишутся; намерение выражается именами классов, методов и переменных.
- Если код непонятен без комментария — улучшать имена или структуру, а не добавлять комментарий.

## Ошибки и логирование

- Невалидные состояния и аргументы — через `throw new` (`ArgumentNullException`, `InvalidOperationException` и т.п.).
- В конструктор исключения передавать только `nameof(...)`, без текстовых сообщений.
- `Debug.LogWarning` / `Debug.LogError` не использовать для обработки ошибок — ошибка либо валидна и обрабатывается кодом, либо это исключение.

```csharp
_audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));

if (_audioService == null)
{
    throw new InvalidOperationException(nameof(_audioService));
}
```

## Запрещённые запахи

- `sealed` модификатор.
- `Manager` без конкретной ответственности.
- `Controller` как свалка UI, gameplay и platform logic.
- God object.
- `public` поля вместо encapsulation.
- Магические числа.
- Hidden singleton dependencies.
- Смешивание scoring, UI и platform-вызовов в одном классе.
- Copy-paste логики.
- `async void` без причины.
- `while (true)` без понятного выхода.
