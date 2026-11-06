using System;
using _Project.Features.Track;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Hud
{
    public class BestLineupButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private TrackRoster _roster;

        private void Awake()
        {
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnButtonClicked);
        }

        [Inject]
        public void Construct(TrackRoster roster)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
        }

        private void OnButtonClicked()
        {
            _roster.ApplyBestLineup();
        }
    }
}
