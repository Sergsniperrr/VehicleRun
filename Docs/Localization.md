# Локализация

Как устроены переводы, где заводить ключи, как повесить текст на элемент интерфейса и как всё это дополнять.

Язык игрок не выбирает: его сообщает площадка через JTL SDK. Поддержаны два варианта текста, русский и английский.

---

## 1. Из чего всё состоит

```
Assets/_Project/
├── Configs/Localization/LocalizationConfig.asset   таблица переводов
├── Prefabs/RootLifetimeScope.prefab                в нём поле с ссылкой на этот ассет
└── Scripts/Runtime/
    ├── Core/Localization/
    │   ├── ILocalizationService.cs                 интерфейс, который видит игровой код
    │   └── LocalizationConfig.cs                   описание таблицы (ScriptableObject)
    ├── Infrastructure/JTL/
    │   └── JTLLocalizationService.cs               реализация, берёт язык у JTL SDK
    └── UI/Localization/
        └── LocalizedText.cs                        компонент для текста на сцене
```

Регистрация в `RootLifetimeScope`:

```csharp
builder.RegisterInstance(_localizationConfig);
builder.Register<JTLLocalizationService>(Lifetime.Singleton).As<ILocalizationService>();
```

*Это фактический код проекта, а не пример.*

Ассет с переводами лежит в поле `_localizationConfig` на префабе `RootLifetimeScope` и попадает в сервис через конструктор.

---

## 2. Где заводить ключи

Все тексты игры лежат в одном ассете: `Assets/_Project/Configs/Localization/LocalizationConfig.asset`. Откройте его и заполняйте список `Entries`. У каждой записи три поля:

| Поле | Что в нём |
|---|---|
| `Key` | Название строки, по которому её запрашивает код. Например `ui.menu.play` |
| `Russian` | Русский текст |
| `English` | Английский текст |

Оба текстовых поля многострочные, так что длинные описания и текст в несколько абзацев вставляются нормально.

Сейчас в ассете ноль записей: таблицу наполняете вы по мере появления текстов в игре.

**Про названия ключей.** Ключ это не сам текст, а его адрес, поэтому пишите его латиницей и по смыслу, от общего к частному:

```
ui.menu.play
ui.menu.settings
ui.level.throws_left
popup.no_ads.title
popup.no_ads.button
```

Договоритесь внутри команды об одном стиле и держитесь его. Разнобой вроде `MenuPlay`, `ui_menu_play` и `play_button` в одном файле быстро превращается в кашу, а найти нужную строку среди сотни становится тяжело.

Ещё пара правил, которые экономят время:

- один текст равно один ключ, не заводите две записи с одинаковым содержимым;
- не подставляйте в ключ номер уровня или счёт, для подстановок есть `string.Format`, об этом ниже;
- если нужен второй ассет с переводами (например отдельный для дев-меню), он создаётся через **Create → _Project → Localization Config**, но по умолчанию хватает одного.

---

## 3. Текст на сцене: компонент LocalizedText

Это самый частый способ. Компонент вешается на объект, у которого уже есть `TMP_Text` (обычный `Text` из старого UI не подойдёт), и в поле `Key` вписывается ключ из таблицы.

```csharp
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string _key;

    private TMP_Text _label;
    private ILocalizationService _localization;

    private void Awake()
    {
        _label = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        _label.text = _localization.Get(_key);
    }

    [Inject]
    public void Construct(ILocalizationService localization)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }
}
```

*Это фактический код проекта, а не пример.*

**Важный шаг, без которого компонент не заработает.** Сервис приходит в компонент через `[Inject]`, а VContainer сам по себе не раздаёт зависимости всем объектам сцены. Он внедряет их только в те объекты, которые указаны в списке `Auto Inject Game Objects` у `LifetimeScope` на сцене.

Поэтому порядок такой:

1. Повесьте `LocalizedText` на объект с `TMP_Text`, впишите ключ.
2. Выберите на сцене объект с `LifetimeScope` (например `GameLifetimeScope`).
3. В его инспекторе разверните список `Auto Inject Game Objects` и добавьте туда корневой `Canvas`.

Внедрение идёт вглубь по всей иерархии, поэтому одного `Canvas` в списке достаточно для всех текстов внутри него. Если этого не сделать, в `Start` вы получите `NullReferenceException`: сервис не пришёл.

---

## 4. Текст из кода

Когда текст собирается в коде (например счёт или сообщение о победе), запросите сервис напрямую:

```csharp
public class ResultScreen : MonoBehaviour
{
    private const string VictoryTitleKey = "level.victory.title";

    [SerializeField] private TMP_Text _title;

    private ILocalizationService _localization;

    [Inject]
    public void Construct(ILocalizationService localization)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }

    public void ShowVictory(int levelIndex)
    {
        string title = string.Format(_localization.Get(VictoryTitleKey), levelIndex);
        _title.text = title;
    }
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

В таблице при этом лежит строка с местом для подстановки:

| Key | Russian | English |
|---|---|---|
| `level.victory.title` | `Уровень {0} пройден` | `Level {0} complete` |

Следите, чтобы `{0}` был в обоих переводах, иначе в одном из языков число просто пропадёт.

У сервиса есть и второй метод, `Localize(russian, english)`. Он возвращает нужный вариант без всякой таблицы:

```csharp
_title.text = _localization.Localize("Победа", "Victory");
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Это быстрый способ для одноразовых строк, до которых руки не дошли. Злоупотреблять им не стоит: тексты расползаются по коду, и потом их не собрать в одном месте. Всё, что видит игрок в готовой игре, должно жить в таблице.

