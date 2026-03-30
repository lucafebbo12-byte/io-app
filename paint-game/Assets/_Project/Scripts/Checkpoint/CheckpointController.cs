// CheckpointController.cs — tracks HP, reacts to dirty tiles each logic tick.
// Ported from checkpoint-paint/server/Checkpoint.js
using System.Collections.Generic;
using UnityEngine;

namespace PaintGame
{
    public class CheckpointController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private byte _ownerIndex = 1;
        public byte OwnerIndex
        {
            get => _ownerIndex;
            set => _ownerIndex = value;
        }

        private int  _hp;
        private bool _alive = true;
        public  bool Alive => _alive;
        public  float HPPercent => (float)_hp / GameConstants.CHECKPOINT_HP;

        private CheckpointVisuals _visuals;
        private PlayerController  _linkedPlayer;

        public void Init(byte ownerIndex, PlayerController player)
        {
            OwnerIndex    = ownerIndex;
            _hp           = GameConstants.CHECKPOINT_HP;
            _linkedPlayer = player;
            _visuals      = GetComponent<CheckpointVisuals>();
            _visuals?.Init(ownerIndex, GameConstants.PLAYER_COLORS[ownerIndex]);
        }

        // ── Called by MatchManager after FlushDirty() ────────────────────────
        public void CheckDamage(List<PaintedTile> dirtyTiles)
        {
            if (!_alive) return;

            var myTile = GameConstants.WorldToTile(transform.position.x, transform.position.y);

            int damage = 0;
            int heal   = 0;

            foreach (var t in dirtyTiles)
            {
                int dx = Mathf.Abs(t.tx - myTile.x);
                int dy = Mathf.Abs(t.ty - myTile.y);

                // Chebyshev distance check
                if (dx > GameConstants.CHECKPOINT_RADIUS || dy > GameConstants.CHECKPOINT_RADIUS)
                    continue;

                if (t.ownerIndex != OwnerIndex && t.ownerIndex != GameConstants.OWNER_NEUTRAL)
                    damage++;
                else if (t.ownerIndex == OwnerIndex)
                    heal++;
            }

            _hp = Mathf.Clamp(_hp - damage + heal, 0, GameConstants.CHECKPOINT_HP);

            _visuals?.UpdateHP(HPPercent);
            GameEvents.RaiseCheckpointDamaged(this, HPPercent);

            if (_hp <= 0) Destroy_();
        }

        private void Destroy_()
        {
            _alive = false;
            _visuals?.PlayDestroyAnim(() => gameObject.SetActive(false));
            GameEvents.RaiseCheckpointDestroyed(this);
            _linkedPlayer?.OnCheckpointDestroyed();
        }
    }
}
