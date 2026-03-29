// HPDisplayUI.cs — 3 heart/dot icons reflecting HP.
using UnityEngine;
using UnityEngine.UI;

namespace PaintGame
{
    public class HPDisplayUI : MonoBehaviour
    {
        [SerializeField] private Image[] _hearts;  // 3 elements

        public void Bind(PlayerStats stats)
        {
            if (stats == null) return;
            stats.OnHPChanged += UpdateHP;
            UpdateHP(stats.HP);
        }

        private void UpdateHP(int hp)
        {
            for (int i = 0; i < _hearts.Length; i++)
                if (_hearts[i] != null)
                    _hearts[i].color = i < hp ? Color.white : new Color(1f, 1f, 1f, 0.2f);
        }
    }
}
