using Lumina;
using Lumina.Excel.Sheets;
using VendorCompass;

using var game = new GameData(args.Single());
var catalog = new Catalog(game, CancellationToken.None);
Loc.CheckCompleteness();
foreach (var raw in new ulong[] { 5056, 505056, 1005056 })
    if (ItemKey.Normalize(raw) != 5056) throw new Exception("Item quality normalization.");
foreach (var raw in new ulong[] { 0, 500000, 1000000, 2005056, ulong.MaxValue })
    if (ItemKey.Normalize(raw) != 0) throw new Exception("Invalid item accepted.");
if (!catalog.Find(5056).Any(v => v.Territory == 129 &&
    game.Excel.GetSheet<ENpcResident>().GetRow(v.Npc).Singular.ExtractText() == "Engerrand"))
    throw new Exception("Bronze Ingot vendor in Limsa Lominsa missing.");
if (catalog.Find(2).Any() || catalog.Find(5056).Distinct().Count() != catalog.Find(5056).Count())
    throw new Exception("Unexpected sale or duplicate placement.");
var timer = System.Diagnostics.Stopwatch.StartNew();
for (int i = 0; i < 100000; i++) if (!catalog.Has(4868)) throw new Exception("Repeated lookup");
Console.WriteLine($"100000 warm menu lookups: {timer.ElapsedMilliseconds} ms");
var testDir = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "fixture-" + Guid.NewGuid().ToString("N")));
var file = Path.Combine(testDir.FullName, "config.json");
try
{
    long now = 0;
    using (var store = new ConfigStore(file, () => now))
    {
        foreach (var lang in new[] { "ja", "en", "de", "fr", "ko" })
        {
            store.Change(store.Current with { Language = lang });
            now += 60001;
            store.Tick();
        }
        var bytes = store.PayloadBytes;
        for (int i = 0; i < 480; i++) { now += 60001; store.Tick(); }
        if (store.SaveCount != 5 || store.PayloadBytes != bytes || Directory.GetFiles(testDir.FullName, "*.bak.*").Length != 3)
            throw new Exception($"Persistence: saves={store.SaveCount}, failures={store.Failures}, backups={Directory.GetFiles(testDir.FullName, "*.bak.*").Length}, error={store.Error}");
        Console.WriteLine($"Five changes: {bytes} bytes; 480 unchanged checkpoints: 0 writes");
        store.Change(store.Current with { Language = "zh-Hant" });
        store.Flush();
    }
    using (var reload = new ConfigStore(file))
        if (reload.Current.Language != "zh-Hant") throw new Exception("Logout persistence");
    File.WriteAllText(file, "!");
    using var recovered = new ConfigStore(file);
    if (recovered.Current.Language != "ko") throw new Exception("Backup recovery");
}
finally
{
    foreach (var tempFile in testDir.GetFiles()) tempFile.Delete();
    testDir.Delete();
}
foreach (var id in new uint[] { 2, 4868, 5056, 5333, 5729 })
{
    var places = catalog.Find(id).ToArray();
    Console.WriteLine($"{id} {game.Excel.GetSheet<Item>().GetRow(id).Name}: {places.Length} placements");
    foreach (var place in places.Take(2))
        Console.WriteLine($"  {game.Excel.GetSheet<ENpcResident>().GetRow(place.Npc).Singular} / {place.Territory} / {place.Position}");
}
if (catalog.Has(0) || !catalog.Has(4868) || catalog.Count < 1000) throw new Exception("Catalog regression.");
Console.WriteLine($"{catalog.Count} distinct purchasable items.");
