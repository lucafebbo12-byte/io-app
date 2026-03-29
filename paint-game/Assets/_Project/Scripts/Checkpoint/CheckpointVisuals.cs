// CheckpointVisuals.cs — HP arc ring + star icon pulsing.
using System;
using System.Collections;
using UnityEngine;

namespace PaintGame
{
    public class CheckpointVisuals : MonoBehaviour
    {
        [SerializeField] private LineRenderer _hpRing;
        [SerializeField] private SpriteRenderer _starIcon;
        [SerializeField] private SpriteRenderer _baseIcon;

        private Color  _playerColor;
        private float  _pulseTimer;
        private const int RING_SEGMENTS = 48;

        public void Init(byte ownerIndex, Color color)
        {
            _playerColor = color;

            if (_baseIcon != null) _baseIcon.color = new Color(color.r, color.g, color.b, 0.4f);
            if (_starIcon != null) _starIcon.color = color;

            if (_hpRing != null)
            {
                _hpRing.positionCount  = RING_SEGMENTS + 1;
                _hpRing.startColor     = color;
                _hpRing.endColor       = color;
                _hpRing.startWidth     = 1.5f;
                _hpRing.endWidth       = 1.5f;
                _hpRing.loop           = false;
                _hpRing.useWorldSpace  = false;
                DrawRing(1f);
            }
        }

        public void UpdateHP(float hpPercent)
        {
            if (_hpRing == null) return;

            // Color shift: green → orange → red as HP drops
            Color ringColor = hpPercent > 0.5f
                ? Color.Lerp(new Color(1f, 0.6f, 0f), _playerColor, (hpPercent - 0.5f) * 2f)
                : Color.Lerp(Color.red, new Color(1f, 0.6f, 0f), hpPercent * 2f);

            _hpRing.startColor = ringColor;
            _hpRing.endColor   = ringColor;
            DrawRing(hpPercent);
        }

        private void DrawRing(float fraction)
        {
            float radius  = GameConstants.CHECKPOINT_RADIUS * GameConstants.TILE_SIZE;
            int   count   = Mathf.Max(2, Mathf.RoundToInt(fraction * RING_SEGMENTS));
            _hpRing.positionCount = count + 1;

            for (int i = 0; i <= count; i++)
            {
                float angle = Mathf.Lerp(-Mathf.PI * 0.5f,
                                          -Mathf.PI * 0.5f + 2f * Mathf.PI * fraction,
                                          (float)i / count);
                _hpRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius,
                                                   Mathf.Sin(angle) * radius, 0f));
            }
        }

        void Update()
        {
            if (_starIcon == null) return;
            _pulseTimer += Time.deltaTime * 2f;
            float scale = 1f + Mathf.Sin(_pulseTimer) * 0.08f;
            _starIcon.transform.localScale = Vector3.one * scale;
        }

        public void PlayDestroyAnim(Action onComplete)
        {
            StartCoroutine(DestroyAnim(onComplete));
        }

        private IEnumerator DestroyAnim(Action onComplete)
        {
            float t = 0f;
            var startScale = transform.localScale;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float s = Mathf.Lerp(1f, 2.5f, t / 0.4f);
                transform.localScale = startScale * s;
                var sr = GetComponent<SpriteRenderer>();
                if (sr) sr.color = new Color(1f, 1f, 1f, 1f - t / 0.4f);
                yield return null;
            }
            onComplete?.Invoke();
        }
    }
}
