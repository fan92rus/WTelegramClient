# Patches in this fork

Этот форк — **временная совместимостная заплатка**, а не самостоятельный проект.
Он существует ровно до того момента, когда апстрим (`wiz0u/WTelegramClient`)
догонит текущий слой Telegram. После этого патч нужно удалить (см. «Как снять патч»).

Лицензия и авторство апстрима не меняются: см. `LICENSE.txt` (MIT). Все изменения
перечислены ниже — в соответствии с условием MIT об указании модификаций.

## patch229 — реструктуризация клавиатур ботов (reply + inline)

**Файл:** `src/TL.Schema.Patch229.cs` (полностью аддитивный, ничего из
сгенерированной схемы не правится).

**Проблема.** Telegram изменил схему клавиатур: вместо отдельного конструктора на
каждый вид кнопки (`keyboardButtonRequestPhone`, `keyboardButtonGame`, …) теперь один
`keyboardButton` с полем `type:ButtonType`; inline-клавиатуры так же получили
собственную ветку типов (`KeyboardInlineButton` + `type:InlineButtonType`) и новый
идентификатор `replyInlineMarkup`. Библиотека знает только старый вид — и падает с

```
WTException: Cannot find type for ctor #2f67a72f   // reply-кнопка
WTException: Cannot find type for ctor #b2b15770   // replyInlineMarkup
```

на любом ответе, где встречается такая клавиатура. Оба случая найдены ЖИВОЙ
проверкой на настоящем аккаунте, причём именно на `messages.dialogsSlice`: одна
кнопка в одном сообщении уносит весь список диалогов, потому что в списке
диалогов лежит последнее сообщение каждого диалога.

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

replyInlineMarkup#b2b15770 flags:# force_reply:flags.5?true rows:Vector<KeyboardInlineButtonRow> = ReplyMarkup;
keyboardInlineButtonRow#19420af6 buttons:Vector<KeyboardInlineButton> = KeyboardInlineButtonRow;
keyboardInlineButton#11c1a322 flags:# style:flags.10?KeyboardButtonStyle text:string type:InlineButtonType = KeyboardInlineButton;
inlineButtonTypeUrl#eca4f8d4 url:string = InlineButtonType;
inlineButtonTypeUrlAuth#bfd02da2 flags:# fwd_text:flags.0?string url:string button_id:int = InlineButtonType;
inputInlineButtonTypeUrlAuth#9961bcb4 flags:# request_write_access:flags.0?true fwd_text:flags.1?string url:string bot:flags.2?InputUser = InlineButtonType;
inlineButtonTypeWebView#3bcab5b4 url:string = InlineButtonType;
inlineButtonTypeCallback#2955bc38 flags:# requires_password:flags.0?true data:bytes = InlineButtonType;
inlineButtonTypeGame#5cd3709d = InlineButtonType;
inlineButtonTypeBuy#48bad7a5 = InlineButtonType;
inlineButtonTypeSwitchInline#93773ff5 flags:# same_peer:flags.0?true query:string peer_types:flags.1?Vector<InlineQueryPeerType> = InlineButtonType;
inlineButtonTypeUserProfile#3fa33fcf user_id:long = InlineButtonType;
inputInlineButtonTypeUserProfile#53f3ce5a user_id:InputUser = InlineButtonType;
inlineButtonTypeCopy#b41d3272 copy_text:string = InlineButtonType;
inlineButtonTypeDisabled#a438619d = InlineButtonType;
```

**Что добавлено.**

*Reply-клавиатуры:* `ButtonType` (+7 реализаций) и `KeyboardButtonWithType`
(новый `keyboardButton#2f67a72f`). Старый `KeyboardButton#7D170CFF` не тронут — он
остаётся базой для legacy-наследников (`KeyboardButtonWebView`,
`KeyboardButtonRequestPeer`, …), которым нельзя добавлять обязательное поле `type`.

