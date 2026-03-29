// KillFeedUI.cs — sliding toast messages for kills and eliminations.
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PaintGame
{
    public class KillFeedUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _entryPrefab;
        [SerializeField] private Transform       _container;

        private readonly Queue<TextMeshProUGUI> _entries = new Queue<TextMeshProUGUI>();
        private const int MAX_ENTRIES = 5;

        public void AddEntry(PlayerController killer, PlayerController victim)
        {
            string text = $"<color=#{ColorUtility.ToHtmlStringRGB(killer.Stats.PlayerColor)}>" +
                          $"{killer.Stats.PlayerName}</color> hit " +
                          $"<color=#{ColorUtility.ToHtmlStringRGB(victim.Stats.PlayerColor)}>" +
                          $"{victim.Stats.PlayerName}</color>";
            ShowEntry(text);
        }

        public void AddEliminatedEntry(PlayerController p)
        {
            string text = $"<color=#{ColorUtility.ToHtmlStringRGB(p.Stats.PlayerColor)}>" +
                          $"{p.Stats.PlayerName}</color> eliminated!";
            ShowEntry(text);
        }

        private void ShowEntry(string text)
        {
            if (_entryPrefab == null) return;

            var entry = Instantiate(_entryPrefab, _container);
            entry.text = text;

            if (_entries.Count >= MAX_ENTRIES)
            {
                var old = _entries.Dequeue();
                if (old != null) Destroy(old.gameObject);
            }
            _entries.Enqueue(entry);
            StartCoroutine(FadeEntry(entry, 3.5f));
        }

        private IEnumerator FadeEntry(TextMeshProUGUI entry, float lifetime)
        {
            yield return new WaitForSeconds(lifetime - 0.5f);
            float t = 0f;
            while (t < 0.5f && entry != null)
            {
                t += Time.deltaTime;
                entry.color = new Color(1f, 1f, 1f, 1f - t / 0.5f);
                yield return null;
            }
            if (entry != null) Destroy(entry.gameObject);
        }
    }
}
