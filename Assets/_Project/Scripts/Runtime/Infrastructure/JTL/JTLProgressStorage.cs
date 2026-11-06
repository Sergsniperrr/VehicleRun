using System;
using _Project.Core.Save;
using JTLStudio.SDK;

namespace _Project.Infrastructure.JTL
{
    public class JTLProgressStorage : IProgressStorage
    {
        private const string StateKey = "progress.state";
        private const string BackupKey = "progress.backup";

        private bool _isStateReadFromFailedLoad;

        public bool TryReadState(out GameSave save)
        {
            _isStateReadFromFailedLoad = JTLSDK.Data.LoadState == DataState.Failed;

            return TryRead(StateKey, out save);
        }

        public bool TryReadBackup(out GameSave save)
        {
            return TryRead(BackupKey, out save);
        }

        public void WriteState(GameSave save, bool isImportant)
        {
            Write(StateKey, save, isImportant);
        }

        public void WriteBackup(GameSave save)
        {
            Write(BackupKey, save, true);
        }

        private bool TryRead(string key, out GameSave save)
        {
            save = JTLSDK.Data.GetObject<GameSave>(key);

            return save != null;
        }

        private void Write(string key, GameSave save, bool isImportant)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            if (_isStateReadFromFailedLoad)
            {
                return;
            }

            JTLSDK.Data.SetObject(key, save, isImportant);
        }
    }
}
