# Звук: AudioService

Как устроено воспроизведение звуков и музыки, как добавить новый звук и как дополнить сервис.

Звук работает на встроенных средствах Unity (`AudioSource` и `AudioMixer`), никаких внешних плагинов здесь нет.

Микшер `Assets/_Project/Audio/AudioMixers/AudioMixer.mixer` уже создан: группы `Music` и `SFX` внутри `Master`. У групп `Music` и `SFX` объявлены exposed-параметры `MusicVolume` и `SFXVolume` — через них окно настроек задаёт громкость (раздел 6). Папки `Music` и `Sounds` заводятся вместе с первыми файлами. Звуки и музыка референса лежат отдельно, в `Assets/_Project/Art/Placeholders/Audio/` (см. [TechPlan.md](TechPlan.md), раздел 3.3).

---

## 1. Как всё устроено

Те же правила, что и у остальных сервисов: интерфейс лежит в `Core`, реализация в `Infrastructure`, регистрация в `Composition`.

```
Assets/_Project/
├── Audio/
│   ├── AudioMixers/AudioMixer.mixer    группы Master, Music, SFX
│   ├── Music/                          сюда кладём музыкальные файлы
│   └── Sounds/                         сюда кладём звуки
└── Scripts/Runtime/
    ├── Core/Audio/
    │   ├── IAudioService.cs            интерфейс, который видит игровой код
    │   ├── AudioAsset.cs               настройка одного звука (ScriptableObject)
    │   └── AudioCategories.cs          SFX или Music
    └── Infrastructure/Audio/
        └── AudioService.cs             реализация
```

Регистрация в `RootLifetimeScope.RegisterServices`:

```csharp
builder.Register<AudioService>(Lifetime.Singleton).As<IAudioService>();
```

*Это фактический код проекта, а не пример.*

Сервис один на всю игру. Контейнер создаёт его при первом обращении, а в конструкторе сервис создаёт пустой объект `AudioService` и помечает его `DontDestroyOnLoad`. Все источники звука становятся его дочерними объектами, поэтому при смене сцены звук не пропадает, а в иерархии видно, что играет прямо сейчас.

---

## 2. AudioAsset: настройка одного звука

Звук в проекте описывается не ссылкой на файл, а отдельным ассетом `AudioAsset`. Создаётся через меню: **Create → _Project → Audio → Audio Asset**.

Поля:

| Поле | Что задаёт |
|---|---|
| `Category` | `SFX` или `Music`. Определяет, каким методом звук разрешено играть |
| `Output Mixer Group` | Группа микшера, через которую пойдёт звук: `Music` или `SFX` |
| `Audio Clips` | Массив файлов. При каждом воспроизведении берётся случайный |
| `Volume` | Громкость от 0 до 1 |
| `Pitch` | Скорость и высота звучания от 0.1 до 3, обычное значение 1 |
| `Pitch Variation` | Случайный разброс высоты от 0 до 1. При каждом воспроизведении высота берётся случайно от `Pitch − Pitch Variation` до `Pitch + Pitch Variation` и остаётся в пределах 0.1–3. При 0 высота всегда равна `Pitch` |

Разброс высоты нужен для того же, что и несколько клипов: частый звук (клик, слияние, монеты) с `Pitch Variation` 0,05–0,15 перестаёт звучать одинаково. Музыке разброс оставляйте 0: иначе при каждом запуске мелодия шла бы с другой скоростью. Высоту выбирает `AudioAsset.GetRandomPitch()`. У всех ассетов игры разброс сейчас 0, звучание прежнее.

Массив клипов нужен для разнообразия: положите в один ассет 3-4 варианта удара, и одинаковый звук перестанет надоедать. Если вариант всего один, просто оставьте в массиве одну строку.

Папки `Audio/Music` и `Audio/Sounds` создаёте вы под свои звуки. Разумно держать ассет рядом с самими файлами и называть по смыслу, например `Sfx_Hit_Body`, `Music_Menu`.

---

## 3. Как играть звук

Запросите `IAudioService` через `[Inject]` или конструктор, а сам звук положите в поле `[SerializeField]`:

```csharp
using System;
using _Project.Core.Audio;
using UnityEngine;
using VContainer;

public class HitFeedback : MonoBehaviour
{
    [SerializeField] private AudioAsset _hitSound;

    private IAudioService _audio;

    [Inject]
    public void Construct(IAudioService audio)
    {
        _audio = audio ?? throw new ArgumentNullException(nameof(audio));
    }

    public void OnHit()
    {
        _audio.PlayOneShot(_hitSound);
    }
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Музыка включается так же, одним вызовом:

```csharp
_audio.PlayMusic(_menuMusic);
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

Всего два метода:

