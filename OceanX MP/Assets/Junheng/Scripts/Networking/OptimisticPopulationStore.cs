using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Client-side optimistic overlay for species school counts, keyed by netcode species index.
///
/// The host doesn't lower its count until a removed school has finished swimming out, so a raw
/// read of the synced count lags a few seconds behind a Remove tap. This store remembers the
/// player's not-yet-confirmed taps and reports (authoritative + pending) so every tablet UI shows
/// an instant, consistent number.
///
/// Crucially the pending state lives HERE — one entry per species index — instead of on a UI card,
/// so it survives panel switches and card recreation (that is what previously made a removed
/// organism "snap back" to its old count after leaving and re-opening the panel).
///
/// As the host's authoritative count catches up in the same direction as the tap, the matching
/// pending amount is consumed, so the number holds steady and finally lands exactly on the host's
/// value (no double-count, no snap-back). Movement the OTHER way (e.g. an automatic population
/// tick) is tracked but not counted against the player's intent.
/// </summary>
public static class OptimisticPopulationStore
{
    private static readonly Dictionary<int, int> _pending  = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> _lastAuth = new Dictionary<int, int>();

    // Static state must be cleared on each play session — otherwise, with Domain Reload disabled,
    // pending taps from a previous run would leak into the next.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _pending.Clear();
        _lastAuth.Clear();
    }

    /// <summary>Record a tapped +1 (add) or -1 (remove) that the host hasn't confirmed yet.</summary>
    public static void RegisterDelta(int index, int delta)
    {
        if (index < 0) return;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (!_lastAuth.ContainsKey(index))
            _lastAuth[index] = net != null ? net.GetPopulation(index) : 0;
        _pending[index] = (_pending.TryGetValue(index, out int d) ? d : 0) + delta;
    }

    /// <summary>
    /// The count to display for a species: authoritative synced count + not-yet-confirmed taps,
    /// clamped to [0, max]. Reconciles as a side effect, so calling it every frame is what keeps
    /// the pending amount settling onto the host's real value.
    /// </summary>
    public static int Display(int index)
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null || index < 0) return 0;
        Reconcile(index, net);
        int auth = net.GetPopulation(index);
        int pending = _pending.TryGetValue(index, out int d) ? d : 0;
        int max = net.GetMaxSchools(index);
        int hi = max > 0 ? max : int.MaxValue;
        return Mathf.Clamp(auth + pending, 0, hi);
    }

    private static void Reconcile(int index, EcosystemNetworkManagerGPU net)
    {
        if (!_pending.TryGetValue(index, out int pending)) return;

        int auth = net.GetPopulation(index);
        int last = _lastAuth.TryGetValue(index, out int la) ? la : auth;
        int move = auth - last;
        if (move != 0)
        {
            if (pending > 0 && move > 0)      pending -= Mathf.Min(pending, move);
            else if (pending < 0 && move < 0) pending -= Mathf.Max(pending, move);
            _lastAuth[index] = auth;
        }

        // Snap-back guard: never show outside [0, max] if the host can't satisfy the request.
        int max = net.GetMaxSchools(index);
        int hi = max > 0 ? max : int.MaxValue;
        int disp = auth + pending;
        if (disp > hi)      pending = hi - auth;
        else if (disp < 0)  pending = -auth;

        if (pending == 0) { _pending.Remove(index); _lastAuth.Remove(index); }
        else _pending[index] = pending;
    }
}
