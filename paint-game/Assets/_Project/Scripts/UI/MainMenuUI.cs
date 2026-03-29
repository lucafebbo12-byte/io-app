// MainMenuUI.cs — class-select main menu.
// Attach to a Canvas in MainMenu.unity.
// Assign _gameSession (GameSession.asset), two panel highlights, and Play button.
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PaintGame
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Session")]
        [SerializeField] private GameSessionSO _gameSession;

        [Header("Class Select Buttons")]
        [SerializeField] private Button _shotgunBtn;
        [SerializeField] private Button _akBtn;

        [Header("Selection Indicator (active image on selected card)")]
        [SerializeField] private GameObject _shotgunSelected;
        [SerializeField] private GameObject _akSelected;

        [Header("Class Info Panel")]
        [SerializeField] private TMP_Text _classNameText;
        [SerializeField] private TMP_Text _classDescText;

        [Header("Play Button")]
        [SerializeField] private Button _playBtn;

        [Header("Scene name to load")]
        [SerializeField] private string _gameSceneName = "Game";

        // ── Descriptions ──────────────────────────────────────────────────────
        private static readonly string[] _names = { "SHOTGUN", "AK / WATER-GUN" };
        private static readonly string[] _descs =
        {
            "Wide spray · Short range · Fast move\nClose-range domination",
            "Precise beam · Long range · Slower\nPick off enemies from distance"
        };

        void Start()
        {
            _shotgunBtn?.onClick.AddListener(() => SelectWeapon(0));
            _akBtn     ?.onClick.AddListener(() => SelectWeapon(1));
            _playBtn   ?.onClick.AddListener(OnPlay);

            // Restore last selection
            RefreshUI(_gameSession != null ? _gameSession.selectedWeaponIndex : 0);
        }

        private void SelectWeapon(int index)
        {
            if (_gameSession != null)
                _gameSession.selectedWeaponIndex = index;
            RefreshUI(index);
        }

        private void RefreshUI(int index)
        {
            if (_shotgunSelected) _shotgunSelected.SetActive(index == 0);
            if (_akSelected)      _akSelected     .SetActive(index == 1);

            if (_classNameText) _classNameText.text = _names[index];
            if (_classDescText) _classDescText.text = _descs[index];
        }

        private void OnPlay()
        {
            // Write selection to static field for PlayerSpawnManager
            PlayerSpawnManager.SelectedWeaponIndex =
                _gameSession != null ? _gameSession.selectedWeaponIndex : 0;

            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene(_gameSceneName);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(_gameSceneName);
        }
    }
}
