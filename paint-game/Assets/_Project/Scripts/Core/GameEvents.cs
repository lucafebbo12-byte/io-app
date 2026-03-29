// GameEvents.cs — lightweight static event bus. No ScriptableObject overhead for
// events that fire every frame. UI and FX subscribe; systems raise.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaintGame
{
    /// <summary>Raised when tiles change ownership this logic tick.</summary>
    public static class GameEvents
    {
        // ── Territory ─────────────────────────────────────────────────────────
        public static event Action<List<PaintedTile>> OnTilesPainted;

        // ── Player ────────────────────────────────────────────────────────────
        public static event Action<PlayerController>              OnPlayerDied;
        public static event Action<PlayerController>              OnPlayerRespawned;
        public static event Action<PlayerController>              OnPlayerEliminated;
        public static event Action<PlayerController, PlayerController> OnPlayerKill; // killer, victim

        // ── Checkpoint ────────────────────────────────────────────────────────
        public static event Action<CheckpointController, float>  OnCheckpointDamaged;  // float = hpPercent
        public static event Action<CheckpointController>         OnCheckpointDestroyed;

        // ── Match ──────────────────────────────────────────────────────────────
        public static event Action<float>           OnTimerTick;       // remaining seconds
        public static event Action<int>             OnCountdown;       // 3,2,1,0 (GO)
        public static event Action<PlayerController> OnMatchEnd;       // winner (null = draw)
        public static event Action                  OnMatchStarted;

        // ── Raise helpers ──────────────────────────────────────────────────────
        public static void RaiseTilesPainted(List<PaintedTile> tiles) => OnTilesPainted?.Invoke(tiles);
        public static void RaisePlayerDied(PlayerController p)        => OnPlayerDied?.Invoke(p);
        public static void RaisePlayerRespawned(PlayerController p)   => OnPlayerRespawned?.Invoke(p);
        public static void RaisePlayerEliminated(PlayerController p)  => OnPlayerEliminated?.Invoke(p);
        public static void RaisePlayerKill(PlayerController killer, PlayerController victim)
            => OnPlayerKill?.Invoke(killer, victim);
        public static void RaiseCheckpointDamaged(CheckpointController c, float hp)
            => OnCheckpointDamaged?.Invoke(c, hp);
        public static void RaiseCheckpointDestroyed(CheckpointController c)
            => OnCheckpointDestroyed?.Invoke(c);
        public static void RaiseTimerTick(float remaining) => OnTimerTick?.Invoke(remaining);
        public static void RaiseCountdown(int count)       => OnCountdown?.Invoke(count);
        public static void RaiseMatchEnd(PlayerController winner) => OnMatchEnd?.Invoke(winner);
        public static void RaiseMatchStarted()             => OnMatchStarted?.Invoke();

        /// <summary>Clears all subscribers — call on scene unload.</summary>
        public static void ClearAll()
        {
            OnTilesPainted     = null;
            OnPlayerDied       = null;
            OnPlayerRespawned  = null;
            OnPlayerEliminated = null;
            OnPlayerKill       = null;
            OnCheckpointDamaged    = null;
            OnCheckpointDestroyed  = null;
            OnTimerTick   = null;
            OnCountdown   = null;
            OnMatchEnd    = null;
            OnMatchStarted = null;
        }
    }

    /// <summary>One tile ownership change, produced by TerritoryMap.FlushDirty().</summary>
    public struct PaintedTile
    {
        public int  tx, ty;
        public byte ownerIndex;
        public bool wasWall;

        public PaintedTile(int tx, int ty, byte owner)
        {
            this.tx = tx; this.ty = ty; this.ownerIndex = owner; this.wasWall = false;
        }
    }
}
