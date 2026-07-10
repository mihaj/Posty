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
    Monospace,
    SerifBold,
    Script
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
/// Accented Latin letters (č, ć, ž, š, đ, é, ñ…) have no precomposed styled glyph, so we
/// split them into an ASCII base plus combining accents, style just the base, and re-stack
/// the accents on top — the same trick, driven by Unicode canonical decomposition.
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

    // Stroke letters (đ, ł) have no Unicode decomposition, so we approximate the bar
    // through the stem with a combining overlay and fold it back to the real letter on
    // the way out. The overlay deliberately differs from StrikethroughMark (U+0336).
    private const char StrokeOverlay = '̵'; // combining short stroke overlay
    private static readonly Dictionary<int, char> StrokeToBase = new()
    {
        [0x0111] = 'd', [0x0110] = 'D', // đ Đ
        [0x0142] = 'l', [0x0141] = 'L', // ł Ł
    };
    private static readonly Dictionary<char, char> StrokeFromBase = new()
    {
        ['d'] = 'đ', ['D'] = 'Đ',
        ['l'] = 'ł', ['L'] = 'Ł',
    };

    // First code point of each contiguous styled range in the Mathematical
    // Alphanumeric Symbols block. Digits only exist for some styles, and a few
    // glyphs are relocated to the Letterlike Symbols block (see Exceptions).
    private readonly record struct Range(int Upper, int Lower, int? Digit, Dictionary<char, int>? Exceptions = null);

    // Script's italic-cursive letters have "holes": these code points were already
    // assigned in the Letterlike Symbols block, so the contiguous range skips them.
    private static readonly Dictionary<char, int> ScriptExceptions = new()
    {
        ['B'] = 0x212C, ['E'] = 0x2130, ['F'] = 0x2131, ['H'] = 0x210B, ['I'] = 0x2110,
        ['L'] = 0x2112, ['M'] = 0x2133, ['R'] = 0x211B,
        ['e'] = 0x212F, ['g'] = 0x210A, ['o'] = 0x2134,
    };

    private static readonly Dictionary<TextStyle, Range> Ranges = new()
    {
        [TextStyle.Bold]       = new Range(0x1D5D4, 0x1D5EE, 0x1D7EC),                  // sans-serif bold
        [TextStyle.Italic]     = new Range(0x1D608, 0x1D622, null),                     // sans-serif italic
        [TextStyle.BoldItalic] = new Range(0x1D63C, 0x1D656, null),                     // sans-serif bold italic
        [TextStyle.Monospace]  = new Range(0x1D670, 0x1D68A, 0x1D7F6),                  // monospace
        [TextStyle.SerifBold]  = new Range(0x1D400, 0x1D41A, 0x1D7CE),                  // serif bold
        [TextStyle.Script]     = new Range(0x1D49C, 0x1D4B6, null, ScriptExceptions),   // script / cursive
    };

    // Reverse lookup: styled code point -> the ASCII character it stands for.
    // Lets us normalise any styled text back to plain ASCII before re-styling.
    private static readonly Dictionary<int, char> StyledToAscii = new();

    static PostStyler()
    {
        // Build the reverse map through StyledCodePoint so per-letter exceptions
        // (script's relocated glyphs) are registered at their real code points.
        foreach (var (style, range) in Ranges)
        {
            for (var c = 'A'; c <= 'Z'; c++) StyledToAscii[StyledCodePoint(c, style)] = c;
            for (var c = 'a'; c <= 'z'; c++) StyledToAscii[StyledCodePoint(c, style)] = c;
            if (range.Digit is not null)
                for (var c = '0'; c <= '9'; c++) StyledToAscii[StyledCodePoint(c, style)] = c;
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
        // Un-styling puts letters back to ASCII base + accents; fold those into real
        // precomposed letters (c+caron -> č, d+stroke -> đ) so the plain text is clean.
        return style == TextStyle.Normal ? Recompose(sb.ToString()) : sb.ToString();
    }

    /// <summary>True if <paramref name="text"/> contains at least one letter and every
    /// letter already carries <paramref name="style"/> — used to make buttons toggle.</summary>
    public static bool HasLetterStyle(string text, TextStyle style)
    {
        var any = false;
        foreach (var g in Graphemes(text))
        {
            if (LetterIdentity(g.BaseCodePoint) is not { } ascii) continue;
            any = true;
            // A styled accented letter is stored as a styled ASCII base + accents, so its
            // base code point already equals StyledCodePoint(base). A plain precomposed
            // č never does, so it correctly reads as "not yet styled".
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

    /// <summary>Strip all styling and decorations back to plain text. Letter accents
    /// (the caron on č, the stroke on đ) are preserved; only underline/strike marks go.</summary>
    public static string ClearFormatting(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var g in Graphemes(text))
        {
            sb.Append(StyleBase(g.BaseCodePoint, TextStyle.Normal));
            foreach (var m in g.Marks)
                if (!IsDecorationMark(m)) sb.Append(m); // keep accents, drop underline/strike
        }
        return Recompose(sb.ToString());
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
        if (ascii is not null)
            return style == TextStyle.Normal
                ? ascii.Value.ToString()
                : CodePointToString(StyledCodePoint(ascii.Value, style));

        // Accented Latin (č, ć, ž, š, đ, é…): no precomposed styled glyph exists, so split
        // into an ASCII base + combining accents, style just the base, and re-stack the
        // accents. For Normal we leave the letter precomposed (Recompose handles that).
        if (style != TextStyle.Normal && TryDecompose(codePoint, out var baseAscii, out var accents))
            return CodePointToString(StyledCodePoint(baseAscii, style)) + accents;

        return CodePointToString(codePoint); // punctuation, emoji, non-decomposable letters
    }

    private static int StyledCodePoint(char ascii, TextStyle style)
    {
        if (style == TextStyle.Normal || !Ranges.TryGetValue(style, out var range))
            return ascii;

        if (range.Exceptions is not null && range.Exceptions.TryGetValue(ascii, out var relocated))
            return relocated;

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

    /// <summary>The ASCII letter a code point ultimately stands for — plain ASCII, a styled
    /// variant, or an accented letter (č -> c). Null for anything that isn't a letter.</summary>
    private static char? LetterIdentity(int codePoint)
    {
        if (AsciiLetterOf(codePoint) is { } ascii) return ascii;
        if (TryDecompose(codePoint, out var baseAscii, out _)) return baseAscii;
        return null;
    }

    /// <summary>Split an accented Latin letter into an ASCII base + its combining accents,
    /// e.g. č -> ('c', "◌̌"). Uses Unicode canonical decomposition, with an explicit map for
    /// the stroke letters (đ, ł) that have no decomposition. False for everything else.</summary>
    private static bool TryDecompose(int codePoint, out char baseAscii, out string accents)
    {
        if (StrokeToBase.TryGetValue(codePoint, out baseAscii))
        {
            accents = StrokeOverlay.ToString();
            return true;
        }

        baseAscii = '\0';
        accents = "";
        if (codePoint is >= 0xD800 and <= 0xDFFF) return false; // lone surrogate: not a letter

        var d = char.ConvertFromUtf32(codePoint).Normalize(NormalizationForm.FormD);
        if (d.Length < 2 || d[0] is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z')) return false;
        for (var i = 1; i < d.Length; i++)
            if (!IsCombining(d[i])) return false; // only handle simple base + accents

        baseAscii = d[0];
        accents = d[1..];
        return true;
    }

    private static bool IsDecorationMark(char m) =>
        m is UnderlineMark or DoubleUnderlineMark or StrikethroughMark;

    /// <summary>Fold ASCII base + combining accents back into precomposed letters so plain
    /// text stays canonical: c+caron -> č via NFC, and d+stroke -> đ via the explicit map.</summary>
    private static string Recompose(string s)
    {
        if (s.IndexOf(StrokeOverlay) >= 0)
        {
            var sb = new StringBuilder(s.Length);
            for (var i = 0; i < s.Length; i++)
            {
                if (i + 1 < s.Length && s[i + 1] == StrokeOverlay && StrokeFromBase.TryGetValue(s[i], out var whole))
                {
                    sb.Append(whole);
                    i++; // consumed the overlay too
                }
                else sb.Append(s[i]);
            }
            s = sb.ToString();
        }
        return s.Normalize(NormalizationForm.FormC);
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