| Метод | Для чего | Требует категорию |
|---|---|---|
| `PlayOneShot(AudioAsset)` | Короткий звук: удар, взрыв, клик | `SFX` |
| `PlayMusic(AudioAsset)` | Фоновая музыка, играет по кругу | `Music` |

**Фоновая музыка геймплея** уже подключена так (первое использование сервиса в игре): на файле владельца `Assets/_Project/Audio/Music/VehicleRunMusic.ogg` создан `AudioAsset` `Music_Gameplay` (категория Music, группа микшера Music), а `RunState.Enter()` вызывает `_audio.PlayMusic(_gameplayMusic)` — музыка играет зацикленно с начала геймплея. Ассет приходит в состояние через `WithParameter<AudioAsset>` из поля `_gameplayMusic` на префабе `RootLifetimeScope`. Громкость музыки регулирует слайдер «Музыка» в настройках (группа микшера Music, раздел 6).

---

## 4. Что происходит внутри

**Короткий звук.** Сервис берёт случайный клип из ассета, создаёт новый объект с `AudioSource`, переносит в него настройки ассета (клип, группа микшера, громкость, высота с учётом случайного разброса `Pitch Variation`), запускает воспроизведение и планирует удаление объекта. Время жизни считается по длине клипа с поправкой на высоту звучания плюс 0.1 секунды запаса, так что звук всегда успевает доиграть.

**Музыка.** Источник создаётся так же, но с зацикливанием. Перед запуском новой мелодии старая останавливается, её объект удаляется.

Отдельно стоит запомнить: если попросить включить ту же самую музыку, которая уже играет, сервис ничего не сделает. Мелодия продолжит играть с того же места, а не начнётся заново. Это удобно, когда вызов `PlayMusic` стоит при входе в сцену или в состояние: перезапуск сцены не собьёт музыку.

---

## 5. Типичные ошибки

| Что произойдёт | Причина | Как исправить |
|---|---|---|
| `InvalidOperationException` при `PlayOneShot` | У ассета категория `Music` | Поставьте `SFX` или играйте через `PlayMusic` |
| `InvalidOperationException` при `PlayMusic` | У ассета категория `SFX` | Поставьте `Music` |
| `InvalidOperationException: _audioClips` при каждом вызове | В ассете пустой массив клипов | Добавьте хотя бы один файл |
| `InvalidOperationException: _audioClips` время от времени | В массиве есть пустой элемент. Клип выбирается случайно, поэтому ошибка бывает, только когда выбор попадает на пустую строку | Заполните или удалите пустые строки |
| `NullReferenceException` | Поле `AudioAsset` не заполнено в инспекторе | Перетащите ассет в поле |
| Звука нет, ошибок тоже | Громкость 0, либо не назначена группа микшера, либо группа приглушена в микшере | Проверьте `Volume` у ассета и ползунки в `AudioMixer` |

Проверка категории сделана специально: она ловит случай, когда музыку по ошибке запускают как короткий звук. Такой звук не зациклится и не остановится вместе со сменой мелодии, поэтому лучше получить ошибку сразу.

---

## 6. Микшер и громкость

Микшер лежит в `Assets/_Project/Audio/AudioMixers/AudioMixer.mixer`, в нём три группы: `Master`, а внутри неё `Music` и `SFX`. Каждый `AudioAsset` указывает свою группу в поле `Output Mixer Group`, поэтому вся музыка и все звуки регулируются двумя ползунками, а `Master` приглушает всё сразу.

Громкость музыки и эффектов задаёт окно настроек. Оно сделано так (задача 3.6, частично; окно `SettingsWindow` в [Gameplay.md](Gameplay.md), раздел 13):

- **Параметры микшера.** У групп `Music` и `SFX` открыты (exposed) параметры `Volume` под именами `MusicVolume` и `SFXVolume`. Менять громкость из кода можно только через такие параметры.
- **Запись в микшер.** Интерфейс `IAudioMixerControl` (`Core/Audio`) и его реализация `AudioMixerControl` (`Infrastructure/Audio`) принимают громкость в долях 0..1, переводят её в децибелы и вызывают `AudioMixer.SetFloat`. Микшер держит ссылку в конструкторе. `Core` не знает про `AudioMixer`: тип лежит только в реализации.

```csharp
public void SetMusicVolume(float linearVolume)
{
    _mixer.SetFloat(MusicVolumeParameter, ToDecibels(linearVolume));
}

private float ToDecibels(float linearVolume)
{
    return Mathf.Log10(Mathf.Max(linearVolume, MinLinearVolume)) * DecibelsPerDecade;
}
```

*Это фактический код проекта (`AudioMixerControl`), приведён сокращённо.*

Громкость у микшера задаётся в децибелах, а не долями от нуля до единицы, поэтому значение ползунка переводят через логарифм. Если передать напрямую, половина ползунка на слух окажется почти тишиной. Константа `MinLinearVolume` нужна, чтобы не брать логарифм от нуля.

