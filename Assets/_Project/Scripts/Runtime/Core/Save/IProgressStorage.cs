namespace _Project.Core.Save
{
    public interface IProgressStorage
    {
        bool TryReadState(out GameSave save);

        bool TryReadBackup(out GameSave save);

        void WriteState(GameSave save, bool isImportant);

        void WriteBackup(GameSave save);
    }
}
