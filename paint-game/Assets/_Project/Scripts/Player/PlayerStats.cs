// PlayerStats.cs — pure data container for a player/bot.
// No game logic here — just state accessed by other components.
using UnityEngine;

namespace PaintGame
{
    public class PlayerStats : MonoBehaviour
    {
        // ── Identity ──────────────────────────────────────────────────────────
        public byte  OwnerIndex   { get; set; }
        public Color PlayerColor  { get; set; }
        public bool  IsHuman      { get; set; }
        public string PlayerName  { get; set; }

        // ── Position / Aim ────────────────────────────────────────────────────
        public Vector2 WorldPos   { get; set; }
        public float   AimAngle   { get; set; }   // radians

        // ── Input flags ───────────────────────────────────────────────────────
        public Vector2 MoveDir    { get; set; }
        public bool    IsShooting { get; set; }
        public bool    WantsToShoot { get; set; }

        // ── HP ────────────────────────────────────────────────────────────────
        private int _hp;
        public  int HP
        {
            get => _hp;
            set { _hp = Mathf.Clamp(value, 0, GameConstants.PLAYER_MAX_HP); OnHPChanged?.Invoke(_hp); }
        }
        public bool Alive { get; private set; } = true;
        public bool CheckpointAlive { get; set; } = true;

        public System.Action<int> OnHPChanged;

        // ── Ink ────────────────────────────────────────────────────────────────
        private float _ink = GameConstants.INK_MAX;
        public  float Ink => _ink;
        public void DrainInk(float amount)
        {
            _ink = Mathf.Max(0f, _ink - amount);
            OnInkChanged?.Invoke(_ink);
        }
        public void RefillInk(float amount)
        {
            _ink = Mathf.Min(GameConstants.INK_MAX, _ink + amount);
            OnInkChanged?.Invoke(_ink);
        }

        public System.Action<float> OnInkChanged;

        // ── Zone state → speed/ink multipliers ────────────────────────────────
        public enum ZoneType { Neutral, Own, Enemy }
        public ZoneType CurrentZone { get; set; }

        public float SpeedMultiplier =>
            CurrentZone == ZoneType.Own   ? GameConstants.ZONE_SPEED_OWN :
            CurrentZone == ZoneType.Enemy ? GameConstants.ZONE_SPEED_ENEMY : 1f;

        public float DrainMultiplier =>
            CurrentZone == ZoneType.Enemy ? 1.5f : 1f;

        public float RefillMultiplier =>
            CurrentZone == ZoneType.Own ? 1.5f : 0.4f;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        public void SetAlive(bool value)
        {
            Alive = value;
            if (!value) { IsShooting = false; WantsToShoot = false; }
        }

        public void ResetForRespawn()
        {
            HP   = GameConstants.PLAYER_MAX_HP;
            _ink = GameConstants.INK_MAX;
            SetAlive(true);
        }

        void Awake()
        {
            HP = GameConstants.PLAYER_MAX_HP;
        }
    }
}
