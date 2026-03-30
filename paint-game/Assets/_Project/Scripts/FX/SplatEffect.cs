// SplatEffect.cs — pooled splat sprite: scale 0.2→1.8, alpha 0.7→0 in 90ms.
using System;
using System.Collections;
using UnityEngine;

namespace PaintGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SplatEffect : MonoBehaviour
    {
        private SpriteRenderer _sr;

        void Awake() => _sr = GetComponent<SpriteRenderer>();

        public void Play(Vector2 pos, Color color, Action onComplete)
        {
            transform.position = new Vector3(pos.x, pos.y, 0f);
            _sr.color = new Color(color.r, color.g, color.b, 0.7f);
            float size = GameConstants.TILE_SIZE * UnityEngine.Random.Range(1.5f, 3f);
            transform.localScale = Vector3.one * 0.2f * size;
            StartCoroutine(Animate(size, onComplete));
        }

        private IEnumerator Animate(float targetSize, Action onComplete)
        {
            float t = 0f;
            float dur = 0.09f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;
                float scale = Mathf.Lerp(0.2f, 1.8f, p) * targetSize;
                transform.localScale = Vector3.one * scale;
                float alpha = Mathf.Lerp(0.7f, 0f, p);
                _sr.color = new Color(_sr.color.r, _sr.color.g, _sr.color.b, alpha);
                yield return null;
            }
            onComplete?.Invoke();
        }
    }
}
