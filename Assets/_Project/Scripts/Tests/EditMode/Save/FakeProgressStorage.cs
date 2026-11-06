using _Project.Core.Save;

namespace _Project.Tests.EditMode.Save
{
    public class FakeProgressStorage : IProgressStorage
    {
        public GameSave State { get; set; }
        public GameSave Backup { get; set; }
        public int StateWriteCount { get; private set; }
        public int BackupWriteCount { get; private set; }
        public bool IsLastStateWriteImportant { get; private set; }

        public bool TryReadState(out GameSave save)
        {
            save = State;

            return save != null;
        }

        public bool TryReadBackup(out GameSave save)
        {
            save = Backup;

            return save != null;
        }

        public void WriteState(GameSave save, bool isImportant)
        {
            State = save;
            StateWriteCount++;
            IsLastStateWriteImportant = isImportant;
        }

        public void WriteBackup(GameSave save)
        {
            Backup = save;
            BackupWriteCount++;
        }
    }
}
