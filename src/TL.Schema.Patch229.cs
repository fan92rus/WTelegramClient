using System;
using System.ComponentModel;

// ─────────────────────────────────────────────────────────────────────────────
//  Патч слоя 229: реструктуризация клавиатур ботов (keyboardButton + ButtonType)
//
//  Telegram изменил схему: вместо отдельных конструкторов на каждый вид кнопки
//  (keyboardButtonRequestPhone, keyboardButtonGame, …) теперь один
//  keyboardButton с полем type:ButtonType.
//
//  Провод (авторитетно — tdlib/td, td/generate/scheme/telegram_api.tl):
//     keyboardButton#2f67a72f flags:# style:flags.10?KeyboardButtonStyle
//                            text:string type:ButtonType = KeyboardButton;
//     buttonTypeDefault#c9dd90e9 = ButtonType;
//     buttonTypeRequestPhone#df3d36f9 = ButtonType;
//     buttonTypeRequestGeoLocation#9beee140 = ButtonType;
//     buttonTypeRequestPoll#aacfff84 flags:# quiz:flags.0?Bool = ButtonType;
//     buttonTypeRequestPeer#4f58a237 flags:# button_id:int
//                            peer_type:RequestPeerType max_quantity:int = ButtonType;
//     inputButtonTypeRequestPeer#3fe268fe flags:# name_requested:flags.0?true
//                            username_requested:flags.1?true photo_requested:flags.2?true
//                            button_id:int peer_type:RequestPeerType max_quantity:int = ButtonType;
//     buttonTypeSimpleWebView#c01a597a url:string = ButtonType;
//
//  Опубликованная документация (core.telegram.org/schema/json) этот вариант ещё не
//  содержит — поэтому обычная перегенерация из неё проблему не решает.
//
//  Классы ниже — аддитивные: ничего из сгенерированной схемы не меняется.
//  Roslyn-генератор (generator/MTProtoGenerator.cs) подхватывает любой класс с
//  [TLDef] из сборки и сам строит таблицы диспетчеризации и ReadTL/WriteTL.
//
//  Имя KeyboardButtonWithType (а не KeyboardButton) выбрано намеренно: старый
//  KeyboardButton#7D170CFF остаётся базой для legacy-наследников
//  (KeyboardButtonWebView, KeyboardButtonRequestPeer, …), и добавлять им
//  обязательное поле type нельзя.
// ─────────────────────────────────────────────────────────────────────────────
namespace TL
{
	/// <summary>Тип кнопки бота (слой 229). Базовый конструктор — <see cref="KeyboardButtonWithType"/></summary>
	public abstract partial class ButtonType : IObject { }

	/// <summary>Обычная кнопка		<para>See <a href="https://corefork.telegram.org/constructor/buttonTypeDefault"/></para></summary>
	[TLDef(0xC9DD90E9)]
	public sealed partial class ButtonTypeDefault : ButtonType
	{
	}

	/// <summary>Кнопка запроса телефона пользователя		<para>See <a href="https://corefork.telegram.org/constructor/buttonTypeRequestPhone"/></para></summary>
	[TLDef(0xDF3D36F9)]
	public sealed partial class ButtonTypeRequestPhone : ButtonType
	{
	}

	/// <summary>Кнопка запроса геолокации пользователя		<para>See <a href="https://corefork.telegram.org/constructor/buttonTypeRequestGeoLocation"/></para></summary>
	[TLDef(0x9BEEE140)]
	public sealed partial class ButtonTypeRequestGeoLocation : ButtonType
	{
	}

	/// <summary>Кнопка запроса опроса у пользователя		<para>See <a href="https://corefork.telegram.org/constructor/buttonTypeRequestPoll"/></para></summary>
	[TLDef(0xAACFFF84)]
	public sealed partial class ButtonTypeRequestPoll : ButtonType
	{
		/// <summary>Extra bits of information, use <c>flags.HasFlag(...)</c> to test for those</summary>
		public Flags flags;
		/// <summary>If set, only quiz polls can be sent</summary>
		[IfFlag(0)] public bool quiz;

		[Flags] public enum Flags : uint
		{
			/// <summary>Field <see cref="quiz"/> is set</summary>
			has_quiz = 0x1,
		}
	}

	/// <summary>Кнопка запроса пира у пользователя		<para>See <a href="https://corefork.telegram.org/constructor/buttonTypeRequestPeer"/></para></summary>
	[TLDef(0x4F58A237)]
	public sealed partial class ButtonTypeRequestPeer : ButtonType
	{
		/// <summary>Extra bits of information, use <c>flags.HasFlag(...)</c> to test for those</summary>
		public Flags flags;
		/// <summary>Button id, to be passed back in the resulting service message</summary>
		public int button_id;
		/// <summary>Peer type to request</summary>
		public RequestPeerType peer_type;
		/// <summary>Maximum number of peers to be returned</summary>
		public int max_quantity;

