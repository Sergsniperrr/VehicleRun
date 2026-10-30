# VehicleRun

2D WebGL-игра на Unity 2022.3.62f2 (Built-in Render Pipeline, пакет `com.unity.feature.2d`) для площадок, подключённых через JTL SDK (Яндекс Игры, YouTube Playables). Проект собран из шаблона UnityTemplate. Каркас уже готов: VContainer, конечный автомат запуска, загрузка сцен, сервисы площадки. Игровой код пишется в сборках `Features` и `UI`.

## Документация

| Документ | О чём |
|---|---|
| [Docs/TechPlan.md](Docs/TechPlan.md) | тех.план игры: принятые решения, изменения ГДД, что берём из референса Merge Dinosaurs, архитектура, этапы и задачи |
| [README.md](README.md) | стек, структура, сборки, композиция VContainer, запуск, сцены, новый проект из шаблона, типичные ошибки |
| [Docs/Analytics-Ads-Saves.md](Docs/Analytics-Ads-Saves.md) | аналитика (JTL SDK + Яндекс.Метрика), реклама, покупки, сохранения `JTLSDK.Data` |
| [Docs/Localization.md](Docs/Localization.md) | `LocalizationConfig`, ключи, `LocalizedText`, `ILocalizationService` |
| [Docs/Audio.md](Docs/Audio.md) | `AudioService`, `AudioAsset`, микшер и громкость |
| [.claude/CODE_STYLE.md](.claude/CODE_STYLE.md) | стиль C#/Unity-кода: именование, порядок членов, скобки, события, UniTask, VContainer, ошибки, запрещённые запахи |
| [JTL SDK](https://meepifiev.github.io/com.jtlstudio.sdk/) | официальная документация SDK; полный справочник API одним файлом: [llms.txt](https://meepifiev.github.io/com.jtlstudio.sdk/llms.txt) |

Перед изменением системы прочитай её документ. Если меняешь поведение системы, обнови документ. Примеры кода в документах подписываются строкой «*Это фактический код проекта, а не пример.*» или «*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*».

## Архитектура

Код лежит в `Assets/_Project/Scripts/Runtime/`, редакторские скрипты в `Assets/_Project/Scripts/Editor/`.

| Сборка | Ссылается на | Что кладём |
|---|---|---|
| `_Project.Core` | UniTask | интерфейсы сервисов, события аналитики, модели данных, ScriptableObject-конфиги |
| `_Project.Infrastructure` | Core, VContainer, UniTask, JTLStudio.SDK | реализации интерфейсов Core, обёртки над SDK и Unity API |
| `_Project.Features` | Core, VContainer, UniTask | игровые механики |
| `_Project.UI` | Core, Features, VContainer, UniTask, TextMeshPro | экраны, окна, компоненты интерфейса |
| `_Project.Composition` | все сборки выше | регистрация в контейнере, состояния запуска |
| `_Project.Editor` | все сборки выше, только Editor | редакторские тулзы |
| `_Project.Tests.EditMode` | Core, Features, Test Framework, только Editor | EditMode-тесты логики, папка `Assets/_Project/Scripts/Tests/EditMode/` |

- Интерфейс лежит в `Core`, реализация в `Infrastructure`, регистрация в `Composition`.
- `Core` не ссылается на JTL SDK. Если интерфейсу нужен тип из SDK, заведи свой класс данных в `Core`.
- `Features` и `UI` работают только через интерфейсы `Core` и никогда не вызывают `JTLSDK` напрямую. Обёртки над SDK лежат в `Infrastructure/JTL` (namespace `_Project.Infrastructure.JTL`; не называй папку `JTLSDK`, иначе namespace перекроет класс `JTLSDK`).
- Не добавляй в asmdef ссылки против направления зависимостей из таблицы.
- Namespace повторяет путь папки: `Runtime/Core/Localization` даёт `_Project.Core.Localization`.

## Композиция (VContainer)

- Сервис на всю игру регистрируй в `RootLifetimeScope` (префаб `Assets/_Project/Prefabs/RootLifetimeScope.prefab`) в подходящем методе `Register*`: `builder.Register<Impl>(Lifetime.Singleton).As<IInterface>();`.
- Классу нужны `Start`, `Tick` или `Dispose` от контейнера: `builder.RegisterEntryPoint<Impl>().As<IInterface>();`.
- Объекты только игровой сцены регистрируй в `GameLifetimeScope`.
- Обычные классы получают зависимости через конструктор. MonoBehaviour получает их через метод `Construct` с `[Inject]`, а объект должен быть в списке `Auto Inject Game Objects` у `LifetimeScope` сцены. Объекты, созданные в рантайме, создавай через `IObjectResolver.Instantiate`.
- Поле `Parent` у сценовых `LifetimeScope` оставляй пустым: родитель приходит из `Assets/Settings/VContainerSettings.asset`.

## Запуск, сцены, JTL SDK

- Порядок: сцена `Bootstrap` → `Bootstrapper` → `BootstrapState` (`IPlatformInitializer.InitializeAsync`: `JTLSDK.Create()` и `JTLSDK.WhenReady`) → `LoadRunState` (загрузка `Game`) → `RunState` (`GameReadyEvent`).
- До `JTLSDK.Create()` любое обращение к модулям SDK бросает `InvalidOperationException`. Сервис, которому SDK нужен раньше сцены `Game`, сначала делает `await IPlatformInitializer.InitializeAsync()`: метод идемпотентный, SDK создаётся один раз.
- Колбэки SDK превращай в `UniTask` через `UniTaskCompletionSource`.
- `Time.timeScale`, `AudioListener.volume`/`pause`, `Cursor` и `PlayerPrefs` принадлежат JTL SDK. Не пиши в них напрямую: используй `JTLSDK.Time.Scale`, `JTLSDK.Audio`, `JTLSDK.Device`, `JTLSDK.Data`, обёрнутые сервисом из `Core`.
- Id товаров храни константами в `Infrastructure`: сгенерированный SDK файл `Assets/Scripts/Generated/JTLSDKIds.cs` попадает в `Assembly-CSharp`, и наши сборки его не видят. Каждый товар нужно объявить в окне JTL SDK, раздел Purchases.
- Настройки SDK (`Assets/Resources/JTLSDK`, `Assets/Settings/JTLSDK`) и шаблон страницы (`Assets/WebGLTemplates/JTLSDK`) создаёт окно `JTL SDK › Toolkit`, вручную их не правь.
- Play Mode запускай только со сцены `Bootstrap`.
- Новое состояние: класс с `IState` или `IPayloadState<T>` и `builder.Register<T>(Lifetime.Transient)` в `RootLifetimeScope.RegisterStates`. `Enter<T>()` вызывается через `Forget()`, поэтому исключения из состояния попадают в лог UniTask, а не к вызывающему.
- Новая сцена: файл в `Assets/_Project/Scenes/`, значение в `SceneId` с тем же именем, что у файла, сцена в Build Settings, свой `LifetimeScope` на сцене.
- Сохраняемые данные каждой области держи в отдельном классе хранилища (по образцу `ExampleStorage`), ключи пиши с префиксом области (`progress.`, `settings.`). Префикс `jtlsdk.` занят SDK.
- Тексты, которые видит игрок, заводи ключами в `LocalizationConfig.asset`, а не строками в коде.

## Стиль кода

Стиль C#/Unity-кода описан в `.claude/CODE_STYLE.md` (перенесён из проекта IJ-SmashAndHit). Он действует при любом написании и ревью кода:

@.claude/CODE_STYLE.md

## Чего не трогать

- Сторонний код не правим: `Assets/Plugins/`, `Assets/FlatKit/`, `Assets/TextMesh Pro/`, `Assets/SceneSwitcherPro/`.
- `.meta`-файлы коммитятся вместе с ассетами.
- Бинарные файлы (`png`, `psd`, `fbx`, `wav`, `dll`, `pdf` и другие) идут через Git LFS по правилам `.gitattributes`.
- Сцены, префабы и `.asset` меняй через редактор (MCP for Unity), а правку текста YAML оставляй для точечных случаев.

## Проверка

- Компиляцию и консоль проверяй в открытом редакторе через MCP for Unity (сервер `UnityMCP`, `http://127.0.0.1:8080/mcp`; подключается кнопкой Configure для Claude Code в окне `Window › MCP for Unity`, настройка локальная и в репозиторий не попадает). Если редактор закрыт, компиляцию проверяет запуск в batchmode: `Unity.exe -batchmode -nographics -quit -projectPath <проект> -logFile <лог>`, ошибки ищи в логе по `error CS`.
- Play со сцены `Bootstrap`: через секунду загружается `Game`, ошибок в консоли нет.
- Это нормальные сообщения, не ошибки:
  - строки с префиксом `[JTL SDK]` о старте и состоянии модулей;
  - ошибка `[JTL SDK] Settings were not found at Resources/JTLSDK/JTLSDKSettings`, пока в окне SDK не создана конфигурация площадки;
  - предупреждения «Assembly for Assembly Definition File ... will not be compiled» для пустых сборок `_Project.Features` и `_Project.Editor`.
