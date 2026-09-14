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
}
