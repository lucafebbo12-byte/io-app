// ImpactFlash.cs — white ring that expands and fades on bullet impact.
using System;
using System.Collections;
using UnityEngine;

namespace PaintGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ImpactFlash : MonoBehaviour
    {
        private SpriteRenderer _sr;
        void Awake() => _sr = GetComponent<SpriteRenderer>();

        public void Play(Vector2 pos, Color color, Action onComplete)
        {
            transform.position   = new Vector3(pos.x, pos.y, 0f);
            transform.localScale = Vector3.one * 4f;
            _sr.color = new Color(1f, 1f, 1f, 0.9f);
            StartCoroutine(Animate(onComplete));
        }

        private IEnumerator Animate(Action onComplete)
        {
            float t = 0f, dur = 0.18f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;
                transform.localScale = Vector3.one * Mathf.Lerp(4f, 24f, p);
                _sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.9f, 0f, p));
                yield return null;
            }
            onComplete?.Invoke();
        }
    }
}