- **Состояние и сохранение.** `AudioVolumeSettings` (`Features/Settings`) держит две громкости 0..1 (по умолчанию 1), клэмпит их, применяет к микшеру через `IAudioMixerControl` и сохраняет при каждом изменении. Как entry point (`IInitializable`) при старте сцены `Game` он грузит сохранённое и применяет. Сохранение — отдельное хранилище `ISettingsStorage` (`Core/Save`) и `JTLSettingsStorage` (`Infrastructure/JTL`): DTO `SettingsSave` под ключом `settings.audio` в `JTLSDK.Data`, запись неважная (`important: false`), SDK сбрасывает её на площадку при выходе. Это отдельный от `GameSave` ключ с префиксом области `settings.`.
- **Регистрация.** `JTLSettingsStorage` — в `RootLifetimeScope`, микшер (serialized поле `_audioMixer` у `GameLifetimeScope`), `AudioMixerControl` и `AudioVolumeSettings` — в `GameLifetimeScope.RegisterAudio`.

Слайдер «Музыка» регулирует фоновую музыку (раздел 3), слайдер «Эффекты» — звуки игры из раздела 7: все они идут через группу `SFX`.

Общую громкость игры и тишину своего меню паузы по-прежнему задавайте через `JTLSDK.Audio.Volume` и `JTLSDK.Audio.Paused` (ниже), а ползунки `Music` и `SFX` оставьте микшеру.

Микшер и `AudioListener` это разные уровни. `AudioListener.volume` и `AudioListener.pause` принадлежат JTL SDK: на время рекламы, оплаты и паузы площадки он глушит звук, а потом возвращает значения игры. Прямая запись в них перезапишется при следующей паузе. Общую громкость игры и тишину своего меню паузы задавайте через `JTLSDK.Audio.Volume` и `JTLSDK.Audio.Paused`, заведя для этого методы в `IAudioService`, а ползунки `Music` и `SFX` оставьте микшеру.

---

## 7. Звуки игры

Звуки владельца лежат в `Assets/_Project/Audio/VFX/`. На каждом файле создан `AudioAsset` с категорией `SFX` и группой микшера `SFX`, а все собраны в один конфиг `GameSoundsConfig.asset` в той же папке. Где играть звук, определено по имени файла:

| Файл | `AudioAsset` | Когда играет | Кто играет |
|---|---|---|---|
| `Click.ogg` (0,13 с) | `Sfx_Click` | нажатие любой кнопки интерфейса: HUD, окна, строки магазина, узлы гифт-дорожки | `ButtonClickSound` на объекте кнопки |
| `Window.ogg` (0,18 с) | `Sfx_Window` | показ любого окна, кроме окна левелапа | `Window.Show()` |
| `Merge.ogg` (0,65 с) | `Sfx_Merge` | успешное слияние двух машин (`GarageBoard.VehicleMerged`) | `GameplaySounds` |
| `Money.ogg` (1,0 с) | `Sfx_Money` | выплата на финише (`TrackRoster.FinishCrossed`) и забор награды гифт-дорожки (`CollectionMilestones.MilestoneClaimed`) | `GameplaySounds` |
| `LevelUp.ogg` (0,93 с) | `Sfx_LevelUp` | показ окна левелапа, то есть открытие нового ранга | `LevelUpWindow` |
| `BoxOpen` | `Sfx_BoxOpen` | открытие коробки в гараже (`GarageBoard.BoxOpened`) | `GameplaySounds` |
| `BoxShaking` | `Sfx_BoxShake` | каждый цикл тряски коробки в слоте | `GarageSlotView` |

Как это устроено:

