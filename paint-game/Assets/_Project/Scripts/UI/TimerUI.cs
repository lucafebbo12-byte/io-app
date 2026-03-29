// TimerUI.cs — displays remaining match time. Pulses when < 10 seconds.
using TMPro;
using UnityEngine;

namespace PaintGame
{
    public class TimerUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;

        private float _pulseTimer;

        public void UpdateTimer(float remaining)
        {
            if (_label == null) return;

            int secs = Mathf.CeilToInt(Mathf.Max(0f, remaining));
            _label.text = $"{secs / 60:D2}:{secs % 60:D2}";

            if (remaining <= 10f)
            {
                _pulseTimer += Time.deltaTime * 4f;
                float s = 1f + Mathf.Sin(_pulseTimer) * 0.12f;
                _label.transform.localScale = Vector3.one * s;
                _label.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(Time.time * 3f, 1f));
            }
            else
            {
                _label.transform.localScale = Vector3.one;
                _label.color = Color.white;
            }
        }
    }
}
