namespace Posty.Services;

/// <summary>One emoji plus the words people search for to find it.</summary>
public readonly record struct Emoji(string Glyph, string Keywords);

/// <summary>A named group of emoji shown as a tab in the picker.</summary>
public readonly record struct EmojiGroup(string Name, string Icon, Emoji[] Emoji);

/// <summary>
/// A compact, LinkedIn-friendly emoji set. Curated rather than exhaustive: the ones
/// that actually show up in professional posts, grouped for quick scanning and tagged
/// with keywords so search finds them.
/// </summary>
public static class EmojiData
{
    public static readonly EmojiGroup[] Groups =
    {
        new("Popular", "⭐", new Emoji[]
        {
            new("🚀", "rocket launch startup growth ship"),
            new("✅", "check done tick complete success"),
            new("🔥", "fire hot trending lit"),
            new("💡", "idea bulb insight tip"),
            new("🎯", "target goal aim focus"),
            new("📈", "chart growth up increase trend"),
            new("💪", "muscle strong effort power"),
            new("🙌", "raised hands celebrate praise"),
            new("👏", "clap applause congrats"),
            new("🎉", "party celebrate tada launch"),
            new("✨", "sparkles shine new magic"),
            new("⭐", "star favorite rating"),
            new("💯", "hundred perfect score"),
            new("👇", "point down below thread"),
            new("👉", "point right next"),
            new("🤝", "handshake deal partner agreement"),
            new("🧠", "brain smart think knowledge"),
            new("⚡", "bolt fast energy power"),
            new("❤️", "heart love like"),
            new("🙏", "pray thanks grateful please"),
        }),
        new("People", "😀", new Emoji[]
        {
            new("😀", "grin happy smile"),
            new("😃", "smile happy joy"),
            new("😄", "laugh happy smile"),
            new("😁", "beam grin happy"),
            new("😊", "blush smile happy"),
            new("🙂", "slight smile"),
            new("😉", "wink joke"),
            new("😎", "cool sunglasses confident"),
            new("🤩", "star struck amazed wow"),
            new("😍", "heart eyes love adore"),
            new("🤔", "think hmm consider"),
            new("😅", "sweat smile relief nervous"),
            new("😂", "laugh cry lol funny"),
            new("🥳", "party face celebrate"),
            new("😴", "sleep tired boring"),
            new("🤯", "mind blown shock wow"),
            new("😮", "wow surprise open mouth"),
            new("🙄", "eye roll annoyed"),
            new("😇", "angel innocent halo"),
            new("🤗", "hug care welcome"),
        }),
        new("Gestures", "👍", new Emoji[]
        {
            new("👍", "thumbs up like approve yes good"),
            new("👎", "thumbs down dislike no bad"),
            new("👏", "clap applause congrats"),
            new("🙌", "raised hands celebrate praise"),
            new("🙏", "pray thanks grateful please"),
            new("🤝", "handshake deal partner agreement"),
            new("💪", "muscle strong effort"),
            new("✌️", "peace victory two"),
            new("🤞", "fingers crossed luck hope"),
            new("👌", "ok perfect good"),
            new("👇", "point down below"),
            new("👆", "point up above"),
            new("👉", "point right next"),
            new("👈", "point left back"),
            new("🫶", "heart hands love care"),
            new("🤙", "call shaka hang loose"),
            new("✋", "hand stop high five"),
            new("👋", "wave hello bye hi"),
            new("💅", "nails confident sassy"),
            new("🫡", "salute respect yes sir"),
        }),
        new("Work", "💼", new Emoji[]
        {
            new("💼", "briefcase work business job"),
            new("📈", "chart up growth increase"),
            new("📉", "chart down decrease loss"),
            new("📊", "bar chart data analytics"),
            new("💰", "money bag cash revenue"),
            new("💵", "dollars money cash"),
            new("🏆", "trophy win award champion"),
            new("🥇", "gold medal first winner"),
            new("🎯", "target goal aim"),
            new("📌", "pin note important"),
            new("📎", "paperclip attach"),
            new("🗓️", "calendar date schedule"),
            new("⏰", "alarm time clock deadline"),
            new("📝", "memo note write plan"),
            new("✏️", "pencil write edit"),
            new("📢", "megaphone announce loud news"),
            new("🔔", "bell notify alert"),
            new("💻", "laptop computer work code"),
            new("📱", "phone mobile app"),
            new("🌐", "globe web internet world"),
        }),
        new("Ideas", "💡", new Emoji[]
        {
            new("💡", "idea bulb insight tip"),
            new("🧠", "brain smart think"),
            new("🔑", "key solution access unlock"),
            new("🧩", "puzzle piece solve fit"),
            new("🔍", "search magnify find research"),
            new("🔎", "search right magnify"),
            new("📚", "books learn study knowledge"),
            new("🎓", "graduate education degree learn"),
            new("🛠️", "tools build fix diy"),
            new("⚙️", "gear settings process system"),
            new("🧭", "compass direction strategy guide"),
            new("🗺️", "map plan roadmap"),
            new("🚦", "traffic light status go"),
            new("🧪", "test experiment lab try"),
            new("♻️", "recycle reuse sustainable loop"),
            new("🪄", "magic wand transform"),
            new("💎", "diamond value premium gem"),
            new("🌱", "seedling grow start new"),
            new("🌟", "glowing star shine highlight"),
            new("⚡", "bolt fast energy power"),
        }),
        new("Symbols", "✅", new Emoji[]
        {
            new("✅", "check done tick success"),
            new("❌", "cross no wrong error"),
            new("⚠️", "warning caution alert"),
            new("❓", "question ask help"),
            new("❗", "exclamation important alert"),
            new("➡️", "arrow right next"),
            new("⬅️", "arrow left back"),
            new("⬆️", "arrow up increase"),
            new("⬇️", "arrow down decrease"),
            new("🔄", "refresh cycle repeat update"),
            new("➕", "plus add more"),
            new("➖", "minus subtract less"),
            new("✔️", "check mark yes ok"),
            new("🔗", "link chain url connect"),
            new("💬", "speech comment chat reply"),
            new("💭", "thought bubble think"),
            new("🔥", "fire hot trending"),
            new("💯", "hundred perfect"),
            new("🚫", "no forbidden stop ban"),
            new("🆕", "new badge fresh"),
        }),
    };
}
