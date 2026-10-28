# Аналитика, реклама и сохранения

Как устроены три платформенных сервиса, как ими пользоваться из игрового кода и как их дополнять.

JTL SDK 1.7.2 (пакет `com.jtlstudio.sdk`), контейнер зависимостей VContainer 1.19.0, асинхронность на UniTask.

Документация JTL SDK: [meepifiev.github.io/com.jtlstudio.sdk](https://meepifiev.github.io/com.jtlstudio.sdk/), всё одним файлом для ИИ-ассистента: [llms.txt](https://meepifiev.github.io/com.jtlstudio.sdk/llms.txt)

---

## 1. Как всё устроено

Правило одно: **интерфейс лежит в `Core`, реализация в `Infrastructure`, регистрация в `Composition`**. Игровой код никогда не обращается к `JTLSDK` напрямую, только через интерфейсы из `_Project.Core`.

```
Assets/_Project/Scripts/Runtime/
├── Core/                          сборка _Project.Core, на JTL SDK НЕ ссылается
│   ├── Advertising/IAdvertisingService.cs
│   ├── Analytics/IAnalyticsService.cs, IAnalyticsReporter.cs, IAnalyticsEvent.cs
│   ├── Analytics/Events/GameReadyEvent.cs, GameplayStartedEvent.cs, GameplayStoppedEvent.cs
│   ├── Purchasing/IPurchaseService.cs
│   ├── Storage/IExampleStorage.cs
│   └── Platform/IPlatformInitializer.cs
├── Infrastructure/                сборка _Project.Infrastructure, ссылается на JTLStudio.SDK
│   ├── JTL/JTLAdvertisingService.cs
│   ├── JTL/JTLAnalyticsService.cs
│   ├── JTL/JTLPurchaseService.cs
│   ├── JTL/ExampleStorage.cs
│   ├── JTL/JTLSDKInitializer.cs
│   └── Analytics/YandexMetrica.cs
└── Composition/
    ├── RootLifetimeScope.cs       здесь регистрируются все сервисы
    └── Startup/                   BootstrapState → LoadRunState → RunState
```

**Почему так.** У сборки `_Project.Core` нет ссылки на `JTLStudio.SDK`, это сделано специально, чтобы типы SDK не попадали в игровой код. Если новому интерфейсу в `Core` вдруг понадобился тип из SDK, значит интерфейс спроектирован неправильно: заведите свой простой класс с данными в `Core`.

### Порядок запуска

| Шаг | Где | Что происходит |
|---|---|---|
| 1 | Сцена `Bootstrap` | `BootstrapLifetimeScope` регистрирует `Bootstrapper`; `RootLifetimeScope` (префаб `Assets/_Project/Prefabs/RootLifetimeScope.prefab`) регистрирует все сервисы |
| 2 | `Bootstrapper.Start()` | Вход в состояние `BootstrapState` |
| 3 | `BootstrapState` | `await IPlatformInitializer.InitializeAsync()`, внутри `JTLSDK.Create()` и `JTLSDK.WhenReady(...)` |
| 4 | `LoadRunState` | Загрузка сцены `Game` |
| 5 | `RunState` | `analytics.ReportEvent(new GameReadyEvent())` |

Главное правило JTL SDK: **до `JTLSDK.Create()` любое обращение к модулям бросает `InvalidOperationException`**. Без исключений читаются только `JTLSDK.IsCreated`, `IsReady`, `Version` и `LogLevel`. Создание SDK спрятано в `JTLSDKInitializer`, а ожидание стоит в `BootstrapState`, поэтому весь код, который запускается со сцены `Game`, уже в безопасности.

`InitializeAsync` можно вызывать сколько угодно раз: SDK создаётся один раз, все вызовы ждут одну и ту же готовность. Сервисы рекламы и покупок пользуются этим в своём `Start()`. Готовность приходит всегда: модуль, который не ответил за 15 секунд, помечается `Failed` и дальше отвечает безопасными значениями, без исключений.

Отдельно от этого работает `YandexMetrica`: точка входа в `RootLifetimeScope`, которая в `Initialize()` поднимает счётчик Яндекс.Метрики, как только собран корневой контейнер. От JTL SDK она не зависит.

---

## 2. Аналитика

### 2.1 Два канала

| Канал | Через что | Что отправляет | Где работает |
|---|---|---|---|
| JTL SDK | `JTLSDK.GameEvents` | Ход игры: `GameReady`, `GameplayStarted`, `GameplayStopped` | Везде. В редакторе работает симуляция SDK |
| Яндекс.Метрика | `YandexMetrica` → вызов JS-функций из `YandexMetrica.jslib` | Любые свои цели с параметрами | Только в WebGL-сборке. В редакторе пишет в `Debug.Log` |

Оба канала спрятаны за одним классом `JTLAnalyticsService`. Он реализует сразу два интерфейса: `IAnalyticsService` (его видит игровой код) и `IAnalyticsReporter` (через него события отправляют себя).

### 2.2 Как это работает

```
Игровой код → IAnalyticsService.ReportEvent(событие)
                    ↓
              событие.Report(reporter)      ← событие само знает, ЧТО и с какими данными отправить
                    ↓
              JTLAnalyticsService           ← сервис знает, КУДА отправить
                    ↓
              JTLSDK.GameEvents | YandexMetrica
```

Каждое событие это отдельный маленький класс. Данные передаются ему в конструкторе. Игровому коду не нужно знать ни названий событий, ни того, в какую систему они уходят.

### 2.3 Как пользоваться

Запросите `IAnalyticsService` через конструктор (обычный класс):

```csharp
using _Project.Core.Analytics;
using _Project.Core.Analytics.Events;

public class LevelSession
{
    private readonly IAnalyticsService _analytics;

    public LevelSession(IAnalyticsService analytics)
    {
        _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
    }

    public void StartLevel()
    {
        _analytics.ReportEvent(new GameplayStartedEvent());
    }

    public void StopLevel()
    {
        _analytics.ReportEvent(new GameplayStoppedEvent());
    }
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Или через `[Inject]` (если это MonoBehaviour и на сцене есть `LifetimeScope`):

```csharp
public class ResultScreen : MonoBehaviour
{
    private IAnalyticsService _analytics;

    [Inject]
    public void Construct(IAnalyticsService analytics)
    {
        _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
    }
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

`GameplayStarted` и `GameplayStopped` вызываются при старте и завершении уровня, а также при открытии и закрытии своего меню паузы. Удобное место для этого: класс в `Features`, который запускает и завершает уровень. Яндекс получает их как `GameplayAPI.start()` и `stop()`, на YouTube меняется только внутреннее состояние SDK. На время рекламы и паузы площадки SDK сам останавливает геймплей и возобновляет его после, вручную это делать не нужно. Повторный вызов с тем же состоянием ничего не отправляет.

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
_analytics.ReportEvent(new LevelCompletedEvent(_levelIndex, _session.CurrentDamage, _session.ThrowsLeft));
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Всё. Регистрировать событие в контейнере не нужно, оно создаётся обычным `new`.

> **А когда добавлять новый метод в `IAnalyticsReporter`?** Почти никогда. Свой метод нужен, только если событие должно уйти в особый метод SDK, как `GameplayStarted`, который площадка понимает по-своему. Обычные игровые события отправляйте через `ReportDesignEvent`, иначе интерфейс разрастётся до сотни методов.
>
> Если всё же добавили метод в `IAnalyticsReporter`, не забудьте реализовать его в `JTLAnalyticsService`, иначе проект не соберётся.

Значения параметров всегда строки. Числа приводите к строке сами (`ToString()`).

### 2.5 Настройка Яндекс.Метрики

Номер счётчика задаётся на префабе `Assets/_Project/Prefabs/RootLifetimeScope.prefab`, поле `Yandex Metrica Counter Id`. Контейнер передаёт его в конструктор `YandexMetrica`:

```csharp
builder.RegisterEntryPoint<YandexMetrica>().AsSelf().WithParameter(_yandexMetricaCounterId);
```

*Это фактический код проекта, а не пример.*

`JTLAnalyticsService` получает `YandexMetrica` через конструктор, как любую другую зависимость.

> ⚠️ Сейчас поле пустое, поэтому Метрика фактически выключена: при пустом номере `Initialize` сразу выходит. Перед сборкой на площадку впишите туда номер счётчика.

JS-часть лежит в `Assets/_Project/Plugins/WebGL/YandexMetrica.jslib`: она подключает скрипт Метрики на страницу и вызывает `ym(номер, 'reachGoal', имя, данные)`. Трогать её обычно не нужно.

У JTL SDK есть свой пакет аналитики `com.jtlstudio.sdk.analytics`, он тоже шлёт цели в Метрику, а счётчик хранит в конфигурации площадки. В проекте он не установлен. Если решите на него перейти, уберите `YandexMetrica.jslib` и класс `YandexMetrica`, иначе скрипт Метрики поднимется на странице дважды.

---

## 3. Реклама

### 3.1 Интерфейс

`Core/Advertising/IAdvertisingService.cs`:

```csharp
public interface IAdvertisingService
{
    void ShowInterstitial();
    void ShowRewarded(string rewardId, Action rewarded, Action closed);
}
```

*Это фактический код проекта, а не пример.*

`ShowInterstitial` показывает рекламу между уровнями, `ShowRewarded` рекламу за награду.

### 3.2 Реализация

`Infrastructure/JTL/JTLAdvertisingService.cs`, обёртка над `JTLSDK.Ads`. Зарегистрирована как точка входа, поэтому VContainer сам вызывает у неё `Start()` и `Dispose()`:

```csharp
builder.RegisterEntryPoint<JTLAdvertisingService>().As<IAdvertisingService>();
```

*Это фактический код проекта, а не пример.*

Что она делает сверх простого проброса вызовов:

1. **Ждёт SDK.** В `Start()` ждёт `IPlatformInitializer.InitializeAsync()` и, если куплено «без рекламы», прячет баннер. Ожидание получает токен отмены, который отменяется в `Dispose()`.
2. **Следит за покупками.** Подписана на `IPurchaseService.Changed`. Если игрок купил «без рекламы», баннер прячется через `HideBanner()`, а `ShowInterstitial()` просто ничего не делает.
3. **Отписывается** в `Dispose()`, иначе останется висеть подписка на уничтоженный объект.
4. **Упрощает награду.** SDK отдаёт один колбэк с `AdResult`. Сервис превращает его в два понятных: `rewarded` (вызывается только при `AdResult.Rewarded`) и `closed` (вызывается всегда).

Обратите внимание: реклама за награду **не** проверяет «без рекламы». Так и задумано: покупка не должна отбирать у игрока возможность получить награду за просмотр.

Остальное SDK делает сам. Пока реклама на экране, игра стоит на паузе: `Time.timeScale` равен 0, звук выключен, ввод не доходит до игры, геймплей для площадки остановлен. Интервалы на Яндексе задаются в конфигурации площадки: по умолчанию interstitial не чаще раза в 60 секунд и не раньше 60 секунд после досмотренной рекламы за награду. Внутри интервала `ShowInterstitial()` ничего не показывает. Там же включается sticky banner, который показывается сразу при старте.

### 3.3 Как пользоваться

Реклама за награду:

```csharp
public class ExtraThrowsButton : MonoBehaviour
{
    private const string RewardId = "extra_throws";
    private const int RewardedThrows = 3;

    [SerializeField] private Button _button;

    private IAdvertisingService _ads;
    private LevelSession _session;

    [Inject]
    public void Construct(IAdvertisingService ads, LevelSession session)
    {
        _ads = ads ?? throw new ArgumentNullException(nameof(ads));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public void OnClick()
    {
        _button.interactable = false;

        _ads.ShowRewarded(
            rewardId: RewardId,
            rewarded: () => _session.AddThrows(RewardedThrows),
            closed: () => _button.interactable = true);
    }
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Реклама между уровнями вызывается одной строкой и ничего не возвращает. Если куплено «без рекламы», вызов просто не сработает:

```csharp
_ads.ShowInterstitial();
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

**Что важно помнить:**

- `rewardId` обязателен: пустая строка даёт `ArgumentException` из SDK. На YouTube он уходит в `requestRewardedAd`, на Яндексе не используется. Храните такие метки константами в одном месте, не пишите строки прямо в местах вызова.
- `rewarded` обязателен: если передать `null`, вылетит `ArgumentNullException`. `closed` можно не передавать.
- Награду выдавайте **только** в `rewarded`, а кнопки и интерфейс возвращайте в рабочее состояние в `closed`. После успешного просмотра вызываются оба.
- Не показывайте рекламу между уровнями сразу после рекламы за награду и не ставьте её на каждый перезапуск уровня: площадки за это снижают доход.

### 3.4 Рядом: покупки

`Core/Purchasing/IPurchaseService.cs`:

```csharp
public interface IPurchaseService
{
    bool AdsDisabled { get; }

    string RemoveAdsPrice { get; }

    event Action Changed;

    void BuyRemoveAds();
}
```

*Это фактический код проекта, а не пример.*

Реализация `Infrastructure/JTL/JTLPurchaseService.cs`. Сейчас поддержан один товар: `NoAdsProductId` это id товара в каталоге JTL SDK, `NoAdsOwnedKey` это отметка в сохранениях.

```csharp
private const string NoAdsProductId = "no_ads";
private const string NoAdsOwnedKey = "no_ads.owned";
```

*Это фактический код проекта, а не пример.*

Товар `no_ads` нужно объявить в окне JTL SDK, раздел Purchases, с типом Non-consumable. Без этого `Purchase` сразу уходит в ошибку: товара нет в каталоге. Если id на Яндексе другой, он задаётся в поле Yandex Games id. Кнопка Generate constants в том же разделе пишет классы `ProductIds` в `Assets/Scripts/Generated/`, но этот файл попадает в `Assembly-CSharp`, а наши сборки на неё сослаться не могут, поэтому id храните константами в `Infrastructure`.

Свойство `AdsDisabled` возвращает `true`, если сработало хотя бы одно из двух: в сохранениях есть отметка или площадка подтверждает покупку через `IsAlreadyPurchased`. После успешной покупки отметка записывается в `JTLSDK.Data` и поднимается событие `Changed`, на него реагирует рекламный сервис и прячет баннер. Сохраняет сейв и помечает покупку выданной сам SDK.

Свойство `RemoveAdsPrice` отдаёт цену `no_ads` строкой, которую уже отформатировала площадка: её выводят на кнопку покупки как есть. Вернётся пустая строка, если товара нет в каталоге JTL SDK, модуль покупок не в состоянии Ready (ещё не готов, не ответил или площадка без покупок, как YouTube) или площадка не вернула этот товар. Кнопку с пустой ценой лучше спрятать, чем показывать без цены.

Сервис зарегистрирован как точка входа: в `Start()` он ждёт SDK и вызывает `RestorePurchases`, а в `Dispose()` отменяет ожидание. Так возвращается покупка, которую игрок оплатил, но не получил: оборвалась связь или закрылась вкладка. На площадках без покупок (YouTube) восстановление пропускается.

Если понадобится второй товар, вынесите id товаров в настройку (ScriptableObject) и замените `BuyRemoveAds()` на `BuyProduct(string productId, Action onSuccess)`. Восстановление при этом тоже должно уметь выдавать любой товар, а не только `no_ads`. Расходуемые товары (монеты) начисляйте прибавлением к значению в `JTLSDK.Data`, а не присваиванием: SDK списывает такую покупку только после сохранения.

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

Реализация `Infrastructure/JTL/ExampleStorage.cs`, тонкая обёртка над `JTLSDK.Data`:

```csharp
public int Coins
{
    get => JTLSDK.Data.GetInt(CoinsKey);
    set => JTLSDK.Data.SetInt(CoinsKey, value);
}

public void Save()
{
    JTLSDK.Data.Save();
}
```

*Это фактический код проекта, а не пример.*

Имя `ExampleStorage` говорит само за себя: это образец из заготовки проекта. Настоящие сохранения (прогресс уровней, звёзды, настройки) пишутся по его примеру, см. 4.3.

### 4.2 Как работает `JTLSDK.Data`

Все ключи лежат в памяти и уходят на площадку одним JSON-документом целиком:

| Где запускаем | Куда физически попадает | Лимит |
|---|---|---|
| Редактор Unity | `PlayerPrefs`, ключ `JTLSDK.Data` | как у площадки активной конфигурации |
| Яндекс Игры | Облако игрока: `player.getData` и `player.setData` | 200 KiB, предупреждение после 100 KiB |
| YouTube Playables | Облако: `ytgame.game.loadData` и `saveData` | 3 MiB, предупреждение после 500 KiB |

Документ больше лимита не отправляется, в консоли будет ошибка. Резервной копии в `localStorage` нет.

**Загрузка.** SDK загружает сейв при старте, модуль становится готовым только после первой загрузки, а `InitializeAsync` ждёт и его. До готовности чтение возвращает значение по умолчанию с предупреждением, а запись не выполняется. Если загрузка не удалась, запись блокируется, чтобы не затереть прогресс в облаке, и загрузка повторяется каждые 30 секунд.

**Параметр `important`** (по умолчанию `true`):

- `important: true`: значение меняется в памяти и запускает автосохранение, документ уйдёт на площадку через 2 секунды после последнего важного изменения;
- `important: false`: значение меняется только в памяти и уйдёт на площадку со следующим важным изменением, при `Save()` или когда игрок скроет вкладку.

**`Save(onSaved)` отправляет сразу**, вместе со всеми неважными изменениями. Колбэк получает `false`, если сейв ещё не загружен или загрузился с ошибкой. Если уже идёт другая запись, новая встаёт в очередь.

**Правило на практике:** монеты, открытые уровни и покупки пишите как есть, с `important` по умолчанию. С `important: false` пишите то, что меняется часто: прогресс внутри уровня, позицию игрока, счётчики. В конце важного момента (завершение уровня, выход в меню) вызывайте `Save()`.

Закрытие вкладки SDK ловит сам: когда страница скрывается, изменённый сейв уходит на площадку. Если игрок на Яндексе войдёт в аккаунт, SDK перечитает сейв уже с аккаунта.

### 4.3 Как добавить своё сохраняемое значение

Быстро: добавить поле в существующий класс. В интерфейс `Core/Storage/IExampleStorage.cs`:

```csharp
int BestScore { get; set; }
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

И в реализацию `Infrastructure/JTL/ExampleStorage.cs`:

```csharp
private const string BestScoreKey = "best_score";

public int BestScore
{
    get => JTLSDK.Data.GetInt(BestScoreKey);
    set => JTLSDK.Data.SetInt(BestScoreKey, value);
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Правильно: завести отдельный класс под свою область. Так и делайте, вместо того чтобы раздувать `ExampleStorage`. Интерфейс `Core/Storage/ILevelProgressStorage.cs`:

```csharp
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

Реализация `Infrastructure/JTL/LevelProgressStorage.cs`:

```csharp
public class LevelProgressStorage : ILevelProgressStorage
{
    private const string LastUnlockedKey = "progress.last_unlocked";
    private const string StarsKeyPrefix = "progress.stars.";

    public int LastUnlockedLevel
    {
        get => JTLSDK.Data.GetInt(LastUnlockedKey);
        set => JTLSDK.Data.SetInt(LastUnlockedKey, value);
    }

    public int GetStars(int levelIndex)
    {
        return JTLSDK.Data.GetInt(StarsKeyPrefix + levelIndex);
    }

    public void SetStars(int levelIndex, int stars)
    {
        JTLSDK.Data.SetInt(StarsKeyPrefix + levelIndex, stars);
    }

    public void Save()
    {
        JTLSDK.Data.Save();
    }
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

И зарегистрируйте его в `RootLifetimeScope.RegisterStorage`:

```csharp
builder.Register<LevelProgressStorage>(Lifetime.Singleton).As<ILevelProgressStorage>();
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Ключи именуйте с префиксом области (`progress.`, `settings.`), так их легко найти и не пересечься с чужими. Ключи `jtlsdk.*` занимает сам SDK.

### 4.4 Сохранение сложных данных

Для целых структур есть `GetObject<T>` и `SetObject<T>`. Внутри обычный `JsonUtility`, со всеми его ограничениями: класс должен быть помечен `[Serializable]`, сохраняются только поля, которые сериализует `JsonUtility` (открытые и `[SerializeField]`, не свойства), `Dictionary` не поддерживается.

Такой класс это чистая модель данных, поэтому открытые поля в нём допустимы:

```csharp
[Serializable]
public class ProfileData
{
    public int Coins;
    public int LastLevel;
    public int[] Stars;
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Запись и чтение внутри класса хранилища в `Infrastructure`:

```csharp
private const string ProfileKey = "profile";

public void SaveProfile(ProfileData profile)
{
    JTLSDK.Data.SetObject(ProfileKey, profile);
    JTLSDK.Data.Save();
}

public ProfileData LoadProfile()
{
    return JTLSDK.Data.GetObject(ProfileKey, new ProfileData());
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Ещё есть `HasKey`, `DeleteKey` и `DeleteAll`, удобно для кнопки «сбросить прогресс» в отладочном меню. Учтите: `DeleteAll` сотрёт и отметку о покупке `no_ads.owned`, и служебную отметку SDK о выданных покупках `jtlsdk.granted`.

### 4.5 Как очистить сохранения

Во время разработки постоянно нужно пройти игру «с нуля»: проверить первый запуск, туториал, выдачу стартовых монет. В редакторе сейв лежит в `PlayerPrefs`, а смотреть и чистить его удобнее всего в окне SDK:

**JTL SDK → Toolkit → Saves**: значения можно поправить руками, очистить, выгрузить и загрузить JSON. В разделе Simulation есть `Simulate load failure` и `Empty save on start`.

Пункт **Edit → Clear All PlayerPrefs** тоже работает, но стирает вообще всё: сейв, симуляцию покупок редактора (`JTLSDK.Prototype.Purchases`) и настройки. Игру после очистки нужно перезапустить: SDK читает сейв один раз при старте и дальше держит его в памяти.

Если нужно стереть не всё, а только часть, сделайте кнопку в отладочном меню. `DeleteKey` стирает одно значение, `DeleteAll` все данные; после них вызывайте `Save()`. Методы живут в классе хранилища в `Infrastructure`, а кнопка вызывает их через интерфейс из `Core`:

```csharp
public void ResetProgress()
{
    JTLSDK.Data.DeleteKey(LastUnlockedKey);
    JTLSDK.Data.Save();
}

public void ResetAll()
{
    JTLSDK.Data.DeleteAll();
    JTLSDK.Data.Save();
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Так же это работает и в собранной игре, где меню редактора недоступно.

---

## 5. Как проверить, что всё работает

| Что проверяем | Как |
|---|---|
| Реклама | В Play Mode SDK показывает во вкладке Game окно с вариантами ответа площадки, обработчики вызываются по-настоящему. Ответы настраиваются в окне JTL SDK, раздел Simulation |
| Игровые события | В консоли строки `[JTL SDK]` о `GameReady` и смене геймплея (при уровне логов «Всё») |
| Яндекс.Метрика | В редакторе: строка `[YandexMetrica] (sim) ym(...)` в консоли. В WebGL-сборке: вкладка Network в браузере, запросы на `mc.yandex.ru`, и отчёт «Конверсии» в кабинете Метрики |
| Сохранения | Окно JTL SDK, раздел Saves. Как сбросить, см. 4.5 |
| Покупки | Без настоящей оплаты: Simulation, Purchases, `Ask every time` или `Use selected result`. Вариант `paid, restored on next launch` проверяет восстановление: товар придёт через `RestorePurchases` при следующем запуске Play Mode |

Сообщения SDK в консоли начинаются с `[JTL SDK]`. Уровень логов задаётся в окне SDK (Configurations, General settings, Logging level) или из кода через `JTLSDK.LogLevel`.

Если ответа на вопрос нет в этом документе, смотрите официальную документацию JTL SDK: [meepifiev.github.io/com.jtlstudio.sdk](https://meepifiev.github.io/com.jtlstudio.sdk/).
