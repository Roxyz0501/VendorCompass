using Roxyz0501.Dalamud.CommonUi;

namespace VendorCompass;

internal static class Dependencies
{
    public static readonly PluginDependency[] All =
    [
        new("vnavmesh", "vnavmesh", false,
            new InfoText(
                "Automatic travel to the selected vendor.",
                "選択した販売NPCへの自動移動。",
                "Automatische Reise zum ausgewählten Händler.",
                "Déplacement automatique vers le vendeur sélectionné.",
                "선택한 판매 NPC로 자동 이동.",
                "自动前往选定的商人。",
                "自動前往選定的商人。"),
            Guide: "https://github.com/awgil/ffxiv_navmesh")
    ];
}
