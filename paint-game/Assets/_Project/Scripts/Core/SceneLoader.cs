// SceneLoader.cs — transition helper. Call from UI buttons.
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaintGame
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _fadeGroup;

        public static SceneLoader Instance { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void LoadScene(string name) => StartCoroutine(FadeLoad(name));

        private IEnumerator FadeLoad(string name)
        {
            if (_fadeGroup != null)
            {
                float t = 0f;
                while (t < 0.3f)
                {
                    t += Time.deltaTime;
                    _fadeGroup.alpha = t / 0.3f;
                    yield return null;
                }
            }
            SceneManager.LoadScene(name);
        }
    }
}
