using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using Roxyz0501.Dalamud.CommonUi;

namespace VendorCompass;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface pi;
    private readonly IContextMenu menu;
    private readonly ICommandManager commands;
    private readonly IGameGui gui;
    private readonly IDataManager data;
    private readonly IClientState client;
    private readonly IObjectTable objects;
    private readonly IAetheryteList destinations;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly Travel travel;
    private readonly ConfigStore config;
    private readonly LocalizedFonts fonts;
    private readonly CancellationTokenSource cancel = new();
    private readonly Task<Catalog> catalog;
    private readonly CommandInfo command;
    private string language;
    private bool open, focusVendors, catalogErrorReported;
    private string itemName = "";
    private (Vendor Place, string Label)[] rows = [];
    private int page;
    private const int PageSize = 12;

    public Plugin(IDalamudPluginInterface pi, IContextMenu menu, ICommandManager commands, IGameGui gui,
        IDataManager data, IClientState client, IObjectTable objects, IAetheryteList destinations,
        ICondition condition, IFramework framework, IPluginLog log)
    {
        this.pi = pi;
        this.menu = menu;
        this.commands = commands;
        this.gui = gui;
        this.data = data;
        this.client = client;
        this.objects = objects;
        this.destinations = destinations;
        this.framework = framework;
        this.log = log;
        Loc.CheckCompleteness();
        config = new(pi.ConfigFile.FullName);
        language = Loc.IsSupported(config.Current.Language) ? config.Current.Language! :
            Loc.FromGame(client.ClientLanguage) ?? Loc.Normalize(pi.UiLanguage) ?? "en";
        config.Change(config.Current with { Language = language });
        travel = new(pi, client, objects, destinations, condition, log);
        fonts = new(pi.UiBuilder.FontAtlas, []);
        var game = data.GameData;
        catalog = Task.Run(() => new Catalog(game, cancel.Token));
        command = new((_, _) => Open()) { HelpMessage = T("Vendors") };
        commands.AddHandler("/vendorcompass", command);
        menu.OnMenuOpened += OnMenu;
        pi.UiBuilder.Draw += Draw;
        pi.UiBuilder.OpenConfigUi += Open;
        pi.UiBuilder.OpenMainUi += Open;
        framework.Update += Update;
        client.Logout += Logout;
        open = config.LoadNotice != null || config.LoadBlocked;
    }

    private string T(string key) => Loc.Get(language, key);
    private void Open() => open = true;
    private void Update(IFramework _)
    {
        travel.Update();
        config.Tick();
        if (catalog.IsFaulted && !catalogErrorReported)
        {
            log.Error(catalog.Exception!, "Could not build vendor index");
            catalogErrorReported = open = true;
        }
    }
    private void Logout(int type, int code)
    {
        travel.End(null);
        config.Flush();
        rows = [];
        itemName = "";
        open = false;
    }

    private unsafe void OnMenu(IMenuOpenedArgs args)
    {
        if (!config.Current.Enabled || !catalog.IsCompletedSuccessfully) return;
        ulong raw;
        if (args.Target is MenuTargetInventory inventory)
            raw = inventory.TargetItem?.ItemId ?? 0;
        else if (args.Target is MenuTargetDefault)
        {
            var recipeItem = AgentRecipeItemContext.Instance();
            if (recipeItem != null && args.EventInterfaces.Contains((nint)recipeItem))
                raw = recipeItem->ResultItemId;
            else
                raw = gui.HoveredItem;
            if (raw == 0 && args.AddonName?.StartsWith("ChatLog", StringComparison.Ordinal) == true &&
                AgentChatLog.Instance() is var chat && chat != null && args.EventInterfaces.Contains((nint)chat))
                raw = chat->ContextItemId;
        }
        else return;
        uint id = ItemKey.Normalize(raw);
        if (!catalog.Result.Has(id)) return;
        args.AddMenuItem(new MenuItem { Name = T("Find"), OnClicked = _ => ShowVendors(id) });
    }
    private void ShowVendors(uint id)
    {
        var places = catalog.Result.Find(id);
        var pos = objects.LocalPlayer?.Position ?? Vector3.Zero;
        var attuned = destinations.Where(d => !d.IsApartment && !d.IsSharedHouse && d.SubIndex == 0).Select(d => d.TerritoryId).ToHashSet();
        var names = data.GetExcelSheet<ENpcResident>();
        var areas = data.GetExcelSheet<TerritoryType>();
        itemName = data.GetExcelSheet<Item>().GetRowOrDefault(id)?.Name.ExtractText() ?? $"#{id}";
        rows = places.OrderBy(v => v.Territory != client.TerritoryType)
            .ThenBy(v => !attuned.Contains(v.Territory))
            .ThenBy(v => v.Territory == client.TerritoryType ? Vector3.DistanceSquared(v.Position, pos) : v.Territory)
            .ThenBy(v => v.Npc).Select(v =>
            {
                var name = names.GetRowOrDefault(v.Npc)?.Singular.ExtractText();
                var area = areas.GetRowOrDefault(v.Territory)?.PlaceName.ValueNullable?.Name.ExtractText();
                return (v, $"{(string.IsNullOrWhiteSpace(name) ? $"#{v.Npc}" : name)}  ·  {area}");
            }).ToArray();
        page = 0;
        focusVendors = open = true;
        if (travel.Target == null) travel.End(null);
    }

    private void Draw()
    {
        if (!open) return;
        using var font = fonts.Push(language);
        ImGui.SetNextWindowSize(new Vector2(680, 400), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Vendor Compass###VendorCompass", ref open) && ImGui.BeginTabBar("Tabs"))
        {
            var flags = focusVendors ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
            focusVendors = false;
            if (ImGui.BeginTabItem(T("Vendors") + "###Vendors", flags))
            {
                DrawVendors();
                ImGui.EndTabItem();
            }
            StandardPluginTabs.DrawStandardTrailingTabs(T("Settings"), "Settings", () =>
            {
                var enabled = config.Current.Enabled;
                if (ImGui.Checkbox(T("Menu") + "###Enabled", ref enabled)) config.Change(config.Current with { Enabled = enabled });
                ImGui.TextWrapped(T("SavePolicy"));
                if (config.LoadNotice is { } notice) ImGui.TextWrapped(T(notice));
                if (config.Error != null)
                {
                    ImGui.TextWrapped(T(config.LoadBlocked ? "LoadBlocked" : config.Stopped ? "SaveStopped" : "SaveFailed"));
                    if (!config.LoadBlocked && ImGui.Button(T("RetrySave"))) config.Retry();
                }
            }, T("LanguageSettings"), () =>
            {
                var choice = StandardPluginTabs.DrawLanguageSelector(T("Language"), T("LanguageHelp"), language);
                if (choice == null) return;
                language = choice;
                config.Change(config.Current with { Language = choice });
                command.HelpMessage = T("Vendors");
            }, T("Support"), () => StandardPluginTabs.DrawSupportContent(T("SupportHeading"), T("SupportText"), T("SupportRecipient"), T("OpenKoFi")));
            ImGui.EndTabBar();
        }
        ImGui.End();
    }

    private void DrawVendors()
    {
        if (travel.Status is { } status) ImGui.TextWrapped(T(status));
        if (travel.Target != null && ImGui.Button(T("Stop"))) travel.End("Stopped");
        if (rows.Length == 0)
        {
            ImGui.TextWrapped(T(catalog.IsFaulted ? "DataError" : catalog.IsCompletedSuccessfully ? "Hint" : "Loading"));
            return;
        }
        // Use the host's game font for game-supplied names.
        using (pi.UiBuilder.DefaultFontHandle.Push()) ImGui.TextUnformatted(itemName);
        if (rows.Length > PageSize)
        {
            if (ImGui.ArrowButton("##previous", ImGuiDir.Left)) page = Math.Max(0, page - 1);
            ImGui.SameLine();
            ImGui.TextUnformatted($"{page + 1} / {(rows.Length + PageSize - 1) / PageSize}");
            ImGui.SameLine();
            if (ImGui.ArrowButton("##next", ImGuiDir.Right)) page = Math.Min((rows.Length - 1) / PageSize, page + 1);
        }
        for (int i = page * PageSize; i < Math.Min((page + 1) * PageSize, rows.Length); i++)
        {
            var (place, label) = rows[i];
            ImGui.PushID(i);
            if (ImGui.SmallButton(T("Map"))) gui.OpenMapWithMapLink(place.Territory, place.Map, place.Position);
            ImGui.SameLine();
            ImGui.BeginDisabled(travel.Target != null);
            if (ImGui.SmallButton(T("Go"))) travel.Start(place);
            ImGui.EndDisabled();
            ImGui.SameLine();
            using (pi.UiBuilder.DefaultFontHandle.Push()) ImGui.TextUnformatted(label);
            ImGui.PopID();
        }
    }

    public void Dispose()
    {
        menu.OnMenuOpened -= OnMenu;
        framework.Update -= Update;
        client.Logout -= Logout;
        pi.UiBuilder.Draw -= Draw;
        pi.UiBuilder.OpenMainUi -= Open;
        pi.UiBuilder.OpenConfigUi -= Open;
        commands.RemoveHandler("/vendorcompass");
        travel.End(null);
        cancel.Cancel();
        try { catalog.GetAwaiter().GetResult(); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { log.Warning(ex, "Vendor index unavailable"); }
        cancel.Dispose();
        config.Dispose();
        fonts.Dispose();
    }
}
