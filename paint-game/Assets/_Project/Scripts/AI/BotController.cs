// BotController.cs — state machine AI. Port of checkpoint-paint/server/Bot.js
// Writes directly into PlayerStats; PlayerController reads those values.
using System.Collections.Generic;
using UnityEngine;

namespace PaintGame
{
    public class BotController : MonoBehaviour
    {
        // ── State ──────────────────────────────────────────────────────────────
        private enum BotState { Roam, OrbitTarget, RushCheckpoint }
        private BotState _state = BotState.Roam;

        private PlayerController  _self;
        private PlayerController  _targetPlayer;
        private CheckpointController _targetCheckpoint;

        // Timing
        private float _retargetTimer;
        private float _changeDirTimer;
        private float _retargetInterval;

        // Roam direction
        private float _roamAngle;

        // Orbit
        private int   _orbitSign = 1;   // +1 or -1 for orbit direction
        private const float PREFERRED_ORBIT_DIST = 110f;
        private const float ORBIT_APPROACH_BAND  = 40f;  // within this of preferred = orbit tangentially

        // Edge avoidance
        private const float EDGE_MARGIN = 10 * GameConstants.TILE_SIZE;  // 80 units

        public void Init(PlayerController self)
        {
            _self         = self;
            _roamAngle    = Random.Range(0f, Mathf.PI * 2f);
            _orbitSign    = Random.value > 0.5f ? 1 : -1;
            _retargetInterval = Random.Range(1.5f, 3.0f);   // seconds
            Retarget();
        }

        // ── Logic tick (called by PlayerController.LogicStep) ─────────────────
        // BotController.Think() writes to PlayerStats; PlayerController then moves.
        public void Think(float dt, List<PlayerController> allPlayers,
                          List<CheckpointController> checkpoints)
        {
            if (_self == null || !_self.Stats.Alive) return;

            _retargetTimer  += dt;
            _changeDirTimer += dt;

            if (_retargetTimer >= _retargetInterval)
            {
                _retargetTimer    = 0f;
                _retargetInterval = Random.Range(1.5f, 3.5f);
                ChooseState(allPlayers, checkpoints);
            }

            Vector2 moveVec = ComputeMove(dt) + ComputeEdgeAvoidance();

            if (moveVec.sqrMagnitude > 0.001f)
            {
                moveVec = moveVec.normalized;
                _self.Stats.MoveDir  = moveVec;
                _self.Stats.AimAngle = Mathf.Atan2(moveVec.y, moveVec.x);
            }
            else
            {
                _self.Stats.MoveDir = Vector2.zero;
            }

            // Bots always spray
            _self.Stats.IsShooting   = true;
            _self.Stats.WantsToShoot = true;

            // Infinite ink for bots (no ink management)
            var stats = _self.Stats;
            typeof(PlayerStats)
                .GetField("_ink", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(stats, GameConstants.INK_MAX);
        }

        private void ChooseState(List<PlayerController> allPlayers,
                                  List<CheckpointController> checkpoints)
        {
            if (Random.value < 0.25f)
            {
                // Rush nearest enemy checkpoint
                _targetCheckpoint = FindNearestEnemyCheckpoint(checkpoints);
                _state = _targetCheckpoint != null ? BotState.RushCheckpoint : BotState.Roam;
            }
            else
            {
                // Orbit nearest player
                _targetPlayer = FindNearestPlayer(allPlayers);
                _state = _targetPlayer != null ? BotState.OrbitTarget : BotState.Roam;
            }
        }

        private void Retarget() => ChooseState(
            new List<PlayerController>(Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None)),
            new List<CheckpointController>(Object.FindObjectsByType<CheckpointController>(FindObjectsSortMode.None)));

        // ── Movement computation ───────────────────────────────────────────────
        private Vector2 ComputeMove(float dt)
        {
            Vector2 myPos = _self.Stats.WorldPos;

            switch (_state)
            {
                case BotState.Roam:
                    return ComputeRoam(dt);

                case BotState.OrbitTarget:
                    if (_targetPlayer == null || !_targetPlayer.Stats.Alive)
                        return ComputeRoam(dt);
                    return ComputeOrbit(myPos, _targetPlayer.Stats.WorldPos);

                case BotState.RushCheckpoint:
                    if (_targetCheckpoint == null || !_targetCheckpoint.Alive)
                        return ComputeRoam(dt);
                    Vector2 dir = (Vector2)_targetCheckpoint.transform.position - myPos;
                    return dir.sqrMagnitude > 1f ? dir.normalized : Vector2.zero;
            }
            return Vector2.zero;
        }

