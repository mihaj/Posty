using System.Text;

namespace Posty.Services;

/// <summary>
/// The letter style applied to a run of text. Styles are mutually exclusive:
/// a character is exactly one of these.
/// </summary>
public enum TextStyle
{
    Normal,
    Bold,
    Italic,
    BoldItalic,
    Monospace
}

/// <summary>
/// A combining decoration layered on top of a character. Decorations are additive
/// (a character can be bold AND underlined) but the three underline-family marks
/// are toggled independently.
/// </summary>
public enum Decoration
{
    Underline,
    DoubleUnderline,
    Strikethrough
}

/// <summary>
/// Turns plain ASCII into visually-styled Unicode that survives a copy-paste into
/// platforms (LinkedIn, X, Instagram…) that strip real rich-text formatting.
///
/// Letter styling uses the Mathematical Alphanumeric Symbols block. We deliberately
/// use the SANS-SERIF variants because those ranges are contiguous (no reserved
/// "holes" the way serif italic/script are), so a simple offset from the ASCII base
/// always lands on the right glyph. Decorations use zero-width combining marks that
/// sit on top of the preceding character.
///
/// Everything is grapheme-aware: a "grapheme" is a base code point plus any trailing
/// combining marks, so bold+underline compose and each attribute toggles cleanly.
/// </summary>
public static class PostStyler
{
    // --- Combining marks (each renders on top of the preceding base glyph) ---
    public const char UnderlineMark = '̲';       // combining low line
    public const char DoubleUnderlineMark = '̳'; // combining double low line
    public const char StrikethroughMark = '̶';   // combining long stroke overlay

    // First code point of each contiguous styled range in the Mathematical
    // Alphanumeric Symbols block. Digits only exist for some styles.
    private readonly record struct Range(int Upper, int Lower, int? Digit);

    private static readonly Dictionary<TextStyle, Range> Ranges = new()
    {
        [TextStyle.Bold]       = new Range(0x1D5D4, 0x1D5EE, 0x1D7EC), // sans-serif bold
        [TextStyle.Italic]     = new Range(0x1D608, 0x1D622, null),    // sans-serif italic
        [TextStyle.BoldItalic] = new Range(0x1D63C, 0x1D656, null),    // sans-serif bold italic
        [TextStyle.Monospace]  = new Range(0x1D670, 0x1D68A, 0x1D7F6), // monospace
    };

    // Reverse lookup: styled code point -> the ASCII character it stands for.
    // Lets us normalise any styled text back to plain ASCII before re-styling.
    private static readonly Dictionary<int, char> StyledToAscii = new();

    static PostStyler()
    {
        foreach (var range in Ranges.Values)
        {
            for (var i = 0; i < 26; i++)
            {
                StyledToAscii[range.Upper + i] = (char)('A' + i);
                StyledToAscii[range.Lower + i] = (char)('a' + i);
            }

            if (range.Digit is { } digit)
                for (var i = 0; i < 10; i++)
                    StyledToAscii[digit + i] = (char)('0' + i);
        }
    }

    public static char MarkFor(Decoration decoration) => decoration switch
    {
        Decoration.Underline => UnderlineMark,
        Decoration.DoubleUnderline => DoubleUnderlineMark,
        Decoration.Strikethrough => StrikethroughMark,
        _ => UnderlineMark
    };