		[Flags] public enum Flags : uint
		{
		}
	}

	/// <summary>Кнопка запроса пира у пользователя (на отправку)		<para>See <a href="https://corefork.telegram.org/constructor/inputButtonTypeRequestPeer"/></para></summary>
	[TLDef(0x3FE268FE)]
	public sealed partial class InputButtonTypeRequestPeer : ButtonType
	{
		/// <summary>Extra bits of information, use <c>flags.HasFlag(...)</c> to test for those</summary>
		public Flags flags;
		/// <summary>Button id, to be passed back in the resulting service message</summary>
		public int button_id;
		/// <summary>Peer type to request</summary>
		public RequestPeerType peer_type;
		/// <summary>Maximum number of peers to be returned</summary>
		public int max_quantity;

		// Внимание: в TL это флаги вида flags.N?true — на проводе у них НЕТ payload,
		// поэтому они represented только битами enum (как creator=0x1 в RequestPeerTypeChat),
		// а не bool-полями: bool-поле прочитало бы лишние 4 байта и сдвинуло разбор.
		[Flags] public enum Flags : uint
		{
			/// <summary>Whether the name (first + last) of the peer must be requested</summary>
			name_requested = 0x1,
			/// <summary>Whether the username of the peer must be requested</summary>
			username_requested = 0x2,
			/// <summary>Whether the profile photo of the peer must be requested</summary>
			photo_requested = 0x4,
		}
	}

	/// <summary>Кнопка открытия простого веб-приложения		<para>See <a href="https://corefork.telegram.org/constructor/buttonTypeSimpleWebView"/></para></summary>
	[TLDef(0xC01A597A)]
	public sealed partial class ButtonTypeSimpleWebView : ButtonType
	{
		/// <summary>URL of the web app</summary>
		public string url;
	}

	/// <summary>Кнопка клавиатуры бота (слой 229)		<para>See <a href="https://corefork.telegram.org/constructor/keyboardButton"/></para></summary>
	[TLDef(0x2F67A72F)]
	public partial class KeyboardButtonWithType : KeyboardButtonBase
	{
		/// <summary>Extra bits of information, use <c>flags.HasFlag(...)</c> to test for those</summary>
		public Flags flags;
		/// <summary>Button style, see <a href="https://corefork.telegram.org/api/bots/buttons#button-styles">here »</a> for more info on button styles.</summary>
		[IfFlag(10)] public KeyboardButtonStyle style;
		/// <summary>Button text</summary>
		public string text;
		/// <summary>Button type</summary>
		public ButtonType type;

		[Flags] public enum Flags : uint
		{
			/// <summary>Field <see cref="style"/> has a value</summary>
			has_style = 0x400,
		}

		/// <summary>Button style, see <a href="https://corefork.telegram.org/api/bots/buttons#button-styles">here »</a> for more info on button styles.</summary>
		public override KeyboardButtonStyle Style => style;
		/// <summary>Button text</summary>
		public override string Text => text;
	}

	// ─────────────────────────────────────────────────────────────────────────
	//  Inline-клавиатуры: тот же слой переработал и их (отдельная ветка типов).
	//
	//  Провод (tdlib/td, td/generate/scheme/telegram_api.tl):
	//     replyInlineMarkup#b2b15770 flags:# force_reply:flags.5?true
	//                            rows:Vector<KeyboardInlineButtonRow> = ReplyMarkup;
	//     keyboardInlineButtonRow#19420af6 buttons:Vector<KeyboardInlineButton> = KeyboardInlineButtonRow;
	//     keyboardInlineButton#11c1a322 flags:# style:flags.10?KeyboardButtonStyle
	//                            text:string type:InlineButtonType = KeyboardInlineButton;
	//     inlineButtonType*#... = InlineButtonType;
	//     inlineQueryPeerType*#... = InlineQueryPeerType;
	//
	//  Важно: старый replyInlineMarkup#48A30254 остаётся в схеме — он приходит на
	//  старые клавиатуры, уже лежащие на сервере. Поэтому новый вариант добавлен
	//  ОТДЕЛЬНЫМ классом (ReplyInlineMarkupWithRows), а не правкой сгенерированного.
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>Тип inline-кнопки бота (слой 229). Базовый конструктор — <see cref="KeyboardInlineButtonWithType"/></summary>
	public abstract partial class InlineButtonType : IObject { }

