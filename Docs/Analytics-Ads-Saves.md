# Аналитика, реклама и сохранения

Как устроены три платформенных сервиса, как ими пользоваться из игрового кода и как их дополнять.

PrimeSDK 5.2.6 (пакет `com.primesdk`), контейнер зависимостей VContainer 1.19.0, асинхронность на UniTask.

Документация PrimeSDK: [prime-publisher.com/sdk](https://prime-publisher.com/sdk/)

---

## 1. Как всё устроено

Правило одно: **интерфейс лежит в `Core`, реализация в `Infrastructure`, регистрация в `Composition`**. Игровой код никогда не обращается к `PrimeSDK` напрямую, только через интерфейсы из `_Project.Core`.

```
Assets/_Project/Scripts/Runtime/
├── Core/                          сборка _Project.Core, на PrimeSDK НЕ ссылается
│   ├── Advertising/IAdvertisingService.cs
│   ├── Analytics/IAnalyticsService.cs, IAnalyticsReporter.cs, IAnalyticsEvent.cs
│   ├── Analytics/Events/GameReadyEvent.cs, GameplayStartedEvent.cs, GameplayStoppedEvent.cs
│   ├── Purchasing/IPurchaseService.cs
│   ├── Storage/IExampleStorage.cs
│   └── Platform/IPlatformInitializer.cs
├── Infrastructure/                сборка _Project.Infrastructure, ссылается на PrimeGames.SDK
│   ├── Prime/PrimeAdvertisingService.cs
│   ├── Prime/PrimeAnalyticsService.cs
│   ├── Prime/PrimePurchaseService.cs
│   ├── Prime/ExampleStorage.cs
│   ├── Prime/PrimeSDKInitializer.cs
│   └── Analytics/YandexMetrica.cs, YandexMetricaInitializer.cs
└── Composition/
    ├── RootLifetimeScope.cs       здесь регистрируются все сервисы
    └── Startup/                   BootstrapState → LoadRunState → RunState
```

**Почему так.** У сборки `_Project.Core` нет ссылки на `PrimeGames.SDK`, это сделано специально, чтобы типы SDK не попадали в игровой код. Если новому интерфейсу в `Core` вдруг понадобился тип из SDK, значит интерфейс спроектирован неправильно: заведите свой простой класс с данными в `Core`.

### Порядок запуска

| Шаг | Где | Что происходит |
|---|---|---|
| 1 | Сцена `Bootstrap` | `BootstrapLifetimeScope` регистрирует `Bootstrapper`; `RootLifetimeScope` (префаб `Assets/_Project/Prefabs/RootLifetimeScope.prefab`) регистрирует все сервисы |
| 2 | `Bootstrapper.Start()` | Вход в состояние `BootstrapState` |
| 3 | `BootstrapState` | `await IPlatformInitializer.InitializeAsync()`, внутри `PrimeSDK.WaitForProviders(...)` |
| 4 | `LoadRunState` | Загрузка сцены `Game` |
| 5 | `RunState` | `analytics.ReportEvent(new GameReadyEvent())` |

Главное правило PrimeSDK: **пока не отработал `WaitForProviders`, ни один метод SDK вызывать нельзя**. Этот вызов спрятан в `PrimeSDKInitializer`, а ожидание стоит в `BootstrapState`, поэтому весь код, который запускается со сцены `Game`, уже в безопасности.

Отдельно от этого работает `YandexMetricaInitializer`: обычный MonoBehaviour на сцене `Bootstrap`, который в `Awake()` поднимает счётчик Яндекс.Метрики. От PrimeSDK он не зависит.

---

## 2. Аналитика

### 2.1 Два канала

| Канал | Через что | Что отправляет | Где работает |
|---|---|---|---|
| PrimeSDK | `PrimeSDK.Analytics` | Ход игры: `GameIsReady`, `GameplayStart`, `GameplayStop` | Везде. В редакторе заглушки, пишут предупреждение в консоль |
| Яндекс.Метрика | `YandexMetrica` → вызов JS-функций из `YandexMetrica.jslib` | Любые свои цели с параметрами | Только в WebGL-сборке. В редакторе пишет в `Debug.Log` |

Оба канала спрятаны за одним классом `PrimeAnalyticsService`. Он реализует сразу два интерфейса: `IAnalyticsService` (его видит игровой код) и `IAnalyticsReporter` (через него события отправляют себя).

### 2.2 Как это работает

```
Игровой код → IAnalyticsService.ReportEvent(событие)
                    ↓
              событие.Report(reporter)      ← событие само знает, ЧТО и с какими данными отправить
                    ↓
              PrimeAnalyticsService         ← сервис знает, КУДА отправить
                    ↓
              PrimeSDK.Analytics | YandexMetrica
```

Каждое событие это отдельный маленький класс. Данные передаются ему в конструкторе. Игровому коду не нужно знать ни названий событий, ни того, в какую систему они уходят.

### 2.3 Как пользоваться

Запросите `IAnalyticsService` через конструктор (обычный класс):

```csharp
using _Project.Core.Analytics;
using _Project.Core.Analytics.Events;

public class LevelFlow
{
    private readonly IAnalyticsService _analytics;

    public LevelFlow(IAnalyticsService analytics)
    {
        _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
    }

    public void StartLevel() =>
        _analytics.ReportEvent(new GameplayStartedEvent());

    public void StopLevel() =>
        _analytics.ReportEvent(new GameplayStoppedEvent());
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Или через `[Inject]` (если это MonoBehaviour и на сцене есть `LifetimeScope`):

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

`GameplayStart` и `GameplayStop` вызываются при старте и завершении уровня, удобное место для этого: класс в `Features`, который запускает и завершает уровень. На Яндекс Играх вызывать их не обязательно, игра прекрасно работает и без этого. Это просто подсказка площадке, что сейчас идёт активный игровой процесс: зная это, площадка аккуратнее выбирает момент для показа рекламы. Требования других площадок уточняйте отдельно.

### 2.4 Как добавить своё событие

**Шаг 1.** Создайте класс события в `Core/Analytics/Events/`:

```csharp
namespace _Project.Core.Analytics.Events
{
    public class LevelCompletedEvent : IAnalyticsEvent
    {
        private readonly int _levelIndex;
        private readonly float _damage;
        private readonly int _throwsLeft;

        public LevelCompletedEvent(int levelIndex, float damage, int throwsLeft)
        {
            _levelIndex = levelIndex;
            _damage = damage;
            _throwsLeft = throwsLeft;
        }

        public void Report(IAnalyticsReporter reporter)
        {
            reporter.ReportDesignEvent("level_completed", new Dictionary<string, string>
            {
                ["level"] = _levelIndex.ToString(),
                ["damage"] = Mathf.RoundToInt(_damage).ToString(),
                ["throws_left"] = _throwsLeft.ToString(),
            });
        }
    }
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

**Шаг 2.** Вызовите его из игрового кода:

```csharp
_analytics.ReportEvent(new LevelCompletedEvent(3, session.CurrentDamage, session.ThrowsLeft));
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Всё. Регистрировать событие в контейнере не нужно, оно создаётся обычным `new`.

> **А когда добавлять новый метод в `IAnalyticsReporter`?** Почти никогда. Свой метод нужен, только если событие должно уйти в особый метод SDK, как `GameplayStart`, который площадка понимает по-своему. Обычные игровые события отправляйте через `ReportDesignEvent`, иначе интерфейс разрастётся до сотни методов.
>
> Если всё же добавили метод в `IAnalyticsReporter`, не забудьте реализовать его в `PrimeAnalyticsService`, иначе проект не соберётся.

Значения параметров всегда строки. Числа приводите к строке сами (`ToString()`).

### 2.5 Настройка Яндекс.Метрики

`YandexMetricaInitializer` лежит на сцене `Assets/_Project/Scenes/Bootstrap.unity`, номер счётчика задаётся в поле `_counterId` в инспекторе.

> ⚠️ Сейчас поле пустое, поэтому Метрика фактически выключена: при пустом номере `Init` сразу выходит. Перед сборкой на площадку впишите туда номер счётчика.

JS-часть лежит в `Assets/_Project/Plugins/WebGL/YandexMetrica.jslib`: она подключает скрипт Метрики на страницу и вызывает `ym(номер, 'reachGoal', имя, данные)`. Трогать её обычно не нужно.

---

## 3. Реклама

### 3.1 Интерфейс

`Core/Advertising/IAdvertisingService.cs`:

```csharp
public interface IAdvertisingService
{
    void ShowInterstitial();                                              // реклама между уровнями
    void ShowRewarded(string rewardId, Action rewarded, Action closed);   // реклама за награду
}
```

*Это фактический код проекта, а не пример.*

### 3.2 Реализация

`Infrastructure/Prime/PrimeAdvertisingService.cs`, обёртка над `PrimeSDK.Ads`. Зарегистрирована как точка входа, поэтому VContainer сам вызывает у неё `Start()` и `Dispose()`:

```csharp
builder.RegisterEntryPoint<PrimeAdvertisingService>().As<IAdvertisingService>();
```

*Это фактический код проекта, а не пример.*

Что она делает сверх простого проброса вызовов:

1. **Ждёт SDK.** В `Start()` вызывает `PrimeSDK.WaitForProviders(...)`.
2. **Следит за покупками.** Подписана на `IPurchaseService.Changed`. Если игрок купил «без рекламы», баннер выключается, а `ShowInterstitial()` просто ничего не делает.
3. **Отписывается** в `Dispose()`, иначе останется висеть подписка на уничтоженный объект.
4. **Упрощает награду.** SDK отдаёт один общий обработчик `onClose(bool успех)`. Сервис превращает его в два понятных: `rewarded` (вызывается только при успешном просмотре) и `closed` (вызывается всегда).

Обратите внимание: реклама за награду **не** проверяет «без рекламы». Так и задумано: покупка не должна отбирать у игрока возможность получить награду за просмотр.

### 3.3 Как пользоваться

Реклама за награду:

```csharp
public class ExtraThrowsButton : MonoBehaviour
{
    [SerializeField] private Button _button;

    private IAdvertisingService _ads;
    private LevelSession _session;

    [Inject]
    public void Construct(IAdvertisingService ads, LevelSession session)
    {
        _ads = ads;
        _session = session;
    }

    public void OnClick()
    {
        _button.interactable = false;

        _ads.ShowRewarded(
            rewardId: "extra_throws",
            rewarded: () => _session.AddThrows(3),
            closed: () => _button.interactable = true);
    }
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Реклама между уровнями:

```csharp
_ads.ShowInterstitial();   // ничего не вернёт: если куплено «без рекламы», вызов просто не сработает
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

**Что важно помнить:**

- `rewardId` это метка, которую площадка показывает в своей статистике. Храните такие метки константами в одном месте, не пишите строки прямо в местах вызова.
- `rewarded` обязателен: если передать `null`, вылетит `ArgumentNullException`. `closed` можно не передавать.
- Награду выдавайте **только** в `rewarded`, а кнопки и интерфейс возвращайте в рабочее состояние в `closed`. После успешного просмотра вызываются оба.
- Не показывайте рекламу между уровнями сразу после рекламы за награду и не ставьте её на каждый перезапуск уровня: площадки за это снижают доход.

### 3.4 Рядом: покупки

`Core/Purchasing/IPurchaseService.cs` → `Infrastructure/Prime/PrimePurchaseService.cs`. Сейчас поддержан один товар:

```csharp
private const string NoAdsProductId = "no_ads";        // метка товара на площадке
private const string NoAdsOwnedKey  = "no_ads.owned";  // локальная отметка в сохранениях
```

*Это фактический код проекта, а не пример.*

Свойство `AdsDisabled` возвращает `true`, если сработало хотя бы одно из двух: есть локальная отметка или площадка подтверждает покупку через `IsAlreadyPurchased`. После успешной покупки отметка записывается, вызывается сохранение и поднимается событие `Changed`, на него реагирует рекламный сервис и убирает баннер.

Если понадобится второй товар, вынесите метки товаров в настройку (ScriptableObject) и замените `BuyRemoveAds()` на `BuyProduct(string productId, Action onSuccess)`. В нынешнем виде метод рассчитан ровно на один товар.

---

## 4. Сохранения

### 4.1 Интерфейс

`Core/Storage/IExampleStorage.cs`:

```csharp
public interface IExampleStorage
{
    int Coins { get; set; }
    void Save();
}
```

*Это фактический код проекта, а не пример.*

Реализация `Infrastructure/Prime/ExampleStorage.cs`, тонкая обёртка над `PrimeSDK.Data`:

```csharp
public int Coins
{
    get =>
        PrimeSDK.Data.GetInt(CoinsKey, 0);

    set =>
        PrimeSDK.Data.SetInt(CoinsKey, value, important: false);
}

public void Save() =>
    PrimeSDK.Data.Save();
```

*Это фактический код проекта, а не пример.*

Имя `ExampleStorage` говорит само за себя: это образец из заготовки проекта. Настоящие сохранения (прогресс уровней, звёзды, настройки) пишутся по его примеру, см. 4.3.

### 4.2 Как работает `PrimeSDK.Data`

Все значения лежат в памяти в четырёх словарях (`bool`, `int`, `float`, `string`), а при сохранении превращаются в одну строку JSON и уходят в хранилище платформы:

| Где запускаем | Кто сохраняет | Куда физически попадает |
|---|---|---|
| Редактор Unity | `UnityEngineData` | `PlayerPrefs`, ключ `PrimeGames.SDK.Data` |
| WebGL-сборка на площадке | `PrimeWebData` | В облако площадки через JS |
| Заглушка | `FallbackData` | Никуда не пишет |

**Параметр `important`:**

- `important: true`: сразу после записи запрашивается сохранение;
- `important: false`: значение меняется только в памяти и попадёт в хранилище при ближайшем вызове `Save()`.

**`Save()` не пишет мгновенно.** Он только поднимает флаг «надо сохранить». Отдельная корутина внутри SDK записывает данные на ближайшем кадре и потом не чаще, чем раз в **3 секунды**. Поэтому вызывать `Save()` часто не страшно, но и рассчитывать, что данные окажутся в хранилище сию секунду, нельзя.

**Правило на практике:** обычные значения пишите с `important: false` и вызывайте `Save()` один раз в конце важного момента: завершение уровня, выход в меню. Монеты и покупки пишите с `important: true`, чтобы не потерять их при внезапном закрытии вкладки.

### 4.3 Как добавить своё сохраняемое значение

Быстро: добавить поле в существующий класс:

```csharp
// Core/Storage/IExampleStorage.cs
int BestScore { get; set; }

// Infrastructure/Prime/ExampleStorage.cs
private const string BestScoreKey = "best_score";

public int BestScore
{
    get =>
        PrimeSDK.Data.GetInt(BestScoreKey, 0);

    set =>
        PrimeSDK.Data.SetInt(BestScoreKey, value, important: false);
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Правильно: завести отдельный класс под свою область. Так и делайте, вместо того чтобы раздувать `ExampleStorage`:

```csharp
// Core/Storage/ILevelProgressStorage.cs
namespace _Project.Core.Storage
{
    public interface ILevelProgressStorage
    {
        int LastUnlockedLevel { get; set; }
        int GetStars(int levelIndex);
        void SetStars(int levelIndex, int stars);
        void Save();
    }
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

```csharp
// Infrastructure/Prime/LevelProgressStorage.cs
public class LevelProgressStorage : ILevelProgressStorage
{
    private const string LastUnlockedKey = "progress.last_unlocked";
    private const string StarsKeyPrefix  = "progress.stars.";

    public int LastUnlockedLevel
    {
        get =>
            PrimeSDK.Data.GetInt(LastUnlockedKey, 0);

        set =>
            PrimeSDK.Data.SetInt(LastUnlockedKey, value, important: false);
    }

    public int GetStars(int levelIndex) =>
        PrimeSDK.Data.GetInt(StarsKeyPrefix + levelIndex, 0);

    public void SetStars(int levelIndex, int stars) =>
        PrimeSDK.Data.SetInt(StarsKeyPrefix + levelIndex, stars, important: false);

    public void Save() =>
        PrimeSDK.Data.Save();
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

И зарегистрируйте его в `RootLifetimeScope.RegisterStorage`:

```csharp
builder.Register<LevelProgressStorage>(Lifetime.Singleton).As<ILevelProgressStorage>();
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Ключи именуйте с префиксом области (`progress.`, `settings.`), так их легко найти и не пересечься с чужими.

### 4.4 Сохранение сложных данных

Для целых структур есть `GetObject<T>` и `SetObject<T>`. Внутри обычный `JsonUtility`, со всеми его ограничениями: класс должен быть помечен `[Serializable]`, сохраняются только открытые поля (не свойства), `Dictionary` не поддерживается.

```csharp
[Serializable]
public class ProfileData
{
    public int coins;
    public int lastLevel;
    public int[] stars;
}

// запись
PrimeSDK.Data.SetObject("profile", profile, important: false);
PrimeSDK.Data.Save();

// чтение
ProfileData profile = PrimeSDK.Data.GetObject("profile", new ProfileData());
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Ещё есть `HasKey`, `DeleteKey` и `DeleteAll`, удобно для кнопки «сбросить прогресс» в отладочном меню. Учтите: `DeleteAll` сотрёт и отметку о покупке `no_ads.owned`.

### 4.5 Как очистить сохранения

Во время разработки постоянно нужно пройти игру «с нуля»: проверить первый запуск, туториал, выдачу стартовых монет. В редакторе все сохранения лежат в `PlayerPrefs`, и чистятся они одним пунктом меню:

**Edit → Clear All PlayerPrefs**

Пункт стирает вообще все сохранённые данные проекта: и прогресс, и отметку о покупке «без рекламы», и настройки. Игру после этого нужно перезапустить: `PrimeSDK.Data` читает данные один раз при старте и дальше держит их в памяти, поэтому пока сцена играет, старые значения останутся.

Если нужно стереть не всё, а только часть, сделайте кнопку в отладочном меню:

```csharp
// стереть одно значение
PrimeSDK.Data.DeleteKey("progress.last_unlocked");

// стереть все данные PrimeSDK
PrimeSDK.Data.DeleteAll();

PrimeSDK.Data.Save();
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Так же это работает и в собранной игре, где меню редактора недоступно.

---

## 5. Как проверить, что всё работает

| Что проверяем | Как |
|---|---|
| Реклама | Запустите Play Mode: поверх игры появится окно «рекламы» с кнопкой закрытия, обработчики вызовутся по-настоящему |
| Аналитика PrimeSDK | В консоли редактора появится предупреждение от заглушки, значит событие дошло до сервиса. Дальше в редакторе оно никуда не уходит, это нормально |
| Яндекс.Метрика | В редакторе: строка `[YandexMetrica] (sim) ym(...)` в консоли. В WebGL-сборке: вкладка Network в браузере, запросы на `mc.yandex.ru`, и отчёт «Конверсии» в кабинете Метрики |
| Сохранения | В редакторе всё лежит в `PlayerPrefs` под ключом `PrimeGames.SDK.Data` одной строкой JSON. Как сбросить, см. 4.5 |
| Покупки | В редакторе покупка проходит без настоящей оплаты, можно проверять логику «без рекламы» |

Сообщения самого SDK в консоли начинаются с имени его части. Строка «Hello! Don't forget to WaitForProviders before using my API» означает, что SDK запустился: она появляется всегда и ошибкой не является.

Если ответа на вопрос нет в этом документе, смотрите официальную документацию PrimeSDK: [prime-publisher.com/sdk](https://prime-publisher.com/sdk/).
