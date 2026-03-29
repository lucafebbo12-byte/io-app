// WinScreenUI.cs — shown when the match ends. Displays winner and final scores.
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PaintGame
{
    public class WinScreenUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _winnerLabel;
        [SerializeField] private Image           _bgOverlay;
        [SerializeField] private Transform       _scoreContainer;
        [SerializeField] private ScoreRowUI      _rowPrefab;
        [SerializeField] private Button          _playAgainBtn;
        [SerializeField] private Button          _menuBtn;

        void Awake()
        {
            gameObject.SetActive(false);
            GameEvents.OnMatchEnd += Show;

            if (_playAgainBtn != null) _playAgainBtn.onClick.AddListener(PlayAgain);
            if (_menuBtn      != null) _menuBtn.onClick.AddListener(GoMenu);
        }

        void OnDestroy() => GameEvents.OnMatchEnd -= Show;

        private void Show(PlayerController winner)
        {
            gameObject.SetActive(true);
            StartCoroutine(ShowAnim(winner));
        }

        private IEnumerator ShowAnim(PlayerController winner)
        {
            // Fade in overlay
            if (_bgOverlay != null)
            {
                Color winColor = winner != null ? winner.Stats.PlayerColor : Color.white;
                _bgOverlay.color = new Color(winColor.r, winColor.g, winColor.b, 0f);
                float t = 0f;
                while (t < 0.5f)
                {
                    t += Time.deltaTime;
                    _bgOverlay.color = new Color(winColor.r, winColor.g, winColor.b, t / 0.5f * 0.6f);
                    yield return null;
                }
            }

            // Winner text
            if (_winnerLabel != null)
            {
                _winnerLabel.text = winner != null
                    ? $"<color=#{ColorUtility.ToHtmlStringRGB(winner.Stats.PlayerColor)}>" +
                      $"{winner.Stats.PlayerName}</color> wins!"
                    : "Draw!";
            }

            // Final scores
            var tracker = GameManager.Instance.ScoreTracker;
            if (tracker != null && _scoreContainer != null && _rowPrefab != null)
            {
                foreach (var entry in tracker.Scores)
                {
                    var row = Instantiate(_rowPrefab, _scoreContainer);
                    row.Set(0, entry.Player.Stats.PlayerName,
                              entry.Player.Stats.PlayerColor, entry.Percentage);
                }
            }
        }

        private void PlayAgain() => SceneManager.LoadScene("Game");
        private void GoMenu()    => SceneManager.LoadScene("MainMenu");
    }
}