        private Vector2 ComputeRoam(float dt)
        {
            if (_changeDirTimer > Random.Range(2f, 5f))
            {
                _changeDirTimer = 0f;
                _roamAngle += Random.Range(-0.8f, 0.8f);
            }

            var dir = new Vector2(Mathf.Cos(_roamAngle), Mathf.Sin(_roamAngle));

            // Wall probe — if heading toward a wall, deflect early
            Vector2 probe = _self.Stats.WorldPos + dir * GameConstants.TILE_SIZE * 3f;
            var pt = GameConstants.WorldToTile(probe.x, probe.y);
            if (GameConstants.IsWall(pt.x, pt.y))
            {
                _roamAngle += Mathf.PI * 0.5f + Random.Range(-0.3f, 0.3f);
                _changeDirTimer = 0f;
                dir = new Vector2(Mathf.Cos(_roamAngle), Mathf.Sin(_roamAngle));
            }

            return dir;
        }

        private Vector2 ComputeOrbit(Vector2 myPos, Vector2 targetPos)
        {
            Vector2 toTarget = targetPos - myPos;
            float dist = toTarget.magnitude;
            if (dist < 0.1f) return ComputeRoam(0f);

            Vector2 towardsTarget = toTarget / dist;

            // Tangential (perpendicular) force for orbit
            Vector2 tangent = new Vector2(-towardsTarget.y, towardsTarget.x) * _orbitSign;

            float radialForce;
            if (dist < PREFERRED_ORBIT_DIST - ORBIT_APPROACH_BAND)
                radialForce = -1f;   // too close → back off
            else if (dist > PREFERRED_ORBIT_DIST + ORBIT_APPROACH_BAND)
                radialForce = 1f;    // too far → approach
            else
                radialForce = 0.2f;  // in band → mostly orbit

            return (towardsTarget * radialForce + tangent * 0.8f).normalized;
        }

        private Vector2 ComputeEdgeAvoidance()
        {
            Vector2 pos  = _self.Stats.WorldPos;
            Vector2 push = Vector2.zero;

            float left   = pos.x;
            float right  = GameConstants.WORLD_W - pos.x;
            float bottom = pos.y;
            float top    = GameConstants.WORLD_H - pos.y;

            if (left   < EDGE_MARGIN) push.x += EdgeFactor(left,   EDGE_MARGIN);
            if (right  < EDGE_MARGIN) push.x -= EdgeFactor(right,  EDGE_MARGIN);
            if (bottom < EDGE_MARGIN) push.y += EdgeFactor(bottom, EDGE_MARGIN);
            if (top    < EDGE_MARGIN) push.y -= EdgeFactor(top,    EDGE_MARGIN);

            return push;
        }

        private static float EdgeFactor(float dist, float margin)
        {
            float t = Mathf.Clamp01((margin - dist) / margin);
            return t * t;   // quadratic ease-in (matches JS version)
        }

        // ── Target finders ────────────────────────────────────────────────────
        private PlayerController FindNearestPlayer(List<PlayerController> players)
        {
            Vector2 myPos = _self.Stats.WorldPos;
            PlayerController best = null;
            float bestDist = float.MaxValue;

            foreach (var p in players)
            {
                if (p == _self || !p.Stats.Alive) continue;
                float d = Vector2.Distance(myPos, p.Stats.WorldPos);
                if (d < bestDist) { bestDist = d; best = p; }
            }
            return best;
        }

        private CheckpointController FindNearestEnemyCheckpoint(
            List<CheckpointController> checkpoints)
        {
            Vector2 myPos = _self.Stats.WorldPos;
            CheckpointController best = null;
            float bestDist = float.MaxValue;

            foreach (var cp in checkpoints)
            {
                if (!cp.Alive || cp.OwnerIndex == _self.Stats.OwnerIndex) continue;
                float d = Vector2.Distance(myPos, cp.transform.position);
                if (d < bestDist) { bestDist = d; best = cp; }
            }
            return best;
        }
    }
}
