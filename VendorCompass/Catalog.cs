using System.Numerics;
using Lumina;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Layer;
using Lumina.Excel.Sheets;

namespace VendorCompass;

internal sealed record Vendor(uint Npc, uint Territory, uint Map, Vector3 Position);

internal sealed class Catalog
{
    // One placement array per NPC. Items share those arrays even for very large shops.
    private readonly Dictionary<uint, HashSet<uint>> sellers = new();
    private readonly Dictionary<uint, List<Vendor>> locations = new();
    public int Count => sellers.Count;
    public bool Has(uint item) => sellers.ContainsKey(item);
    public IEnumerable<Vendor> Find(uint item) =>
        sellers.TryGetValue(item, out var npcs) ? npcs.SelectMany(id => locations[id]) : [];

    public Catalog(GameData game, CancellationToken cancel)
    {
        var excel = game.Excel;
        var inventory = new Dictionary<uint, HashSet<uint>>();
        void Stock(uint shop, uint item)
        {
            if (item == 0) return;
            if (!inventory.TryGetValue(shop, out var items)) inventory[shop] = items = [];
            items.Add(item);
        }
        foreach (var group in excel.GetSubrowSheet<GilShopItem>())
            foreach (var row in group) Stock(row.RowId, row.Item.RowId);
        foreach (var shop in excel.GetSheet<SpecialShop>())
        {
            cancel.ThrowIfCancellationRequested();
            foreach (var offer in shop.Item)
                foreach (var reward in offer.ReceiveItems) Stock(shop.RowId, reward.Item.RowId);
        }
        var pre = excel.GetSheet<PreHandler>();
        var topics = excel.GetSheet<TopicSelect>();
        var talks = excel.GetSheet<CustomTalk>();
        var inclusions = excel.GetSheet<InclusionShop>();
        var series = excel.GetSubrowSheet<InclusionShopSeries>();
        var stockByNpc = new Dictionary<uint, uint[]>();
        foreach (var npc in excel.GetSheet<ENpcBase>())
        {
            cancel.ThrowIfCancellationRequested();
            var shops = new HashSet<uint>();
            var seen = new HashSet<uint>();
            void Visit(uint id)
            {
                if (id == 0 || !seen.Add(id)) return;
                if (inventory.ContainsKey(id)) shops.Add(id);
                else if (pre.TryGetRow(id, out var p)) Visit(p.Target.RowId);
                else if (topics.TryGetRow(id, out var t))
                    foreach (var child in t.Shop) Visit(child.RowId);
                else if (talks.TryGetRow(id, out var talk)) Visit(talk.SpecialLinks.RowId);
                else if (inclusions.TryGetRow(id, out var inc))
                    foreach (var c in inc.Category)
                        if (c.RowId != 0 && c.ValueNullable is { } cat && series.TryGetRow(cat.InclusionShopSeries.RowId, out var pages))
                            foreach (var page in pages) Visit(page.SpecialShop.RowId);
            }
            foreach (var entry in npc.ENpcData) Visit(entry.RowId);
            if (shops.Count > 0) stockByNpc[npc.RowId] = shops.ToArray();
        }

        void Place(uint npc, uint territory, uint map, Vector3 pos)
        {
            if (!stockByNpc.ContainsKey(npc) || map == 0 || !float.IsFinite(pos.LengthSquared())) return;
            if (!locations.TryGetValue(npc, out var list)) locations[npc] = list = [];
            if (!list.Any(v => v.Territory == territory && Vector3.DistanceSquared(v.Position, pos) < 4))
                list.Add(new(npc, territory, map, pos));
        }
        var areas = excel.GetSheet<TerritoryType>().Where(t => t.Map.RowId != 0 && t.TerritoryIntendedUse.RowId is 0 or 1).ToDictionary(t => t.RowId);
        foreach (var row in excel.GetSheet<Level>())
            if (stockByNpc.ContainsKey(row.Object.RowId) && areas.TryGetValue(row.Territory.RowId, out var area))
                Place(row.Object.RowId, area.RowId, row.Map.RowId == 0 ? area.Map.RowId : row.Map.RowId, new(row.X, row.Y, row.Z));
        foreach (var area in areas.Values)
        {
            cancel.ThrowIfCancellationRequested();
            var bg = area.Bg.ExtractText();
            var cut = bg.IndexOf("/level/", StringComparison.Ordinal);
            if (cut < 0) continue;
            var lgb = game.GetFile<LgbFile>("bg/" + bg[..cut] + "/level/planevent.lgb");
            if (lgb == null) continue;
            foreach (var layer in lgb.Layers)
                foreach (var obj in layer.InstanceObjects)
                    if (obj.AssetType == LayerEntryType.EventNPC && obj.Object is LayerCommon.ENPCInstanceObject enpc)
                    {
                        var p = obj.Transform.Translation;
                        Place(enpc.ParentData.ParentData.BaseId, area.RowId, area.Map.RowId, new(p.X, p.Y, p.Z));
                    }
        }
        foreach (var npc in locations.Keys)
            foreach (var shop in stockByNpc[npc])
                foreach (var item in inventory[shop])
                {
                    if (!sellers.TryGetValue(item, out var ids)) sellers[item] = ids = [];
                    ids.Add(npc);
                }
    }
}