	/// <summary>Обычная кнопка со ссылкой		<para>See <a href="https://corefork.telegram.org/constructor/inlineButtonTypeUrl"/></para></summary>
	[TLDef(0xECA4F8D4)]
	public sealed partial class InlineButtonTypeUrl : InlineButtonType
	{
		/// <summary>URL</summary>
		public string url;
	}

	/// <summary>Кнопка со ссылкой, требующей подтверждения авторизации		<para>See <a href="https://corefork.telegram.org/constructor/inlineButtonTypeUrlAuth"/></para></summary>
	[TLDef(0xBFD02DA2)]
	public sealed partial class InlineButtonTypeUrlAuth : InlineButtonType
	{
		/// <summary>Extra bits of information, use <c>flags.HasFlag(...)</c> to test for those</summary>
		public Flags flags;
		/// <summary>New text of the button in forwarded messages</summary>
		[IfFlag(0)] public string fwd_text;
		/// <summary>URL</summary>
		public string url;
		/// <summary>Button id, to be passed back in the resulting service message</summary>
		public int button_id;

		[Flags] public enum Flags : uint
		{
			/// <summary>Field <see cref="fwd_text"/> has a value</summary>
			has_fwd_text = 0x1,
		}
	}

	/// <summary>Кнопка со ссылкой, требующей подтверждения авторизации (на отправку)		<para>See <a href="https://corefork.telegram.org/constructor/inputInlineButtonTypeUrlAuth"/></para></summary>
	[TLDef(0x9961BCB4)]
	public sealed partial class InputInlineButtonTypeUrlAuth : InlineButtonType
	{
		/// <summary>Extra bits of information, use <c>flags.HasFlag(...)</c> to test for those</summary>
		public Flags flags;
		/// <summary>New text of the button in forwarded messages</summary>
		[IfFlag(1)] public string fwd_text;
		/// <summary>URL</summary>
		public string url;
		/// <summary>Bot that will be used to handle the authorization</summary>
		[IfFlag(2)] public InputUser bot;

		// Внимание: request_write_access в TL объявлен как flags.0?true — у него НЕТ payload,
		// поэтому он живёт только битом enum (bool-поле съело бы лишние 4 байта).
		[Flags] public enum Flags : uint
		{
			/// <summary>Whether to request write access to the user account</summary>
			request_write_access = 0x1,
			/// <summary>Field <see cref="fwd_text"/> has a value</summary>
			has_fwd_text = 0x2,
			/// <summary>Field <see cref="bot"/> has a value</summary>
			has_bot = 0x4,
		}
	}

	/// <summary>Кнопка открытия веб-приложения		<para>See <a href="https://corefork.telegram.org/constructor/inlineButtonTypeWebView"/></para></summary>
	[TLDef(0x3BCAB5B4)]
	public sealed partial class InlineButtonTypeWebView : InlineButtonType
	{
		/// <summary>URL of the web app</summary>
		public string url;
	}

	/// <summary>Кнопка с callback-данными		<para>See <a href="https://corefork.telegram.org/constructor/inlineButtonTypeCallback"/></para></summary>
	[TLDef(0x2955BC38)]
	public sealed partial class InlineButtonTypeCallback : InlineButtonType
	{
		/// <summary>Extra bits of information, use <c>flags.HasFlag(...)</c> to test for those</summary>
		public Flags flags;
		/// <summary>Callback data</summary>
		public byte[] data;

		[Flags] public enum Flags : uint
		{
			/// <summary>Whether the callback requires a password</summary>
			requires_password = 0x1,
		}
	}

	/// <summary>Кнопка запуска игры		<para>See <a href="https://corefork.telegram.org/constructor/inlineButtonTypeGame"/></para></summary>
	[TLDef(0x5CD3709D)]
	public sealed partial class InlineButtonTypeGame : InlineButtonType
	{
	}

	/// <summary>Кнопка покупки		<para>See <a href="https://corefork.telegram.org/constructor/inlineButtonTypeBuy"/></para></summary>
	[TLDef(0x48BAD7A5)]
	public sealed partial class InlineButtonTypeBuy : InlineButtonType
	{
	}

	/// <summary>Кнопка переключения в inline-режим		<para>See <a href="https://corefork.telegram.org/constructor/inlineButtonTypeSwitchInline"/></para></summary>
	[TLDef(0x93773FF5)]
	public sealed partial class InlineButtonTypeSwitchInline : InlineButtonType
	{
		/// <summary>Extra bits of information, use <c>flags.HasFlag(...)</c> to test for those</summary>
		public Flags flags;
		/// <summary>Inline query</summary>
		public string query;
		/// <summary>Peer types the inline query can be sent to</summary>
		[IfFlag(1)] public InlineQueryPeerType[] peer_types;

