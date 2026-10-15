using System.Text;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>Lists the first few neighbours' quests ("Mia: Sell 5 Donuts 2/5") under the top bar.</summary>
    public sealed class QuestTrackerView : MonoBehaviour
    {
        private const int MaxLines = 3;

        [SerializeField] private GameObject panel = null;
        [SerializeField] private TMP_Text label = null;

        private readonly StringBuilder _text = new StringBuilder();
        private NeighbourService _neighbours;
        private QuestService _quests;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out _neighbours))
            {
                panel.SetActive(false);
                return;
            }

            _quests = ServiceLocator.Get<QuestService>();
            EventBus.Subscribe<QuestProgressed>(OnQuestProgressed);
            EventBus.Subscribe<NeighbourMovedIn>(OnMovedIn);
            Refresh();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<QuestProgressed>(OnQuestProgressed);
            EventBus.Unsubscribe<NeighbourMovedIn>(OnMovedIn);
        }

        private void OnQuestProgressed(QuestProgressed evt) => Refresh();
        private void OnMovedIn(NeighbourMovedIn evt) => Refresh();

        private void Refresh()
        {
            _text.Clear();
            int shown = 0;
            for (int i = 0; i < _neighbours.Count && shown < MaxLines; i++)
            {
                QuestDefinition quest = _quests.GetQuest(i);
                if (quest == null) continue;
                NeighbourModel neighbour = _neighbours.Get(i);
                if (shown > 0) _text.Append('\n');
                _text.Append(neighbour.name).Append(": ").Append(_quests.Describe(i))
                    .Append("  ").Append(neighbour.questProgress).Append('/').Append(quest.target);
                shown++;
            }

            panel.SetActive(shown > 0);
            label.text = _text.ToString();
        }
    }
}