*Inline-клавиатуры:* `InlineButtonType` (+12 реализаций по списку выше),
`KeyboardInlineButton` + `KeyboardInlineButtonWithType#11C1A322`,
`KeyboardInlineButtonRow#19420AF6` и `ReplyInlineMarkupWithRows#B2B15770`.
Старый `ReplyInlineMarkup#48A30254` остаётся: он приходит на клавиатуры, уже
лежащие на сервере в старом виде, поэтому новый вариант добавлен ОТДЕЛЬНЫМ классом,
а не правкой сгенерированного. Потребителю, который разбирает `ReplyMarkup`, надо
учитывать оба варианта inline-разметки.

*Про `InlineQueryPeerType` — здесь переопределять НЕЛЬЗЯ:* в 4.4.8 это `enum`,
значения которого в точности равны ctor-id новых вариантов (`SameBotPM = 0x3081ED9D`
и т.д.), а все варианты на проводе без полей — то есть enum разбирает их корректно.
Дубли классов с теми же ctor-id роняют Roslyn-генератор схемы
(`CS8785: NullReferenceException`), после чего компилятор заваливает проект
сотнями `CS0535 «не реализует IObject.WriteTL»` — это симптом, а не причина.

Названия `KeyboardButtonWithType` / `KeyboardInlineButtonWithType` (а не
`KeyboardButton`) выбраны намеренно: когда апстрим догонит схему, у него будет ровно
один конструктор каждого вида — уже с полем `type`, и наши классы станут не нужны
целиком.

**Особенность TL, на которой легко ошибиться:** `flags.N?true` — это бит без
payload, а `flags.N?Bool` — четыре байта. Поэтому в `inputButtonTypeRequestPeer`
флаги оформлены битами enum (как `creator = 0x1` в `RequestPeerTypeChat`), а не
bool-полями: bool-поле сдвинуло бы разбор.

## patch229 (продолжение) — остальное расхождение слоя (15 типов)

**Файл:** `src/TL.Schema.Patch229.Rest.cs` — сгенерирован `tools/gen-patch229.py`
из живой схемы. Править руками нельзя: при новой дыре — перегенерировать.

**Зачем генератор, а не руки.** Второй живой прогон показал, что клавиатурами дело
не ограничивается: сервер отдаёт в новом формате всё, что рендерит сам — например
превью ссылок пришло как `pageBlockBlockquote#66d1670b` внутри `Page` внутри
`WebPage` внутри сообщения. У каждого такого конструктора важны точные номера
битов флагов и порядок полей; ошибка здесь не падает сразу, а молча сдвигает
разбор всего ответа. Поэтому — скрипт.

**Метрика покрытия (проверяемая).** Расхождение считалось автоматически: ctor-id
живой схемы против (классы + таблица `TL.Table.cs` + значения enum'ов).
Было: из 1663 типов слоя не покрыто 21; стало — 5, и все пять — осознанные
исключения (ниже). Проверка одной командой описана в шапке скрипта.

**Что вошло:** варианты `PageBlock` (`pageBlockBlockquote`, `pageBlockButtonRow`,
`pageBlockDocument`) с `PageButton`/`TextButton`/`RichButtonStyle`, варианты
`MessageAction` и `SendMessageAction` (в т.ч. новые draft-actions — они приходят в
апдейтах), legacy-локации файлов (`inputPeerPhotoFileLocationLegacy`,
`inputStickerSetThumbLegacy`, `inputPhotoLegacyFileLocation`) и
`inputInvoiceStarGiftResale`.

**Что осознанно НЕ вошло** (наш клиент эти методы не вызывает, а их база в 4.4.8
объявлена `sealed`, то есть доподключение невозможно без полной регенерации):
`ephemeralMessage`, `updateEphemeralBotCallbackQuery`, `ephemeral.welcomeMessages`,
`ephemeral.welcomeMessagesNotModified`, `auth.firebasePnvIntent`.

**Тонкости, на которых спотыкаешься:**
- в 4.4.8 база типа может называться с суффиксом `Base` (`InputFileLocationBase`),
  если имя занято конкретным конструктором — генератор это учитывает;
- в `.tl` идентификаторы бывают записаны семью цифрами (`richButtonStyle#3c610bd`),
  а до первого маркера секции тоже лежат типы — обе тонкости учтены;
- имена, уже занятые схемой, получают суффикс `V229`; поля-ключевые слова C#
  экранируются (`@out`).

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
