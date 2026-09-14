"""Генерирует src/TL.Schema.Patch229.Rest.cs — аддитивные классы для типов слоя,
которых нет в опубликованной схеме WTelegramClient, но которые сервер присылает.

Почему скрипт, а не руками: у каждого конструктора важны ТОЧНЫЕ номера битов флагов
(flags.N) и порядок полей. Ручная правка такого объёма почти гарантированно contains
ошибку, а ошибка здесь не падает сразу — она молча сдвигает разбор всего ответа.

Запуск (Windows, полный python):
    python tools/gen-patch229.py <живой-тл-файл> <каталог-форка>

Где взять «живой» .tl: тот, чьи id приходят с провода. Источник — авторитетная схема
Telegram (tdlib/td, td/generate/scheme/telegram_api.tl); при расхождении версий нужен
именно тот, на который свалился наш live-тест (WTException: Cannot find type for
ctor #xxxxxxxx — там напечатан id).
"""
import pathlib
import re
import sys

# ── разбор .tl ──────────────────────────────────────────────────────────────

TYPE_ALIASES = {"int": "int", "long": "long", "double": "double", "string": "string",
                "bytes": "byte[]", "Bool": "bool", "true": "bool"}

CS_KEYWORDS = set(
    "abstract as base bool break byte case catch char checked class const continue decimal "
    "default delegate do double else enum event explicit extern false finally fixed float for "
    "foreach goto if implicit in int interface internal is lock long namespace new null object "
    "operator out override params private protected public readonly ref return sbyte sealed "
    "short sizeof stackalloc static string struct switch this throw true try typeof uint ulong "
    "unchecked unsafe ushort using virtual void volatile while".split())


def cs_ident(name: str) -> str:
    """Имя поля/бита: ключевые слова C# экранируем (@out) — TL прислал именно 'out'."""
    return "@" + name if name in CS_KEYWORDS else name


def cs_name(tl_name: str) -> str:
    parts = re.split(r"[._]", tl_name)
    return "_".join(p[:1].upper() + p[1:] for p in parts)


# Фичи, которые наш клиент НЕ запрашивает, а их база в 4.4.8 объявлена sealed:
# наследоваться нельзя, и полный ре-ген схемы тут не нужен. Появятся в деле —
# придётся перегенерировать схему целиком, а не допатчивать (см. PATCHES.md).
SKIP_TL = {
    "ephemeralMessage",
    "updateEphemeralBotCallbackQuery",
    "ephemeral.welcomeMessages",
    "ephemeral.welcomeMessagesNotModified",
    "auth.firebasePnvIntent",
}


def parse_tl(path: pathlib.Path):
    """-> список (tl_name, ctor, params, base) для секции типов.

    Секции: в telegram_api.tl типы и функции разделены маркерами, но перед первым
    маркером тоже лежат типы — считаем прелюдию секцией типов."""
    types, in_types = [], True
    for line in path.read_text(encoding="utf-8", errors="ignore").splitlines():
        stripped = line.strip()
        if stripped.startswith("---"):
            in_types = stripped == "---types---"
            continue
        if not in_types or not stripped or stripped.startswith("//") or "{" in stripped:
            continue
        match = re.match(r"^([A-Za-z_][A-Za-z0-9_.]*(?:#|\.))([0-9a-f]{1,8})\s*(.*?)\s*=\s*([A-Za-z_][A-Za-z0-9_.]*);?$", stripped)
        if match:
            types.append((match.group(1).rstrip("#"), int(match.group(2), 16), match.group(3), match.group(4)))
    return types


GENERATED = "TL.Schema.Patch229.Rest.cs"   # свой же вывод не считаем «уже известным»


def known_ids(src_dir: pathlib.Path):
    ids = set()
    for path in src_dir.glob("*.cs"):
        if path.name == GENERATED:
            continue
        text = path.read_text(encoding="utf-8", errors="ignore")
        ids.update(int(m, 16) for m in re.findall(r"TLDef\(0x([0-9A-Fa-f]{1,8})", text))
        ids.update(int(m, 16) for m in re.findall(r"\[0x([0-9A-Fa-f]{1,8})\]\s*=", text))
        for body in re.findall(r"public enum \w+ : uint\s*\{(.*?)\}", text, re.S):
            ids.update(int(m, 16) for m in re.findall(r"=\s*0x([0-9A-Fa-f]{1,8})", body))
    return ids


def known_type_names(src_dir: pathlib.Path):
    names = set()
    for path in src_dir.glob("*.cs"):
        if path.name == GENERATED:
            continue
        text = path.read_text(encoding="utf-8", errors="ignore")
        names.update(re.findall(r"\b(?:class|enum)\s+(\w+)", text))
    return names


def sealed_type_names(src_dir: pathlib.Path):
    """Классы схемы, объявленные sealed: от них наследоваться нельзя — такие типы
    слоя мы не патчим (это фичи, которые наш клиент не запрашивает)."""
    sealed = set()
    for path in src_dir.glob("*.cs"):
        if path.name == GENERATED:
            continue
        text = path.read_text(encoding="utf-8", errors="ignore")
        sealed.update(re.findall(r"sealed partial class (\w+)", text))
    return sealed


# ── генерация C# ────────────────────────────────────────────────────────────

def cs_type(tl_type: str, names: set[str]) -> str:
    if tl_type in TYPE_ALIASES:
        return TYPE_ALIASES[tl_type]
    vector = re.match(r"^[Vv]ector<(.+)>$", tl_type)
    if vector:
        return cs_type(vector.group(1), names) + "[]"
    return cs_name(tl_type)


