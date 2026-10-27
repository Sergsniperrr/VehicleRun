# UnityTemplate: устройство шаблона

Заготовка Unity-проекта под WebGL для площадок, подключённых через PrimeSDK. В шаблоне готовы контейнер зависимостей, конечный автомат запуска, загрузка сцен и платформенные сервисы. Игрового кода нет: он пишется в сборках `Features` и `UI`.

Unity 2022.3.62f2 (LTS), цель сборки WebGL, 3D-проект на Built-in Render Pipeline.

Здесь описан каркас: структура, композиция, запуск, сцены и старт нового проекта. Отдельные документы по системам лежат в `Docs/`:

- [Локализация](Docs/Localization.md)
- [Звук: AudioService](Docs/Audio.md)
- [Аналитика, реклама и сохранения](Docs/Analytics-Ads-Saves.md)

---

## 1. Стек

| Компонент | Версия | Назначение | Документация |
|---|---|---|---|
| VContainer | 1.19.0 | контейнер зависимостей, композиция | [vcontainer.hadashikick.jp](https://vcontainer.hadashikick.jp) |
| UniTask | 2.5.11 | async/await без аллокаций | [github.com/Cysharp/UniTask](https://github.com/Cysharp/UniTask) |
| PrimeSDK | 5.2.6 | реклама, покупки, сохранения, язык, аналитика площадки | [prime-publisher.com/sdk](https://prime-publisher.com/sdk/) |
| PrimeGames WebGL Template | 1.0.7 | страница WebGL-сборки и экран загрузки | [github.com/Prime-SDK/PrimeGamesTemplate](https://github.com/Prime-SDK/PrimeGamesTemplate) |
| Яндекс.Метрика | jslib | свои события в WebGL-сборке | [yandex.com/support/metrica](https://yandex.com/support/metrica/) |
| TextMeshPro | 3.0.7 | текст, TMP Essentials импортированы | [docs.unity3d.com](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html) |
| DOTween Pro | 1.0.430 (DOTween 1.3.030) | твины и анимации | [dotween.demigiant.com](https://dotween.demigiant.com/documentation.php) |
| Odin Inspector | 4.0.2.3 | инспекторы, сериализация, editor-тулзы | [odininspector.com](https://odininspector.com/documentation) |
| FlatKit | 4.9.12 | стилизованный шейдинг, импортирован вариант Built-in RP | [flatkit.dustyroom.com](https://flatkit.dustyroom.com) |
| SceneSwitcher Pro | 1.x | переключение сцен из тулбара редактора | [Asset Store](https://assetstore.unity.com/packages/tools/gui/scene-switcher-pro-313355) |
| MCP for Unity (CoplayDev) | git, ветка main | управление открытым редактором из AI-агента | [github.com/CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) |

Наши системы: Audio (`Core/Audio` + `Infrastructure/Audio`), Analytics (`Core/Analytics` + `Infrastructure/Prime`, репортеры Яндекс.Метрика и PrimeSDK), FSM (`Core/StateMachine` + `Composition/Startup`).

---

## 2. Структура проекта

```
Assets/
├── _Project/                          весь код и данные игры
│   ├── Configs/Localization/          LocalizationConfig.asset, таблица переводов
│   ├── Plugins/WebGL/                 YandexMetrica.jslib
│   ├── Prefabs/                       RootLifetimeScope.prefab, корневой контейнер
│   ├── Scenes/                        Bootstrap.unity, Game.unity
│   └── Scripts/
│       ├── Editor/                    сборка _Project.Editor, пока пустая
│       └── Runtime/
│           ├── Core/                  интерфейсы и модели, не знает про SDK
│           ├── Infrastructure/        реализации: PrimeSDK, аудио, сцены, время, FSM
│           ├── Features/              игровые механики, пока пустая
│           ├── UI/                    интерфейс, сейчас LocalizedText
│           └── Composition/           LifetimeScope и стартовые состояния
├── FlatKit/                           шейдеры и демо, unitypackage-файлы в git не попадают
├── Plugins/                           DOTween Pro, Odin Inspector, UniTask
├── Resources/                         DOTweenSettings.asset, настройки PrimeSDK
├── SceneSwitcherPro/                  редакторская тулза
├── Settings/                          VContainerSettings.asset
├── TextMesh Pro/                      ресурсы TMP Essentials
└── WebGLTemplates/PrimeGames/         шаблон страницы WebGL
```

Сборки (asmdef) и их ссылки:

| Сборка | Ссылается на | Что сюда кладём |
|---|---|---|
| `_Project.Core` | UniTask | интерфейсы сервисов, события аналитики, модели данных, ScriptableObject-конфиги |
| `_Project.Infrastructure` | Core, VContainer, UniTask, PrimeGames.SDK | реализации интерфейсов Core, обёртки над SDK и Unity API |
| `_Project.Features` | Core, VContainer, UniTask | игровые механики: уровни, персонажи, экономика |
| `_Project.UI` | Core, Features, VContainer, UniTask, Unity.TextMeshPro | экраны, окна, компоненты интерфейса |
| `_Project.Composition` | Core, Infrastructure, Features, UI, VContainer, UniTask | регистрация в контейнере, состояния запуска |
| `_Project.Editor` | все сборки выше, только Editor | редакторские скрипты и тулзы |

Правила, которые держат структуру:

- интерфейс лежит в `Core`, реализация в `Infrastructure`, регистрация в `Composition`;
- `Core` не ссылается на PrimeSDK. Если интерфейсу нужен тип из SDK, заведите свой класс данных в `Core`;
- игровой код (`Features`, `UI`) работает только через интерфейсы из `Core`;
- namespace повторяет путь папки: `Core/Localization` даёт `_Project.Core.Localization`, `UI/Localization` даёт `_Project.UI.Localization`.

---

## 3. Композиция: VContainer

### 3.1 Три контейнера

| Контейнер | Где | Что регистрирует |
|---|---|---|
| `RootLifetimeScope` | префаб `Assets/_Project/Prefabs/RootLifetimeScope.prefab` | все сервисы, автомат состояний и состояния |
| `BootstrapLifetimeScope` | сцена `Bootstrap` | точку входа `Bootstrapper` |
| `GameLifetimeScope` | сцена `Game` | пока ничего, сюда идут регистрации игровой сцены |

Корневой контейнер указан в `Assets/Settings/VContainerSettings.asset`. VContainer создаёт его один раз при старте, помечает `DontDestroyOnLoad` и делает родителем каждого `LifetimeScope` на сценах. Поле `Parent` у сценовых контейнеров оставляйте пустым: родитель приходит из настроек.

### 3.2 Что регистрирует RootLifetimeScope

```csharp
protected override void Configure(IContainerBuilder builder)
{
    RegisterConfigs(builder);
    RegisterServices(builder);
    RegisterPlatform(builder);
    RegisterStorage(builder);
    RegisterStateMachine(builder);
    RegisterStates(builder);
}
```

*Это фактический код проекта, а не пример.*

| Интерфейс | Реализация | Lifetime | Метод регистрации |
|---|---|---|---|
| `LocalizationConfig` (инстанс) | ассет из поля `_localizationConfig` | Singleton | `RegisterConfigs` |
| `ISceneLoader` | `SceneLoader` | Singleton | `RegisterServices` |
| `IAudioService` | `AudioService` | Singleton | `RegisterServices` |
| `ITimeService` | `TimeService` | Singleton | `RegisterServices` |
| `IPlatformInitializer` | `PrimeSDKInitializer` | Singleton | `RegisterPlatform` |
| `IAnalyticsService` | `PrimeAnalyticsService` | Singleton | `RegisterPlatform` |
| `ILocalizationService` | `PrimeLocalizationService` | Singleton | `RegisterPlatform` |
| `IPurchaseService` | `PrimePurchaseService` | Singleton | `RegisterPlatform` |
| `IAdvertisingService` | `PrimeAdvertisingService` | EntryPoint | `RegisterPlatform` |
| `IExampleStorage` | `ExampleStorage` | Singleton | `RegisterStorage` |
| `IStateFactory` | `StateFactory` | Singleton | `RegisterStateMachine` |
| `IGameStateMachine` | `GameStateMachine` | Singleton | `RegisterStateMachine` |
| `BootstrapState`, `LoadRunState`, `RunState` | сами классы | Transient | `RegisterStates` |

`BootstrapLifetimeScope` регистрирует одну строку:

```csharp
builder.RegisterEntryPoint<Bootstrapper>();
```

*Это фактический код проекта, а не пример.*

### 3.3 Куда регистрировать новое

| Что добавляете | Куда | Как |
|---|---|---|
| Сервис на всю игру | `RootLifetimeScope`, подходящий `Register*` | `builder.Register<Impl>(Lifetime.Singleton).As<IInterface>();` |
| Класс с `Start`, `Tick`, `Dispose` | `RootLifetimeScope` или `GameLifetimeScope` | `builder.RegisterEntryPoint<Impl>().As<IInterface>();` |
| Состояние автомата | `RootLifetimeScope.RegisterStates` | `builder.Register<MyState>(Lifetime.Transient);` |
| Объект только для игровой сцены | `GameLifetimeScope` | `builder.Register<LevelSession>(Lifetime.Singleton);` |
| Компонент со сцены | `GameLifetimeScope` | `builder.RegisterComponentInHierarchy<Hud>();` |

Точка входа (`RegisterEntryPoint`) нужна классу, который должен получать `Start()`, `Tick()` или `Dispose()` от контейнера без своего MonoBehaviour. Так зарегистрирован `PrimeAdvertisingService`: в `Start()` он ждёт SDK и подписывается на покупки, в `Dispose()` отписывается.

### 3.4 Зависимости в MonoBehaviour

Обычные классы получают зависимости через конструктор. У MonoBehaviour конструктора нет, поэтому используется метод с атрибутом `[Inject]`:

```csharp
public class ResultScreen : MonoBehaviour
{
    private IAnalyticsService _analytics;

    [Inject]
    public void Construct(IAnalyticsService analytics) =>
        _analytics = analytics;
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

VContainer внедряет зависимости только в объекты, перечисленные в списке `Auto Inject Game Objects` у `LifetimeScope` на сцене. Добавьте туда корневой объект (например `Canvas`), внедрение пройдёт по всей его иерархии. Объекты, созданные в рантайме, создавайте через `IObjectResolver.Instantiate`, тогда внедрение сработает само.

---

## 4. Запуск: конечный автомат

### 4.1 Порядок запуска

| Шаг | Где | Что происходит |
|---|---|---|
| 1 | сцена `Bootstrap` | создаётся `RootLifetimeScope` со всеми сервисами, затем `BootstrapLifetimeScope` регистрирует `Bootstrapper` |
| 2 | `Bootstrapper.Start()` | вход в `BootstrapState` |
| 3 | `BootstrapState` | `await IPlatformInitializer.InitializeAsync()`, внутри `PrimeSDK.WaitForProviders` |
| 4 | `LoadRunState` | `await ISceneLoader.LoadAsync(SceneId.Game)` |
| 5 | `RunState` | `IAnalyticsService.ReportEvent(new GameReadyEvent())` |

Пока не отработал шаг 3, методы PrimeSDK вызывать нельзя. Весь код, который живёт на сцене `Game`, запускается уже после него.

```csharp
public async UniTask Enter()
{
    await _platformInitializer.InitializeAsync();

    _stateMachine.Enter<LoadRunState>();
}
```

*Это фактический код проекта (`BootstrapState`), приведён сокращённо.*

### 4.2 Из чего состоит автомат

```
Core/StateMachine/
├── IExitableState.cs      UniTask Exit()
├── IState.cs              UniTask Enter()
├── IPayloadState.cs       UniTask Enter(TPayload payload)
├── IStateFactory.cs       TState Create<TState>()
├── IGameStateMachine.cs   Enter<TState>(), Enter<TState, TPayload>(payload)
└── GameStateMachine.cs    реализация

Infrastructure/StateMachine/
└── StateFactory.cs        берёт состояние из контейнера через IObjectResolver
```

Смена состояния: автомат ждёт `Exit()` текущего состояния, создаёт новое через фабрику и вызывает его `Enter()`. Вызов `Enter<T>()` не ждёт завершения перехода, это `Forget()`, поэтому исключение внутри состояния попадёт в консоль через обработчик UniTask, а не в вызывающий код.

### 4.3 Как добавить состояние

Шаг 1. Класс состояния. Зависимости через конструктор, проверка на `null`:

```csharp
public class MenuState : IState
{
    private readonly IGameStateMachine _stateMachine;
    private readonly IAudioService _audio;
    private readonly AudioAsset _menuMusic;

    public MenuState(IGameStateMachine stateMachine, IAudioService audio, AudioAsset menuMusic)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        _menuMusic = menuMusic ?? throw new ArgumentNullException(nameof(menuMusic));
    }

    public UniTask Enter()
    {
        _audio.PlayMusic(_menuMusic);

        return UniTask.CompletedTask;
    }

    public UniTask Exit() =>
        UniTask.CompletedTask;
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Шаг 2. Регистрация в `RootLifetimeScope.RegisterStates`:

```csharp
builder.Register<MenuState>(Lifetime.Transient);
```

Шаг 3. Переход из другого состояния или из игрового кода:

```csharp
_stateMachine.Enter<MenuState>();
```

Состояние с данными реализует `IPayloadState<T>` и вызывается с аргументом:

```csharp
public class LoadLevelState : IPayloadState<int>
{
    public async UniTask Enter(int levelIndex)
    {
        await _sceneLoader.LoadAsync(SceneId.Game);

        _stateMachine.Enter<PlayState, int>(levelIndex);
    }

    public UniTask Exit() =>
        UniTask.CompletedTask;
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Незарегистрированное состояние даст `VContainerException` при первом `Enter`: фабрика не найдёт тип в контейнере.

---

## 5. Сцены

| Сцена | Индекс в Build Settings | Что на ней |
|---|---|---|
| `Bootstrap` | 0 | `BootstrapLifetimeScope`, `YandexMetricaInitializer` |
| `Game` | 1 | `Main Camera` (перспективная), `Directional Light`, `GameLifetimeScope` |

Сцены загружаются по имени: `SceneLoader` вызывает `SceneManager.LoadSceneAsync(sceneId.ToString(), LoadSceneMode.Single)`. Значение перечисления `SceneId` обязано совпадать с именем файла сцены.

Как добавить сцену:

1. Создайте файл в `Assets/_Project/Scenes/`, например `Menu.unity`.
2. Добавьте `Menu` в `Core/SceneManagement/SceneId.cs`.
3. Добавьте сцену в File, Build Settings.
4. Положите на сцену объект с `LifetimeScope` (свой класс в `Composition`), иначе внедрение на этой сцене работать не будет.

Play Mode запускайте со сцены `Bootstrap`, переключаться удобно через тулбар SceneSwitcher Pro. Если стартовать сразу с `Game`, корневой контейнер создастся, но `Bootstrapper` не запустится: SDK не проинициализируется, автомат не стартует.

---

## 6. Сервисы

| Интерфейс | Реализация | Где описан |
|---|---|---|
| `IAnalyticsService`, `IAdvertisingService`, `IPurchaseService`, `IExampleStorage` | `Infrastructure/Prime/*` | [Аналитика, реклама и сохранения](Docs/Analytics-Ads-Saves.md) |
| `ILocalizationService` | `PrimeLocalizationService` | [Локализация](Docs/Localization.md) |
| `IAudioService` | `AudioService` | [Звук: AudioService](Docs/Audio.md) |
| `ISceneLoader` | `SceneLoader` | раздел 5 |
| `IPlatformInitializer` | `PrimeSDKInitializer` | раздел 4.1 |
| `ITimeService` | `TimeService` | ниже |

`ITimeService` отдаёт `DeltaTime` и `Time` из `UnityEngine.Time`. Смысл обёртки в том, что игровая логика не зависит от статики Unity: в тестах сервис подменяется, а замедление времени делается в одном месте.

```csharp
public class TimeService : ITimeService
{
    public float DeltaTime =>
        UnityEngine.Time.deltaTime;

    public float Time =>
        UnityEngine.Time.time;
}
```

*Это фактический код проекта, а не пример.*

В шаблоне нет папки `Assets/_Project/Audio` и микшера, о которых говорит [документ по звуку](Docs/Audio.md). Перед первым `AudioAsset` создайте микшер через Create, Audio Mixer с группами `Music` и `SFX` внутри `Master`.

---

## 7. Новый проект из шаблона

1. Клонируйте репозиторий в папку с именем игры и уберите привязку к шаблону:

    ```bash
    git clone https://github.com/meepifiev/UnityTemplate.git MyGame
    cd MyGame
    git remote remove origin
    ```

2. Project Settings, Player: `Product Name`, `Company Name` (сейчас `GOLTECH STUDIO`), `Version`. Шаблон WebGL уже выбран: `PrimeGames`.
3. Для 2D-игры переключите `Edit, Project Settings, Editor, Default Behavior Mode` на 2D, сделайте камеру на сцене `Game` ортографической и добавьте пакет `com.unity.feature.2d` через Package Manager.
4. PrimeSDK: меню `PrimeSDK, Open Toolkit`, задайте идентификаторы игры и провайдеров площадки.
5. Яндекс.Метрика: на сцене `Bootstrap` у `YandexMetricaInitializer` впишите номер счётчика в поле `_counterId`. Пустое поле выключает Метрику.
6. Картинки страницы: замените `gameIcon.png` (256×256), `gameBackground.png` и `thumbnail.png` в `Assets/WebGLTemplates/PrimeGames/`.
7. Заполните `LocalizationConfig.asset`, создайте микшер и ассеты звуков.
8. FlatKit: импортирован вариант Built-in RP. Файлы `[Render Pipeline] *.unitypackage` в git не хранятся (`.gitignore`), при необходимости берите их из Asset Store.
9. Сборка: File, Build Settings, WebGL. Отчёт PrimeSDK по размеру билда появляется в папке `BuildReports/`.

Бинарные файлы (`png`, `psd`, `fbx`, `wav`, `dll`, `pdf` и другие) идут через Git LFS по правилам из `.gitattributes`. `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.sln` в git не попадают.

---

## 8. Инструменты редактора

| Инструмент | Как пользоваться |
|---|---|
| SceneSwitcher Pro | кнопки сцен в тулбаре редактора, список настраивается в `Assets/SceneSwitcherPro/` |
| Odin Inspector | атрибуты `[Button]`, `[ShowInInspector]`, `[Required]` в любых MonoBehaviour и ScriptableObject |
| DOTween Pro | настройки в `Assets/Resources/DOTweenSettings.asset`; после обновления пакета откройте `Tools, Demigiant, DOTween Utility Panel` и нажмите Setup |
| TextMeshPro | шрифт по умолчанию `LiberationSans SDF`; для кириллицы создайте свой Font Asset через `Window, TextMeshPro, Font Asset Creator` |
| MCP for Unity | пакет `com.coplaydev.unity-mcp` подключает открытый редактор к AI-агенту: сцены, объекты, консоль, тесты |

---

## 9. Как проверить, что шаблон работает

| Что проверяем | Как |
|---|---|
| Запуск | Play со сцены `Bootstrap`: через секунду загружается `Game`, ошибок в консоли нет |
| Контейнер | в иерархии в `DontDestroyOnLoad` есть объект `RootLifetimeScope` |
| SDK | в консоли строка PrimeSDK «Hello! Don't forget to WaitForProviders before using my API», это нормальное сообщение о старте |
| Аналитика | в консоли предупреждение заглушки PrimeSDK про `GameIsReady` после загрузки `Game` |
| Компиляция | предупреждения «Assembly for Assembly Definition File ... will not be compiled» для `_Project.Features` и `_Project.Editor` исчезнут после первого скрипта в этих папках |
| WebGL | собранная страница показывает экран загрузки PrimeGames: иконка, прогресс-бар, затем игра |

---

## 10. Типичные ошибки

| Что произойдёт | Причина | Как исправить |
|---|---|---|
| `NullReferenceException` в `Start` у компонента с `[Inject]` | объект не в `Auto Inject Game Objects` | добавьте корневой объект в список у `LifetimeScope` сцены |
| `VContainerException: No such registration of type` при `Enter<T>` | состояние не зарегистрировано | добавьте `builder.Register<T>(Lifetime.Transient)` в `RegisterStates` |
| `InvalidOperationException` из `SceneLoader` | имя сцены не совпадает с `SceneId` или сцены нет в Build Settings | проверьте имя файла и Build Settings |
| `VContainerParentTypeReferenceNotFound`, сцена не стартует | у `LifetimeScope` заполнено поле `Parent` типом, которого нет на сцене | очистите поле, родитель приходит из `VContainerSettings` |
| Предупреждения PrimeSDK о вызове до инициализации | код обратился к SDK раньше `BootstrapState` | вызывайте SDK только через сервисы и только со сцены `Game` |
| Play со сцены `Game` ничего не запускает | `Bootstrapper` живёт на сцене `Bootstrap` | стартуйте с `Bootstrap` |
| Ошибки компиляции после добавления папки в `Features` | скрипт ссылается на сборку, которой нет в asmdef | добавьте ссылку в `_Project.Features.asmdef` или вынесите интерфейс в `Core` |
