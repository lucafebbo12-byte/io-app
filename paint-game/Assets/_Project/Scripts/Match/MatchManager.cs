// MatchManager.cs — drives the 20 Hz logic tick and match flow.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PaintGame
{
    public class MatchManager : MonoBehaviour
    {
        // ── State ──────────────────────────────────────────────────────────────
        public enum MatchState { Countdown, Playing, Ended }
        public MatchState State { get; private set; } = MatchState.Countdown;

        private float _remainingTime;
        private float _logicAccum;

        // Cached every tick
        private List<PlayerController>    _players;
        private List<CheckpointController> _checkpoints;
        private List<BulletProjectile>     _activeBullets = new List<BulletProjectile>(64);

        private TerritoryMap     _map;
        private TerritoryRenderer _renderer;
        private ScoreTracker      _scorer;

        void Start()
        {
            _map        = GameManager.Instance.TerritoryMap;
            _renderer   = GameManager.Instance.TerritoryRenderer;
            _scorer     = GameManager.Instance.ScoreTracker;
            _remainingTime = GameConstants.ROUND_TIME;

            StartCoroutine(CountdownSequence());
        }

        // ── Countdown 3-2-1-GO ────────────────────────────────────────────────
        private IEnumerator CountdownSequence()
        {
            State = MatchState.Countdown;
            for (int i = 3; i >= 0; i--)
            {
                GameEvents.RaiseCountdown(i);
                yield return new WaitForSeconds(1f);
            }
            State = MatchState.Playing;
            GameEvents.RaiseMatchStarted();
        }

        // ── Main update ───────────────────────────────────────────────────────
        void Update()
        {
            if (State != MatchState.Playing) return;

            // Accumulate time and run fixed-rate logic ticks
            _logicAccum += Time.deltaTime;
            while (_logicAccum >= GameConstants.LOGIC_TICK_INTERVAL)
            {
                _logicAccum -= GameConstants.LOGIC_TICK_INTERVAL;
                LogicTick(GameConstants.LOGIC_TICK_INTERVAL);
            }

            // Timer
            _remainingTime -= Time.deltaTime;
            GameEvents.RaiseTimerTick(_remainingTime);

            if (_remainingTime <= 0f)
                EndMatch();
        }

        // ── 20 Hz Logic Tick ──────────────────────────────────────────────────
        private void LogicTick(float dt)
        {
            if (_players == null) CollectPlayers();

            // 1. Player / bot logic
            foreach (var p in _players)
            {
                p.SetCheckpointsCache(_checkpoints);
                p.LogicStep(dt, _players);
            }

            // 2. Bullet logic
            for (int i = _activeBullets.Count - 1; i >= 0; i--)
            {
                bool done = _activeBullets[i].LogicStep(dt, _players);
                if (done) _activeBullets.RemoveAt(i);
            }

            // 3. Territory flush
            var dirty = _map.FlushDirty();
            if (dirty.Count > 0)
            {
                _renderer.QueueTiles(dirty);

                // 4. Checkpoint damage
                foreach (var cp in _checkpoints)
                    cp.CheckDamage(dirty);

                GameEvents.RaiseTilesPainted(dirty);
            }

            // 5. Scoring
            _scorer?.UpdateScores(_players);

            // 6. Win condition check
            CheckWin();
        }

        // ── Win condition ─────────────────────────────────────────────────────
        private void CheckWin()
        {
            if (State != MatchState.Playing) return;

            // Count alive players (checkpoint alive OR still alive)
            int aliveCount = 0;
            PlayerController lastAlive = null;
            foreach (var p in _players)
            {
                if (p.Stats.Alive || p.Stats.CheckpointAlive)
                {
                    aliveCount++;
                    lastAlive = p;
                }
            }

            if (aliveCount <= 1)
                EndMatch(lastAlive);
        }

        private void EndMatch(PlayerController forceWinner = null)
        {
            if (State == MatchState.Ended) return;
            State = MatchState.Ended;

            PlayerController winner = forceWinner ?? GetTerritoryWinner();
            GameEvents.RaiseMatchEnd(winner);
        }

        private PlayerController GetTerritoryWinner()
        {
            if (_players == null) return null;
            PlayerController best = null;
            int bestTiles = -1;
            foreach (var p in _players)
            {
                int tiles = _map.CountTiles(p.Stats.OwnerIndex);
                if (tiles > bestTiles) { bestTiles = tiles; best = p; }
            }
            return best;
        }

        // ── Bullet registration ───────────────────────────────────────────────
        public void RegisterBullet(BulletProjectile b) => _activeBullets.Add(b);

        // ── Helpers ───────────────────────────────────────────────────────────
        private void CollectPlayers()
        {
            _players     = new List<PlayerController>(
                Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None));
            _checkpoints = new List<CheckpointController>(
                Object.FindObjectsByType<CheckpointController>(FindObjectsSortMode.None));
        }
    }
}