- **`GameSoundsConfig`** (`Core/Audio`, меню **Create → _Project → Audio → Game Sounds Config**) — ScriptableObject со ссылками на `AudioAsset` (`Click`, `Window`, `Merge`, `Money`, `LevelUp`, `BoxOpen`, `BoxShake`) и полем `Money Min Interval Seconds` = 0,5. Ассет назначен в поле `Game Sounds` у `GameLifetimeScope`, без него контейнер бросает `InvalidOperationException`.
- **`GameplaySounds`** (`Features/Sounds`) — entry point (`IStartable`, `IDisposable`) игровой сцены. Подписывается на события логики и вызывает `IAudioService.PlayOneShot`. Звук монет играет не чаще раза в `Money Min Interval Seconds` по `ITimeService.Time`: при восьми машинах на трассе выплаты идут чаще секунды, а звук длится секунду, без ограничения они складывались бы в гул.
- **`UiSounds`** (`UI/Sounds`) — singleton игровой сцены с методами `PlayClick`, `PlayWindowOpened` и `PlayLevelUp`.
- **`ButtonClickSound`** (`UI/Sounds`) — компонент рядом с `Button`: подписывается на `onClick` и играет клик. Выключенная кнопка (`interactable` = false) не щёлкает: `onClick` у неё не срабатывает. Компонент стоит на всех кнопках `Canvas` сцены `Game` и на кнопке префаба `ShopRow`. На затемнении карточки машины его нет: это не кнопка в глазах игрока.
- **Окна.** `Window.Show()` вызывает виртуальный `PlayOpenSound()`, по умолчанию это звук окна. `LevelUpWindow` переопределяет его звуком левелапа, чтобы два звука не накладывались.
- **`GarageSlotView`** (`Features/Garage`) получает `IAudioService` и `GameSoundsConfig` через `[Inject] Construct` (слоты внедряются из `Auto Inject` объекта `World`). Он играет `BoxShake` в колбэке DOTween-последовательности тряски — раз за цикл, перед наклонами ([Gameplay.md](Gameplay.md), раздел 12). Каждая коробка трясётся на своём случайном интервале 2,5–3,5 секунды, поэтому звук звучит вразнобой.

**Новой кнопке** добавьте компонент `ButtonClickSound`. Кнопка в префабе, созданном через `IObjectResolver.Instantiate`, получит `UiSounds` сама, кнопка сцены — через `Auto Inject` объекта `Canvas`.

**Новый звук:** файл в `Audio/VFX`, `AudioAsset` на нём, поле в `GameSoundsConfig` и вызов в `GameplaySounds` (событие логики) или `UiSounds` (интерфейс).

Отправка на трассу, покупка, турбо и переработка пока беззвучны: файлов под них нет. Остаток задачи 3.5 — эти звуки, двигатели и музыка турбо.

---

## 8. Как дополнять сервис

Сервис намеренно маленький, в нём ровно два метода. Ниже частые доработки.

**Остановить или приглушить музыку.** Внутри уже есть приватный `StopMusic`, наружу он не выведен. Добавьте метод в интерфейс `Core/Audio/IAudioService.cs`, а в `AudioService` сделайте существующий метод публичным:

```csharp
void StopMusic();
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

**Звук в точке мира.** Сейчас все источники создаются в одном месте и звучат одинаково громко, откуда бы ни шли. Для трёхмерного звука добавьте перегрузку с позицией в интерфейс `Core/Audio/IAudioService.cs`:

```csharp
void PlayOneShot(AudioAsset asset, Vector3 position);
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

И её реализацию в `Infrastructure/Audio/AudioService.cs`:

```csharp
public void PlayOneShot(AudioAsset asset, Vector3 position)
{
    if (asset.Category != AudioCategories.SFX)
    {
        throw new InvalidOperationException(nameof(asset));
    }

    AudioSource source = CreateSource(asset);

    source.transform.position = position;
    source.spatialBlend = 1f;

    source.Play();

    Object.Destroy(source.gameObject, GetDestroyDelay(source));
}
```

*Пример условный: в вашем проекте имена, параметры и логика могут отличаться.*

**Плавный переход между мелодиями.** Сейчас смена музыки происходит резко. Плавность добавляется затуханием громкости старого источника и нарастанием нового, например через DOTween, который в проекте уже есть.

**Пул источников.** На каждый короткий звук создаётся и уничтожается объект. Для десятка звуков в секунду это нормально, но если звуки посыплются сотнями (много обломков, длинная серия попаданий), заведите пул готовых `AudioSource` и переиспользуйте их вместо создания новых.

**Ограничение на одинаковые звуки.** Когда в один кадр приходит десять одинаковых ударов, они складываются и получается неприятный хлопок. Лечится счётчиком: не запускать один и тот же ассет чаще, чем раз в 50-70 миллисекунд.

Общее правило при доработке: новый метод сначала появляется в `IAudioService`, потом в `AudioService`. Игровой код не должен знать про `AudioSource` и создавать его сам.

---

## 9. Как проверить, что всё работает

| Что проверяем | Как |
|---|---|
| Звук вообще играет | В Play Mode в иерархии появляется объект `AudioService`, а внутри него на время звучания дочерние объекты с именем клипа |
| Короткий звук не обрывается | Дочерний объект исчезает сразу после того, как звук доиграл, а не раньше |
| Музыка не перезапускается | Повторный вызов `PlayMusic` с тем же ассетом не сбивает мелодию на начало |
| Группы микшера | Откройте окно микшера в Play Mode: ползунки `Music` и `SFX` должны показывать движение уровня, когда звук играет |
| Звук при смене сцены | Объект `AudioService` остаётся в иерархии после загрузки другой сцены |

Если объект `AudioService` в иерархии не появился, значит сервис ещё ни разу не запросили: контейнер создаёт его лениво, при первом обращении.
