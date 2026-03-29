// HUDController.cs — wires game events to UI sub-components.
using UnityEngine;

namespace PaintGame
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private TimerUI      _timerUI;
        [SerializeField] private InkBarUI     _inkBarUI;
        [SerializeField] private HPDisplayUI  _hpUI;
        [SerializeField] private ScoreboardUI _scoreboardUI;
        [SerializeField] private CountdownUI  _countdownUI;
        [SerializeField] private KillFeedUI   _killFeedUI;
        [SerializeField] private GameObject   _mobileControls;

        void OnEnable()
        {
            GameEvents.OnTimerTick       += OnTimerTick;
            GameEvents.OnCountdown       += OnCountdown;
            GameEvents.OnMatchEnd        += OnMatchEnd;
            GameEvents.OnPlayerKill      += OnKill;
            GameEvents.OnPlayerEliminated += OnEliminated;
        }

        void OnDisable()
        {
            GameEvents.OnTimerTick       -= OnTimerTick;
            GameEvents.OnCountdown       -= OnCountdown;
            GameEvents.OnMatchEnd        -= OnMatchEnd;
            GameEvents.OnPlayerKill      -= OnKill;
            GameEvents.OnPlayerEliminated -= OnEliminated;
        }

        void Start()
        {
            // Show mobile controls only on mobile
            if (_mobileControls != null)
                _mobileControls.SetActive(Application.isMobilePlatform);

            // Bind human player stats after spawn
            // PlayerSpawnManager.Players is populated in Start(), so wait one frame
            StartCoroutine(BindPlayerStats());
        }

        private System.Collections.IEnumerator BindPlayerStats()
        {
            yield return null;  // wait one frame for PlayerSpawnManager.Start()

            if (PlayerSpawnManager.Players != null && PlayerSpawnManager.Players.Length > 0)
            {
                var humanStats = PlayerSpawnManager.Players[0].Stats;
                _inkBarUI?.Bind(humanStats);
                _hpUI?.Bind(humanStats);
            }

            _scoreboardUI?.Init(GameManager.Instance.ScoreTracker);
        }

        private void OnTimerTick(float remaining) => _timerUI?.UpdateTimer(remaining);
        private void OnCountdown(int count)        => _countdownUI?.ShowCount(count);
        private void OnMatchEnd(PlayerController w) { /* WinScreen handles this */ }
        private void OnKill(PlayerController killer, PlayerController victim)
            => _killFeedUI?.AddEntry(killer, victim);
        private void OnEliminated(PlayerController p)
            => _killFeedUI?.AddEliminatedEntry(p);
    }
}