---

## 5. Как это работает внутри

При первом же обращении к сервису происходит следующее. Он спрашивает у JTL SDK текущий язык (`JTLSDK.Language.Current`). Если это русский, дальше берётся колонка `Russian`, во всех остальных случаях `English`. Затем все записи из ассета складываются в словарь, и дальше запросы идут уже к нему, а не к ассету.

Какой язык вернёт SDK, решают его настройки: окно JTL SDK, раздел Languages. Там задаются языки проекта, Default language и Replacements, замены для языков, которых в проекте нет. SDK берёт код языка у площадки и ищет его среди языков проекта; если не нашёл, берёт первую подходящую замену, иначе Default language. Для этого проекта разумно поставить языки Russian и English, Default language English, а украинский, белорусский и казахский заменять на русский.

Отсюда три следствия, которые стоит держать в голове.

**Язык определяется один раз.** Он запоминается при первом обращении и больше не проверяется. SDK умеет менять язык на ходу (`JTLSDK.Language.Set` и событие `Changed`), но сервис на это событие не подписан, поэтому для смены языка нужен перезапуск игры.

**Языков ровно два.** JTL SDK знает 26 языков, но у нас всё, что не русский, показывается по-английски.

**Пропущенный ключ не роняет игру.** Если ключа нет в таблице, `Get` вернёт сам ключ. На экране вы увидите надпись вроде `ui.menu.play` вместо текста. Это удобно: опечатка или забытая строка видны сразу, без чтения логов.

---

## 6. Как проверить

Язык в редакторе задаётся в настройках JTL SDK, а не в системе. Откройте **JTL SDK → Toolkit**, раздел Languages, и поставьте `Russian` или `English` в поле Start language, затем перезапустите Play Mode. Переключатель языка в углу вкладки Game меняет язык на ходу, а сервис запоминает язык при первом обращении, поэтому после переключения тоже нужен перезапуск.

| Что проверяем | Как |
|---|---|
| Ключ найден | На экране осмысленный текст, а не сам ключ |
| Переключение языка | Смените Start language в окне JTL SDK, перезапустите игру, текст поменялся |
| Внедрение сработало | Нет `NullReferenceException` в `Start` у `LocalizedText`, объект добавлен в `Auto Inject Game Objects` |
| Длинные строки | Проверяйте на английском и на русском: русский текст обычно длиннее, кнопки от него расползаются |

В собранной игре язык приходит от площадки и настройками редактора уже не управляется.

---

## 7. Как дополнять

**Добавить третий язык.** Сейчас в записи два текстовых поля. Для третьего языка добавьте поле в `Entry`, разберите язык в сервисе и включите язык в окне JTL SDK, раздел Languages:

```csharp
private string Pick(LocalizationConfig.Entry entry)
{
    return _language switch
    {
        Language.Russian => entry.Russian,
        Language.Turkish => entry.Turkish,
        _ => entry.English,
    };
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Если языков становится больше трёх, поля в инспекторе перестают быть удобными. Тогда лучше перейти на список пар «язык и текст» внутри записи, на таблицу из внешнего файла или на пакет JTL SDK `com.jtlstudio.sdk.localization`: в нём таблицы ключей, компонент `Localized Text`, шрифты по языкам и автоперевод пустых строк. Пакет ставится из окна SDK, раздел Package Manager.

**Обновлять текст при смене языка.** Нужно, если появится переключатель языка в настройках. Сервис подписывается на `JTLSDK.Language.Changed`, пересобирает словарь и поднимает своё событие. В интерфейс `Core/Localization/ILocalizationService.cs` добавляется:

```csharp
event Action LanguageChanged;
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

А `UI/Localization/LocalizedText.cs` подписывается на него и перечитывает свой ключ. Подписка стоит в `Start`, а не в `OnEnable`: к `Start` сервис уже внедрён, а `OnEnable` может сработать раньше внедрения.

```csharp
private void Start()
{
    _localization.LanguageChanged += OnLanguageChanged;
    ApplyText();
}

private void OnDestroy()
{
    _localization.LanguageChanged -= OnLanguageChanged;
}

private void ApplyText()
{
    _label.text = _localization.Get(_key);
}

private void OnLanguageChanged()
{
    ApplyText();
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

**Локализовать не только текст.** Картинки с надписями, озвучка, иконки валюты делаются по тому же принципу: свой ассет с записями и свой компонент рядом с `LocalizedText`.

**Найти забытые ключи.** Когда текстов станет много, полезен редакторский скрипт: собрать все `LocalizedText` в сценах и префабах и сверить их ключи с таблицей. Всё, что не нашлось, вывести списком в консоль. Это дешевле, чем искать глазами надпись `popup.no_ads.titel` на скриншоте перед релизом.

**Тексты для объявлений площадки.** Название игры и описание в кабинете площадки живут отдельно от этой таблицы, их заполняют при публикации.
