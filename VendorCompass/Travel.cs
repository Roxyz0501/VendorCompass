using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace VendorCompass;

internal sealed class Travel(IDalamudPluginInterface pi, IClientState client, IObjectTable objects,
    IAetheryteList destinations, ICondition condition, IPluginLog log)
{
    private readonly Dalamud.Plugin.Ipc.ICallGateSubscriber<bool> ready = pi.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
    private readonly Dalamud.Plugin.Ipc.ICallGateSubscriber<bool> running = pi.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");
    private readonly Dalamud.Plugin.Ipc.ICallGateSubscriber<Vector3, Vector3, bool, CancellationToken, Task<List<Vector3>>> find =
        pi.GetIpcSubscriber<Vector3, Vector3, bool, CancellationToken, Task<List<Vector3>>>("vnavmesh.Nav.PathfindCancelable");
    private readonly Dalamud.Plugin.Ipc.ICallGateSubscriber<List<Vector3>, bool, object> move = pi.GetIpcSubscriber<List<Vector3>, bool, object>("vnavmesh.Path.MoveTo");
    private readonly Dalamud.Plugin.Ipc.ICallGateSubscriber<object> stop = pi.GetIpcSubscriber<object>("vnavmesh.Path.Stop");
    private CancellationTokenSource? request;
    private Task<List<Vector3>>? path;
    private bool walking, awaitingTeleport;
    private uint origin;
    private long deadline, nextTick;
    public Vendor? Target { get; private set; }
    public string? Status { get; private set; }

    private bool UnsafeState => condition[ConditionFlag.InCombat] || condition[ConditionFlag.Unconscious]
        || condition[ConditionFlag.BoundByDuty] || condition[ConditionFlag.WatchingCutscene]
        || condition[ConditionFlag.OccupiedInCutSceneEvent] || condition[ConditionFlag.OccupiedInEvent];
    private bool Loading => condition[ConditionFlag.BetweenAreas] || condition[ConditionFlag.BetweenAreas51];

    public unsafe void Start(Vendor vendor)
    {
        if (Target != null) return;
        try
        {
            if (objects.LocalPlayer == null || UnsafeState || Loading) { Status = "Unavailable"; return; }
            if (!ready.HasFunction || !running.HasFunction || !find.HasFunction || !move.HasAction || !stop.HasAction)
            { Status = "NeedNav"; return; }
            if (running.InvokeFunc()) { Status = "Busy"; return; }
            origin = client.TerritoryType;
            awaitingTeleport = origin != vendor.Territory;
            if (awaitingTeleport)
            {
                var crystal = destinations.Where(d => d.TerritoryId == vendor.Territory && !d.IsSharedHouse && !d.IsApartment
                    && d.SubIndex == 0 && d.AetheryteData.ValueNullable is { IsAetheryte: true })
                    .OrderBy(d => d.GilCost).FirstOrDefault();
                if (crystal == null) { Status = "Locked"; return; }
                var telepo = Telepo.Instance();
                if (telepo == null || !telepo->Teleport(crystal.AetheryteId, crystal.SubIndex)) { Status = "Unavailable"; return; }
            }
            Target = vendor;
            Status = awaitingTeleport ? "Teleporting" : "Planning";
            deadline = Environment.TickCount64 + 90_000;
        }
        catch (Exception ex) { Fail(ex); }
    }

    public void Update()
    {
        if (Target is not { } goal || Environment.TickCount64 < nextTick) return;
        nextTick = Environment.TickCount64 + 200;
        try
        {
            if (nextTick > deadline || UnsafeState) { End("Interrupted"); return; }
            if (awaitingTeleport)
            {
                if (Loading || objects.LocalPlayer == null) return;
                if (client.TerritoryType == origin) return;
                awaitingTeleport = false;
            }
            if (client.TerritoryType != goal.Territory || Loading || objects.LocalPlayer is not { } player)
            { End("Interrupted"); return; }
            if (Vector3.Distance(player.Position, goal.Position) <= 3.5f)
            {
                var npc = objects.FirstOrDefault(o => o.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc &&
                    o.BaseId == goal.Npc && Vector3.Distance(o.Position, player.Position) <= 6);
                End(npc != null ? "Arrived" : "Absent");
                return;
            }
            if (walking)
            {
                if (!running.InvokeFunc()) End("NoPath");
                return;
            }
            if (!ready.InvokeFunc()) return;
            if (running.InvokeFunc()) { End("Busy"); return; }
            Status = "Planning";
            if (path == null)
            {
                request = new();
                path = find.InvokeFunc(player.Position, goal.Position, false, request.Token);
                // Observe a late failure even if this trip is cancelled before the next frame.
                _ = path.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                return;
            }
            if (!path.IsCompleted) return;
            var steps = path.GetAwaiter().GetResult();
            if (steps.Count == 0 || Vector3.Distance(steps[^1], goal.Position) > 3.5f)
            { End("NoPath"); return; }
            move.InvokeAction(steps, false);
            walking = true;
            Status = "Walking";
            deadline = Environment.TickCount64 + 600_000;
        }
        catch (Exception ex) { Fail(ex); }
    }
    private void Fail(Exception ex)
    {
        log.Warning(ex, "Vendor travel failed");
        End("Interrupted");
    }
    public void End(string? message)
    {
        if (walking)
            try { stop.InvokeAction(); } catch (Exception ex) { log.Warning(ex, "Stopping movement failed"); }
        request?.Cancel();
        request?.Dispose();
        request = null;
        path = null;
        walking = false;
        awaitingTeleport = false;
        Target = null;
        Status = message;
    }
}
