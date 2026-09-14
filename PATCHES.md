# Patches in this fork

Этот форк — **временная совместимостная заплатка**, а не самостоятельный проект.
Он существует ровно до того момента, когда апстрим (`wiz0u/WTelegramClient`)
догонит текущий слой Telegram. После этого патч нужно удалить (см. «Как снять патч»).

Лицензия и авторство апстрима не меняются: см. `LICENSE.txt` (MIT). Все изменения
перечислены ниже — в соответствии с условием MIT об указании модификаций.

## patch229 — реструктуризация клавиатур ботов (`keyboardButton` + `ButtonType`)

**Файл:** `src/TL.Schema.Patch229.cs` (полностью аддитивный, ничего из
сгенерированной схемы не правится).

**Проблема.** Telegram изменил схему: вместо отдельного конструктора на каждый вид
кнопки (`keyboardButtonRequestPhone`, `keyboardButtonGame`, …) теперь один
`keyboardButton` с полем `type:ButtonType`. Все обычные reply-клавиатуры приходят
на провод в новом виде, а библиотека знает только старый — и падает с

```
WTException: Cannot find type for ctor #2f67a72f
```

на любом ответе, где встречается такая кнопка (в частности на
`messages.dialogsSlice`, то есть теряется весь список диалогов).

**Почему обычная перегенерация не помогает.** Опубликованная схема
`core.telegram.org/schema/json` этот вариант ещё не содержит: там `keyboardButton`
объявлен под другим идентификатором и без поля `type`. Источник схемы и живой
сервер в момент раскатки расходятся.

**Откуда взяты идентификаторы.** Из авторитетной TL-схемы проекта Telegram —
`tdlib/td`, файл `td/generate/scheme/telegram_api.tl`:

```
keyboardButton#2f67a72f flags:# style:flags.10?KeyboardButtonStyle text:string type:ButtonType = KeyboardButton;
buttonTypeDefault#c9dd90e9 = ButtonType;
buttonTypeRequestPhone#df3d36f9 = ButtonType;
buttonTypeRequestGeoLocation#9beee140 = ButtonType;
buttonTypeRequestPoll#aacfff84 flags:# quiz:flags.0?Bool = ButtonType;
buttonTypeRequestPeer#4f58a237 flags:# button_id:int peer_type:RequestPeerType max_quantity:int = ButtonType;
inputButtonTypeRequestPeer#3fe268fe flags:# name_requested:flags.0?true username_requested:flags.1?true
    photo_requested:flags.2?true button_id:int peer_type:RequestPeerType max_quantity:int = ButtonType;
buttonTypeSimpleWebView#c01a597a url:string = ButtonType;
```

**Что добавлено.** Классы `ButtonType` (+7 реализаций) и `KeyboardButtonWithType`
(новый `keyboardButton#2f67a72f`). Старый `KeyboardButton#7D170CFF` не тронут — он
остаётся базой для legacy-наследников (`KeyboardButtonWebView`,
`KeyboardButtonRequestPeer`, …), которым нельзя добавлять обязательное поле `type`.

Название `KeyboardButtonWithType`, а не `KeyboardButton`, выбрано намеренно:
когда апстрим догонит схему, у него будет ровно один `keyboardButton` — уже с
полем `type`, и наши классы станут не нужны целиком.

**Особенность TL, на которой легко ошибиться:** `flags.N?true` — это бит без
payload, а `flags.N?Bool` — четыре байта. Поэтому в `inputButtonTypeRequestPeer`
флаги оформлены битами enum (как `creator = 0x1` в `RequestPeerTypeChat`), а не
bool-полями: bool-поле сдвинуло бы разбор.

## Проверка

Тесты на разбор данных этого слоя живут в потребителе — в проекте
`TgGate.WTelegram.Tests` (xunit, «фикстуры как с провода»), а не здесь: они
проверяют наш реальный путь использования, а не абстрактный слой.

В форке CI только собирает и пакует — это доказывает, что патч компилируется.

## Как снять патч

1. Убедиться, что в апстриме появился слой со `keyboardButton#2f67a72f`
   (или что в его схеме есть `ButtonType`).
2. Удалить `src/TL.Schema.Patch229.cs`, переключиться на версию апстрима.
3. В потребителе заменить ссылку на пакет форка обратно на `WTelegramClient`
   из nuget.org.
