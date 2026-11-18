using System;
using _Project.Features.Progression;
using UnityEngine;
using VContainer;

namespace _Project.UI.Hud
{
    public class CatalogNewsBadgeView : MonoBehaviour
    {
        [SerializeField] private GameObject _badge;

        private CatalogNews _news;

        private void Start()
        {
            _news.Changed += OnNewsChanged;
            ShowBadge();
        }

        private void OnDestroy()
        {
            _news.Changed -= OnNewsChanged;
        }

        [Inject]
        public void Construct(CatalogNews news)
        {
            _news = news ?? throw new ArgumentNullException(nameof(news));
        }

        private void ShowBadge()
        {
            _badge.SetActive(_news.HasNews);
        }

        private void OnNewsChanged()
        {
            ShowBadge();
        }
    }
}