def resolve_base(base: str, names: set[str], sealed: set[str]) -> str:
    """База типа. У апстрима есть конвенция «Base»-суффикса: если имя базы занято
    конкретным конструктором (и потому sealed), настоящая база — <Имя>Base."""
    cs = cs_name(base)
    if cs in sealed and cs + "Base" in names and cs + "Base" not in sealed:
        return cs + "Base"
    if cs not in names and cs + "Base" in names and cs + "Base" not in sealed:
        return cs + "Base"
    return cs


def render(tl_name, ctor, params, base, names, sealed):
    name = cs_name(tl_name)
    if name in names:                      # имя занято сгенерированной схемой
        name += "V229"
    base_cs = resolve_base(base, names, sealed)
    if base_cs == name:                    # конструктор и есть сам базовый тип
        base_cs = "IObject"
    fields, bits = [], []
    for token in params.split():
        if token == "flags:#":
            fields.append(("flags", "Flags", None, True))
            continue
        match = re.match(r"^(\w+):flags\.(\d+)\?(\??[A-Za-z_][\w.<>]*)$", token)
        if match:
            field, bit, typ = match.group(1), int(match.group(2)), match.group(3).lstrip("?")
            if typ == "true":                                   # бит без payload
                bits.append((field, None, bit))
            elif typ == "Bool":                                 # bool + бит
                bits.append(("has_" + field, field, bit))
                fields.append((field, "bool", bit, False))
            else:
                bits.append(("has_" + field, field, bit))
                fields.append((field, cs_type(typ, names), bit, False))
            continue
        match = re.match(r"^(\w+):(\??[A-Za-z_][\w.<>]*)$", token)
        if match:
            fields.append((match.group(1), cs_type(match.group(2).lstrip("?"), names), None, False))
            continue
        raise ValueError(f"не разобран токен '{token}' у {tl_name}")

    out = [f"\t/// <summary>Слой 229: {tl_name} </summary>",
           f"\t[TLDef(0x{ctor:08X})]",
           f"\tpublic sealed partial class {name} : {base_cs}",
           "\t{"]
    if bits:
        out.append("\t\t/// <summary>Extra bits of information, use <c>flags.HasFlag(...)</c> to test for those</summary>")
        out.append("\t\tpublic Flags flags;")
    for field, typ, bit, is_flags in fields:
        if is_flags:
            continue
        if bit is not None:
            out.append(f"\t\t[IfFlag({bit})] public {typ} {cs_ident(field)};")
        else:
            out.append(f"\t\tpublic {typ} {cs_ident(field)};")
    if bits:
        out.append("")
        out.append("\t\t[Flags] public enum Flags : uint")
        out.append("\t\t{")
        for enum_name, _, bit in bits:
            out.append(f"\t\t\t{cs_ident(enum_name)} = 0x{1 << bit:X},")
        out.append("\t\t}")
    out.append("\t}")
    return name, base_cs, "\n".join(out)


def main():
    tl_path, fork_dir = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
    src = fork_dir / "src"
    ids, names = known_ids(src), known_type_names(src)
    sealed = sealed_type_names(src)

    def extendable(tl_name, base):
        """Патчим, если база — сам конструктор (синглтон-тип) либо не sealed."""
        own = cs_name(tl_name)
        base_cs = resolve_base(base, names, sealed)
        return base_cs == own or base_cs not in sealed

    todo = [t for t in parse_tl(tl_path) if t[1] not in ids and t[0] != "vector" and t[0] not in SKIP_TL]
    skipped = [t for t in todo if not extendable(t[0], t[3])]
    todo = [t for t in todo if extendable(t[0], t[3])]
    final_names = {cs_name(t[0]) + ("V229" if cs_name(t[0]) in names else "") for t in todo}
    bases_needed = {(resolve_base(b, names, sealed), b) for _, _, _, b in todo
                    if resolve_base(b, names, sealed) not in names
                    and resolve_base(b, names, sealed) not in final_names}

    chunks, added = [], []
    for base_cs, base_tl in sorted(bases_needed):
        chunks.append(f"\t/// <summary>Слой 229: базовый тип {base_tl} </summary>\n"
                      f"\tpublic abstract partial class {base_cs} : IObject {{ }}")
    for tl_name, ctor, params, base in todo:
        name, _, code = render(tl_name, ctor, params, base, names, sealed)
        chunks.append(code)
        added.append(f"{ctor:08x} {tl_name} -> {name}")

    header = ('// <auto-generated>\n'
              '//   Сгенерировано tools/gen-patch229.py из живой схемы слоя (telegram_api.tl).\n'
              '//   Не править руками: при новой дыре в разборе — перегенерировать.\n'
              '//   Базы: ' + ", ".join(sorted(b for b, _ in bases_needed)) + '\n'
              '// </auto-generated>\n\n'
              'using System;\n\n'
              'namespace TL\n{\n')
    (src / "TL.Schema.Patch229.Rest.cs").write_text(header + "\n\n".join(chunks) + "\n}\n", encoding="utf-8")
    print(f"добавлено типов: {len(added)}, базовых: {len(bases_needed)}")
    for line in added:
        print("  " + line)
    for tl_name, ctor, _, base in skipped:
        print(f"  ПРОПУЩЕН {ctor:08x} {tl_name}: база {base} в схеме sealed")


if __name__ == "__main__":
    main()
