// ScoreboardUI.cs — live territory percentage list, updated every 2 sec.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaintGame
{
    public class ScoreboardUI : MonoBehaviour
    {
        [SerializeField] private Transform         _rowParent;
        [SerializeField] private ScoreRowUI        _rowPrefab;

        private ScoreTracker _tracker;
        private readonly List<ScoreRowUI> _rows = new List<ScoreRowUI>(6);
        private float _updateTimer;

        public void Init(ScoreTracker tracker)
        {
            _tracker = tracker;
            // Pre-create 6 rows
            for (int i = 0; i < GameConstants.TOTAL_PLAYERS; i++)
            {
                var row = Instantiate(_rowPrefab, _rowParent);
                row.gameObject.SetActive(false);
                _rows.Add(row);
            }
        }

        void Update()
        {
            if (_tracker == null) return;
            _updateTimer += Time.deltaTime;
            if (_updateTimer < 2f) return;
            _updateTimer = 0f;
            Refresh();
        }

        private void Refresh()
        {
            var scores = _tracker.Scores;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (i < scores.Count)
                {
                    _rows[i].gameObject.SetActive(true);
                    _rows[i].Set(i + 1, scores[i].Player.Stats.PlayerName,
                                  scores[i].Player.Stats.PlayerColor,
                                  scores[i].Percentage);
                }
                else
                {
                    _rows[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
