// ScoreRowUI.cs — single row in the scoreboard.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaintGame
{
    public class ScoreRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _rankLabel;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _pctLabel;
        [SerializeField] private Image           _colorDot;
        [SerializeField] private Image           _bar;

        public void Set(int rank, string playerName, Color color, float pct)
        {
            if (_rankLabel != null) _rankLabel.text = $"{rank}.";
            if (_nameLabel != null) _nameLabel.text = playerName;
            if (_pctLabel  != null) _pctLabel.text  = $"{pct:F1}%";
            if (_colorDot  != null) _colorDot.color  = color;
            if (_bar       != null) { _bar.fillAmount = pct / 100f; _bar.color = color; }
        }
    }
}