		[Flags] public enum Flags : uint
		{
			/// <summary>Whether the query must be sent to the same peer</summary>
			same_peer = 0x1,
			/// <summary>Field <see cref="peer_types"/> has a value</summary>
			has_peer_types = 0x2,
		}
	}

	/// <summary>Кнопка открытия профиля пользователя		<para>See <a href="https://corefork.telegram.org/constructor/inlineButtonTypeUserProfile"/></para></summary>
	[TLDef(0x3FA33FCF)]
	public sealed partial class InlineButtonTypeUserProfile : InlineButtonType
	{
		/// <summary>User id</summary>
		public long user_id;
	}

	/// <summary>Кнопка открытия профиля пользователя (на отправку)		<para>See <a href="https://corefork.telegram.org/constructor/inputInlineButtonTypeUserProfile"/></para></summary>
	[TLDef(0x53F3CE5A)]
	public sealed partial class InputInlineButtonTypeUserProfile : InlineButtonType
	{
		/// <summary>User</summary>
		public InputUser user_id;
	}

	/// <summary>Кнопка копирования текста		<para>See <a href="https://corefork.telegram.org/constructor/inlineButtonTypeCopy"/></para></summary>
	[TLDef(0xB41D3272)]
	public sealed partial class InlineButtonTypeCopy : InlineButtonType
	{
		/// <summary>The text that will be copied to the clipboard</summary>
		public string copy_text;
	}

	/// <summary>Неактивная кнопка		<para>See <a href="https://corefork.telegram.org/constructor/inlineButtonTypeDisabled"/></para></summary>
	[TLDef(0xA438619D)]
	public sealed partial class InlineButtonTypeDisabled : InlineButtonType
	{
	}

	// InlineQueryPeerType намеренно НЕ переопределяется: в 4.4.8 это enum, значения
	// которого в точности равны ctor-id новых вариантов (SameBotPM = 0x3081ED9D и т.д.),
	// а все варианты на проводе без полей — значит enum разбирает их корректно.
	// Дубли с теми же ctor-id ломают Roslyn-генератор (NullReferenceException).

	/// <summary>Строка inline-клавиатуры		<para>See <a href="https://corefork.telegram.org/constructor/keyboardInlineButtonRow"/></para></summary>
	[TLDef(0x19420AF6)]
	public sealed partial class KeyboardInlineButtonRow : IObject
	{
		/// <summary>Inline keyboard buttons</summary>
		public KeyboardInlineButton[] buttons;
	}

	/// <summary>Inline-кнопка бота		<para>See <a href="https://corefork.telegram.org/type/KeyboardInlineButton"/></para></summary>
	public abstract partial class KeyboardInlineButton : IObject
	{
		/// <summary>Button style</summary>
		public virtual KeyboardButtonStyle Style => default;
		/// <summary>Button text</summary>
		public virtual string Text => default;
	}

	/// <summary>Inline-кнопка бота (слой 229)		<para>See <a href="https://corefork.telegram.org/constructor/keyboardInlineButton"/></para></summary>
	[TLDef(0x11C1A322)]
	public partial class KeyboardInlineButtonWithType : KeyboardInlineButton
	{
		/// <summary>Extra bits of information, use <c>flags.HasFlag(...)</c> to test for those</summary>
		public Flags flags;
		/// <summary>Button style</summary>
		[IfFlag(10)] public KeyboardButtonStyle style;
		/// <summary>Button text</summary>
		public string text;
		/// <summary>Button type</summary>
		public InlineButtonType type;

		[Flags] public enum Flags : uint
		{
			/// <summary>Field <see cref="style"/> has a value</summary>
			has_style = 0x400,
		}

		/// <summary>Button style</summary>
		public override KeyboardButtonStyle Style => style;
		/// <summary>Button text</summary>
		public override string Text => text;
	}

	/// <summary>Inline-клавиатура (слой 229)		<para>See <a href="https://corefork.telegram.org/constructor/replyInlineMarkup"/></para></summary>
	[TLDef(0xB2B15770)]
	public sealed partial class ReplyInlineMarkupWithRows : ReplyMarkup
	{
		/// <summary>Extra bits of information, use <c>flags.HasFlag(...)</c> to test for those</summary>
		public Flags flags;
		/// <summary>Inline keyboard rows</summary>
		public KeyboardInlineButtonRow[] rows;

		[Flags] public enum Flags : uint
		{
			/// <summary>Whether the user must send a reply</summary>
			force_reply = 0x20,
		}
	}
}
