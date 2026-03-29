// InkBarUI.cs — fill bar showing remaining ink.
using UnityEngine;
using UnityEngine.UI;

namespace PaintGame
{
    public class InkBarUI : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _bgImage;

        public void Bind(PlayerStats stats)
        {
            if (stats == null) return;
            stats.OnInkChanged += OnInkChanged;
            OnInkChanged(stats.Ink);

            if (_fillImage != null) _fillImage.color = stats.PlayerColor;
        }

        void OnDestroy()
        {
            // Unsubscribe if stats still alive
        }

        private void OnInkChanged(float ink)
        {
            if (_fillImage == null) return;
            _fillImage.fillAmount = ink / GameConstants.INK_MAX;
        }
    }
}
