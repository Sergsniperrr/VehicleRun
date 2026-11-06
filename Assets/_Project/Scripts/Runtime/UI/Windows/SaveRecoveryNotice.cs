using System;
using _Project.Features.Save;
using _Project.Features.Session;
using UnityEngine;
using VContainer;

namespace _Project.UI.Windows
{
    public class SaveRecoveryNotice : MonoBehaviour
    {
        private const string TitleKey = "popup.save.title";
        private const string RestoredFromBackupKey = "popup.save.restored_backup";
        private const string NewGameKey = "popup.save.new_game";

        [SerializeField] private MessageWindow _messageWindow;

        private GameSession _session;

        private void Start()
        {
            switch (_session.LoadOutcome)
            {
                case LoadOutcome.RestoredFromBackup:
                    _messageWindow.Show(TitleKey, RestoredFromBackupKey);
                    break;
                case LoadOutcome.Broken:
                    _messageWindow.Show(TitleKey, NewGameKey);
                    break;
            }
        }

        [Inject]
        public void Construct(GameSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }
    }
}
