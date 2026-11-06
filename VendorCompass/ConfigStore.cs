using System.Text.Json;

namespace VendorCompass;

public sealed record ConfigValues(int Version = 1, string? Language = null, bool Enabled = true);

// Tiny settings only. A synchronous, bounded single writer avoids worker lifetime
// races and never depends on the game/UI thread to complete an outstanding task.
public sealed class ConfigStore : IDisposable
{
    private readonly object gate = new();
    private readonly string path;
    private readonly Func<long> clock;
    private readonly Action<string>? fault;
    private ConfigValues current = new();
    private ConfigValues? saved;
    private long nextCheck;
    private long retryAt;
    private bool disposed, writing;
    public ConfigValues Current { get { lock (gate) return current; } }
    public bool Pending { get { lock (gate) return current != saved; } }
    public int Failures { get; private set; }
    public bool LoadBlocked { get; private set; }
    public bool Stopped => LoadBlocked || Failures >= 3;
    public string? Error { get; private set; }
    public string? LoadNotice { get; private set; }
    public long SaveCount { get; private set; }
    public long PayloadBytes { get; private set; }
    public long Attempts { get; private set; }

    public ConfigStore(string path, Func<long>? clock = null, Action<string>? fault = null)
    {
        this.path = Path.GetFullPath(path);
        this.clock = clock ?? (() => Environment.TickCount64);
        this.fault = fault;
        nextCheck = this.clock() + 60_000;
        try
        {
            var primary = ReadValid(this.path);
            if (primary != null) { current = saved = primary; return; }
            foreach (var backup in Backups())
            {
                var value = ReadValid(backup);
                if (value == null) continue;
                current = value;
                Commit(value); // Preserve corrupt primary; never rotate it into valid backups.
                saved = value;
                LoadNotice = "Recovered";
                return;
            }
            if (File.Exists(this.path)) LoadNotice = "ResetConfig";
        }
        catch (Exception ex) when (IsStorageError(ex))
        {
            // An unreadable file is not evidence that defaults may overwrite it.
            LoadBlocked = true;
            Error = ex.Message;
        }
    }

    public void Change(ConfigValues value)
    {
        lock (gate)
        {
            if (disposed) return;
            Validate(value);
            current = value;
            if (!LoadBlocked && current == saved) { Failures = 0; retryAt = 0; Error = null; }
        }
    }

    public void Tick()
    {
        lock (gate)
        {
            if (disposed || clock() < nextCheck) return;
            nextCheck = clock() + 60_000;
            FlushCore(false);
        }
    }

    public void Flush() { lock (gate) { if (!disposed) FlushCore(false); } }
    public void Retry()
    {
        lock (gate)
        {
            if (disposed || LoadBlocked) return;
            Failures = 0; retryAt = 0;
            FlushCore(true);
        }
    }

    private void FlushCore(bool explicitRetry)
    {
        if (writing || Stopped || current == saved || (!explicitRetry && clock() < retryAt)) return;
        var snapshot = current;
        writing = true;
        Attempts++;
        try
        {
            Commit(snapshot);
            saved = snapshot; // A reentrant/new change remains pending.
            Failures = 0; retryAt = 0; Error = null;
        }
        catch (Exception ex) when (IsStorageError(ex))
        {
            Failures++;
            retryAt = clock() + 60_000;
            Error = ex.Message;
        }
        finally { writing = false; }
    }

    private void Commit(ConfigValues snapshot)
    {
        Validate(snapshot);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(snapshot);
        string temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            fault?.Invoke("write");
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(bytes); PayloadBytes += bytes.Length; stream.Flush(true); }
            fault?.Invoke("validate");
            if (ReadValid(temp) != snapshot) throw new IOException("Temporary settings validation failed.");
            string? previous = null;
            if (File.Exists(path))
            {
                // Access failures propagate. Only malformed JSON is classified as corrupt.
                if (ReadValid(path) != null)
                {
                    long latest = Backups().Select(x => long.Parse(x[(x.LastIndexOf('.') + 1)..])).DefaultIfEmpty(0).Max();
                    if (latest == long.MaxValue) throw new IOException("Backup generation limit reached; preserve backups and rename them before retrying.");
                    long generation = latest + 1;
                    previous = path + ".bak." + generation.ToString("D19");
                }
                else previous = path + ".corrupt-" + Guid.NewGuid().ToString("N");
            }
            fault?.Invoke("replace");
            if (previous != null) File.Replace(temp, path, previous);
            else File.Move(temp, path);
            SaveCount++;
            // Pruning is post-commit only: interrupted/failed saves never rotate
            // the prior valid generations. Failed cleanup does not undo a commit.
            try
            {
                int valid = 0;
                foreach (var old in Backups())
                {
                    if (ReadValid(old) == null)
                        File.Move(old, old + ".corrupt-" + Guid.NewGuid().ToString("N"));
                    else if (++valid > 3) File.Delete(old);
                }
            }
            catch (Exception ex) when (IsStorageError(ex)) { LoadNotice = "BackupCleanup"; }
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); }
            catch (Exception ex) when (IsStorageError(ex)) { /* Preserve orphan; no retry loop. */ }
        }
    }

    private string[] Backups()
    {
        var dir = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(dir)) return [];
        return Directory.GetFiles(dir, Path.GetFileName(path) + ".bak.*")
            .Where(x => x[(x.LastIndexOf('.') + 1)..] is var suffix && suffix.Length == 19 && long.TryParse(suffix, out _))
            .OrderByDescending(x => x, StringComparer.Ordinal).ToArray();
    }

    private static ConfigValues? ReadValid(string file)
    {
        if (!File.Exists(file)) return null;
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > 65536) return null;
        try
        {
            using var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("Version", out var version) ||
                version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var v)) return null;
            // Unknown schemas may be valid newer data. Never replace them with
            // defaults or an older backup when this plugin is downgraded.
            if (v != 1) throw new IOException($"Unsupported settings version {v}; use the matching plugin version.");
            if (!root.TryGetProperty("Enabled", out var enabled) ||
                enabled.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return null;
            string? language = null;
            if (root.TryGetProperty("Language", out var lang) && lang.ValueKind != JsonValueKind.Null)
            {
                // A legacy/invalid language is uninitialized, not a reason to
                // discard the user's other settings (LOCALIZATION_SPEC).
                if (lang.ValueKind == JsonValueKind.String) language = Loc.Normalize(lang.GetString());
            }
            return new(v, language, enabled.GetBoolean());
        }
        catch (JsonException) { return null; }
    }

    private static void Validate(ConfigValues value)
    {
        if (value.Version != 1 || value.Language is not (null or "ja" or "en" or "de" or "fr" or "ko" or "zh-Hans" or "zh-Hant"))
            throw new ArgumentException("Invalid settings values.");
    }
    private static bool IsStorageError(Exception ex) => ex is IOException or UnauthorizedAccessException or System.Security.SecurityException;
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            // One final attempt on normal teardown, unless already stopped after
            // three failures. No background work/retry survives this boundary.
            try { FlushCore(true); }
            finally { disposed = true; }
        }
    }
}
