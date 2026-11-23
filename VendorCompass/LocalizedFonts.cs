using Dalamud;
using Dalamud.Interface.ManagedFontAtlas;

namespace VendorCompass;

internal sealed class LocalizedFonts : IDisposable
{
    private readonly IFontHandle[] fonts;

    // Glyphs used by the canonical information tab and dependency descriptions.
    private const string InfoGlyphs = "ÉÖäéü。きくけしすせたでなにのはへまみるをんイグプラン・人件体作使依信入内再別利刷前功动効動南単可启商啟基報売安定導已开往必态息情態所打択指插操新无未本案機法済無状狀独獨理用的确確移立管能自装裝要訊認认販資赖选選重開需須가관그기내능니다단독동됨러로리매미별보본불비사상선설성수습안업없열용으은음이인자작정치태택판플필한합확활";

    public LocalizedFonts(IFontAtlas atlas, IEnumerable<string> itemNames)
    {
        var glyphs = (Loc.FontText + InfoGlyphs + string.Concat(itemNames)
            + new string(Enumerable.Range(32, 95).Select(x => (char)x).ToArray())).ToGlyphRange();
        fonts = Enumerable.Range(0, 4).Select(face => atlas.NewDelegateFontHandle(step => step.OnPreBuild(tk =>
            tk.AddDalamudAssetFont(DalamudAsset.NotoSansCjkRegular,
                new SafeFontConfig { SizePx = 17, FontNo = face, GlyphRanges = glyphs })))).ToArray();
    }

    private static int Face(string? code) => code switch { "zh-Hant" => 1, "zh-Hans" => 2, "ko" => 3, _ => 0 };
    public IDisposable Push(string? code) => fonts[Face(code)].Push();
    public void Dispose() { foreach (var font in fonts) font.Dispose(); }
}

