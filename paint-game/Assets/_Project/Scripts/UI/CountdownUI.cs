// CountdownUI.cs — 3-2-1-GO display.
using System.Collections;
using TMPro;
using UnityEngine;

namespace PaintGame
{
    public class CountdownUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;

        public void ShowCount(int count)
        {
            if (_label == null) return;
            StopAllCoroutines();

            if (count == 0)
            {
                _label.text = "GO!";
                _label.color = Color.yellow;
                StartCoroutine(FadeOut(0.6f));
            }
            else
            {
                _label.text  = count.ToString();
                _label.color = Color.white;
                _label.gameObject.SetActive(true);
                StartCoroutine(Pulse());
            }
        }

        private IEnumerator Pulse()
        {
            transform.localScale = Vector3.one * 1.6f;
            float t = 0f;
            while (t < 0.8f)
            {
                t += Time.deltaTime;
                float s = Mathf.Lerp(1.6f, 1f, t / 0.8f);
                transform.localScale = Vector3.one * s;
                yield return null;
            }
        }

        private IEnumerator FadeOut(float delay)
        {
            yield return new WaitForSeconds(delay);
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                _label.color = new Color(1f, 1f, 0f, 1f - t / 0.3f);
                yield return null;
            }
            _label.gameObject.SetActive(false);
        }
    }
}
