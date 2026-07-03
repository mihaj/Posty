# Posty

**Write it plain. Paste it formatted.**

Posty is a free, no-login web tool that turns plain text into **bold**, *italic*, underlined and bulleted text you can paste straight into LinkedIn — or any platform that strips real rich-text formatting.

LinkedIn (and X, Instagram, and others) throw away bold, italics and bullets when you paste. Posty gets around this by swapping your letters for look-alike Unicode glyphs from the [Mathematical Alphanumeric Symbols](https://en.wikipedia.org/wiki/Mathematical_Alphanumeric_Symbols) block and layering combining marks for underlines and strikethrough. Because these are real characters rather than styling, they survive the paste and your post reads exactly the way you wrote it.

Everything runs entirely in your browser via Blazor WebAssembly — no ads, no trackers, no sign-up, and nothing is ever uploaded.

## Features

- **Letter styles** — bold, italic, bold italic, serif bold, script/cursive, and monospace
- **Decorations** — underline, double underline, and strikethrough (these compose with letter styles, e.g. bold + underline)
- **Lists** — toggle bulleted (`•`) or numbered (`1.`) prefixes across a block of lines
- **Emoji picker** — searchable, grouped emoji you can insert inline
- **Live LinkedIn preview** — see your post as it will appear in the feed, including the `…more` truncation point
- **Keyboard shortcuts** — <kbd>Ctrl</kbd>/<kbd>Cmd</kbd> + <kbd>B</kbd> / <kbd>I</kbd> / <kbd>U</kbd> on the current selection
- **One-click copy** — paste directly into the LinkedIn post box with formatting intact
- **Character counter** — grapheme-aware count against LinkedIn's 3,000-character limit
- **Clear options** — remove formatting (keeping your words) or wipe the editor

> **Accessibility note:** styled output uses real Unicode characters, not CSS styling, so screen readers may read them oddly. Keep names and key words in plain text.

## Tech stack

- [.NET 10](https://dotnet.microsoft.com/) / **Blazor WebAssembly** — the app is a single-page client-side app; there is no server
- **C#** for the formatting engine ([`Posty/Services/PostStyler.cs`](Posty/Services/PostStyler.cs))
- A thin **JavaScript** interop layer ([`Posty/wwwroot/js/posty.js`](Posty/wwwroot/js/posty.js)) for the parts Blazor can't do natively: reading/restoring the textarea selection, auto-growing the editor, routing keyboard shortcuts, and clipboard access

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run locally

```bash
cd Posty
dotnet run
```

Then open the URL printed in the console (by default <https://localhost:7087>).

### Publish

Produce an optimized static build you can host on any static file host (GitHub Pages, Netlify, Azure Static Web Apps, etc.):

```bash
cd Posty
dotnet publish -c Release
```

The published site lands in `Posty/bin/Release/net10.0/publish/wwwroot`.

## Project structure

```
Posty/
├── Program.cs                 # WebAssembly host bootstrap
├── App.razor                  # Router root
├── Pages/
│   └── Home.razor             # The single-page editor, preview, toolbar & emoji picker
├── Services/
│   ├── PostStyler.cs          # The Unicode styling engine (grapheme-aware)
│   └── EmojiData.cs           # Emoji groups and keywords for the picker
├── Layout/                    # Layout components
└── wwwroot/
    ├── index.html
    ├── css/app.css
    └── js/posty.js            # Selection, auto-grow, shortcuts & clipboard interop
```

## How the styling engine works

`PostStyler` is a stateless, static class that works on **graphemes** — a base code point plus any trailing combining marks:

- **Letter styles** map each ASCII letter/digit to its counterpart in a contiguous range of the Mathematical Alphanumeric Symbols block. The sans-serif variants are used deliberately because their ranges have no reserved "holes," so a simple offset from the ASCII base always lands on the right glyph. Script/cursive is the exception — several of its letters live in the Letterlike Symbols block, handled explicitly.
- **Decorations** are zero-width combining marks (`◌̲` low line, `◌̳` double low line, `◌̶` long stroke overlay) appended after each base glyph, so they compose with any letter style and toggle independently.
- Styling is **reversible**: any styled character is folded back to plain ASCII before re-styling, which is how toggling a style off and switching between styles works cleanly.

## License

© 2026 [Miha Jakovac](https://mihajakovac.com)
