using System;
using System.Collections.Generic;
using System.Threading;
using _Project.Core.Leaderboards;
using _Project.Core.Localization;
using _Project.Features.Economy;
using _Project.Features.Leaderboards;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.UI.Windows
{
    public class LeaderboardWindow : Window
    {
        private const string LoadingKey = "popup.leaderboard.loading";
        private const string EmptyKey = "popup.leaderboard.empty";
        private const string AnonymousKey = "popup.leaderboard.anonymous";

        private readonly List<LeaderboardRowView> _rows = new List<LeaderboardRowView>();

        [SerializeField] private LeaderboardRowView _rowPrefab;
        [SerializeField] private RectTransform _rowsRoot;
        [SerializeField] private TMP_Text _status;

        private ILeaderboardService _leaderboard;
        private LeaderboardScoreSubmitter _submitter;
        private CoinsFormatter _formatter;
        private ILocalizationService _localization;
        private IObjectResolver _resolver;
        private CancellationTokenSource _loading;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CancelLoading();
        }

        [Inject]
        public void Construct(
            ILeaderboardService leaderboard,
            LeaderboardScoreSubmitter submitter,
            CoinsFormatter formatter,
            ILocalizationService localization,
            IObjectResolver resolver)
        {
            _leaderboard = leaderboard ?? throw new ArgumentNullException(nameof(leaderboard));
            _submitter = submitter ?? throw new ArgumentNullException(nameof(submitter));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public override void Show()
        {
            base.Show();
            _submitter.Submit();
            CancelLoading();
            _loading = new CancellationTokenSource();
            LoadRowsAsync(_loading.Token).Forget();
        }

        public override void Hide()
        {
            CancelLoading();
            base.Hide();
        }

        private async UniTaskVoid LoadRowsAsync(CancellationToken cancellationToken)
        {
            HideRows();
            ShowStatus(LoadingKey);
            LeaderboardTable table;

            try
            {
                table = await _leaderboard.LoadAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (table.Rows.Count == 0)
            {
                ShowStatus(EmptyKey);

                return;
            }

            _status.gameObject.SetActive(false);
            ShowRows(table.Rows);
        }

        private void ShowRows(IReadOnlyList<LeaderboardRow> rows)
        {
            CreateMissingRows(rows.Count);

            for (int i = 0; i < rows.Count; i++)
            {
                LeaderboardRow row = rows[i];
                string score = _formatter.FormatShort(row.Score);
                _rows[i].Show(row.Rank, GetPlayerName(row), score, row.IsCurrentPlayer);
            }
        }

        private string GetPlayerName(LeaderboardRow row)
        {
            if (string.IsNullOrWhiteSpace(row.PlayerName))
            {
                return _localization.Get(AnonymousKey);
            }

            return row.PlayerName;
        }

        private void CreateMissingRows(int rowCount)
        {
            while (_rows.Count < rowCount)
            {
                LeaderboardRowView row = _resolver.Instantiate(_rowPrefab, _rowsRoot);
                _rows.Add(row);
            }
        }

        private void HideRows()
        {
            foreach (LeaderboardRowView row in _rows)
            {
                row.Hide();
            }
        }

        private void ShowStatus(string key)
        {
            _status.text = _localization.Get(key);
            _status.gameObject.SetActive(true);
        }

        private void CancelLoading()
        {
            if (_loading == null)
            {
                return;
            }

            _loading.Cancel();
            _loading.Dispose();
            _loading = null;
        }
    }
}