    /// <summary>Re-style every letter/digit in <paramref name="text"/> while keeping any
    /// combining decorations intact. <see cref="TextStyle.Normal"/> strips letter styling.</summary>
    public static string SetLetterStyle(string text, TextStyle style)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var g in Graphemes(text))
        {
            sb.Append(StyleBase(g.BaseCodePoint, style));
            foreach (var mark in g.Marks) sb.Append(mark);
        }
        return sb.ToString();
    }

    /// <summary>True if <paramref name="text"/> contains at least one letter and every
    /// letter already carries <paramref name="style"/> — used to make buttons toggle.</summary>
    public static bool HasLetterStyle(string text, TextStyle style)
    {
        var any = false;
        foreach (var g in Graphemes(text))
        {
            if (AsciiLetterOf(g.BaseCodePoint) is not { } ascii) continue;
            any = true;
            if (g.BaseCodePoint != StyledCodePoint(ascii, style)) return false;
        }
        return any;
    }

    /// <summary>Add or remove a combining decoration on every visible grapheme.
    /// Spaces are decorated too so underlines stay continuous; newlines are skipped.</summary>
    public static string ToggleDecoration(string text, Decoration decoration, bool add)
    {
        var mark = MarkFor(decoration);
        var sb = new StringBuilder(text.Length);
        foreach (var g in Graphemes(text))
        {
            sb.Append(CodePointToString(g.BaseCodePoint));

            var hasMark = false;
            foreach (var m in g.Marks)
            {
                if (m == mark) { hasMark = true; if (!add) continue; }
                sb.Append(m);
            }

            var decorable = !IsNewline(g.BaseCodePoint);
            if (add && decorable && !hasMark) sb.Append(mark);
        }
        return sb.ToString();
    }

    /// <summary>True if every decorable grapheme already carries the decoration.</summary>
    public static bool HasDecoration(string text, Decoration decoration)
    {
        var mark = MarkFor(decoration);
        var any = false;
        foreach (var g in Graphemes(text))
        {
            if (IsNewline(g.BaseCodePoint) || IsSpace(g.BaseCodePoint)) continue;
            any = true;
            if (!g.Marks.Contains(mark)) return false;
        }
        return any;
    }

    /// <summary>Strip all styling and decorations back to plain text.</summary>
    public static string ClearFormatting(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var g in Graphemes(text))
            sb.Append(StyleBase(g.BaseCodePoint, TextStyle.Normal)); // marks dropped
        return sb.ToString();
    }

    private const string Bullet = "• ";

    /// <summary>Toggle a "• " prefix on every non-empty line of the block.</summary>
    public static string ToggleBullets(string block)
    {
        var lines = block.Split('\n');
        var allBulleted = lines.Where(l => l.Trim().Length > 0)
                               .All(l => l.TrimStart().StartsWith(Bullet, StringComparison.Ordinal));

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Trim().Length == 0) continue;

            lines[i] = allBulleted ? StripPrefix(line, Bullet) : Bullet + line.TrimStart();
        }
        return string.Join('\n', lines);
    }

    /// <summary>Toggle a "1. 2. 3." numbered prefix on every non-empty line of the block.</summary>
    public static string ToggleNumbers(string block)
    {
        var lines = block.Split('\n');
        var allNumbered = lines.Where(l => l.Trim().Length > 0).All(HasNumberPrefix);

        var n = 1;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Trim().Length == 0) continue;

            lines[i] = allNumbered ? StripNumberPrefix(line) : $"{n++}. {line.TrimStart()}";
        }
        return string.Join('\n', lines);
    }

    // ---------------------------------------------------------------- helpers

    private static string StyleBase(int codePoint, TextStyle style)
    {
        // Fold any already-styled letter back to ASCII first, so re-styling works.
        var ascii = AsciiOf(codePoint);
        if (ascii is null) return CodePointToString(codePoint); // punctuation, emoji, etc.

        return style == TextStyle.Normal
            ? ascii.Value.ToString()
            : CodePointToString(StyledCodePoint(ascii.Value, style));
    }

    private static int StyledCodePoint(char ascii, TextStyle style)
    {
        if (style == TextStyle.Normal || !Ranges.TryGetValue(style, out var range))
            return ascii;

        if (ascii is >= 'A' and <= 'Z') return range.Upper + (ascii - 'A');
        if (ascii is >= 'a' and <= 'z') return range.Lower + (ascii - 'a');
        if (ascii is >= '0' and <= '9' && range.Digit is { } digit) return digit + (ascii - '0');
        return ascii; // no styled variant (e.g. italic digits) -> leave plain
    }

    /// <summary>Maps a code point to the ASCII letter/digit it represents, whether it is
    /// already plain ASCII or a styled variant. Null for anything else.</summary>
    private static char? AsciiOf(int codePoint)
    {
        if (StyledToAscii.TryGetValue(codePoint, out var mapped)) return mapped;
        if (codePoint is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9')
            return (char)codePoint;
        return null;
    }

    private static char? AsciiLetterOf(int codePoint)
    {
        var ascii = AsciiOf(codePoint);
        return ascii is >= 'A' and <= 'Z' or >= 'a' and <= 'z' ? ascii : null;
    }

    private readonly record struct Grapheme(int BaseCodePoint, List<char> Marks);

    /// <summary>Walk the string as base-glyph + trailing-combining-marks groups,
    /// tolerating surrogate pairs and lone surrogates.</summary>
    private static IEnumerable<Grapheme> Graphemes(string s)
    {
        int? baseCp = null;
        var marks = new List<char>();

        foreach (var cp in CodePoints(s))
        {
            if (IsCombining(cp))
            {
                if (baseCp is null) { baseCp = cp; continue; } // stray mark: treat as base
                marks.Add((char)cp);
            }
            else
            {
                if (baseCp is { } prev) yield return new Grapheme(prev, marks);
                baseCp = cp;
                marks = new List<char>();
            }
        }

        if (baseCp is { } last) yield return new Grapheme(last, marks);
    }

    private static IEnumerable<int> CodePoints(string s)
    {
        for (var i = 0; i < s.Length;)
        {
            var c = s[i];
            if (char.IsHighSurrogate(c) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
            {
                yield return char.ConvertToUtf32(c, s[i + 1]);
                i += 2;
            }
            else
            {
                yield return c;
                i += 1;
            }
        }
    }

    private static string CodePointToString(int cp) =>
        cp is >= 0xD800 and <= 0xDFFF ? ((char)cp).ToString() : char.ConvertFromUtf32(cp);

    private static bool IsCombining(int cp) => cp is >= 0x0300 and <= 0x036F;
    private static bool IsNewline(int cp) => cp is '\n' or '\r';
    private static bool IsSpace(int cp) => cp is ' ' or '\t';

    private static string StripPrefix(string line, string prefix)
    {
        var leading = line[..(line.Length - line.TrimStart().Length)];
        var body = line.TrimStart();
        return leading + (body.StartsWith(prefix, StringComparison.Ordinal) ? body[prefix.Length..] : body);
    }

    private static bool HasNumberPrefix(string line)
    {
        var body = line.TrimStart();
        var i = 0;
        while (i < body.Length && char.IsDigit(body[i])) i++;
        return i > 0 && i + 1 < body.Length && body[i] == '.' && body[i + 1] == ' ';
    }

    private static string StripNumberPrefix(string line)
    {
        if (!HasNumberPrefix(line)) return line;
        var leading = line[..(line.Length - line.TrimStart().Length)];
        var body = line.TrimStart();
        var dot = body.IndexOf('.');
        return leading + body[(dot + 2)..]; // skip "N. "
    }
}
