using System;
using _Project.Core.Save;

namespace _Project.Features.Save
{
    public class ProgressLoader
    {
        private readonly IProgressStorage _storage;
        private readonly GameSaveValidator _validator;
        private readonly GameStateRestorer _restorer;

        public ProgressLoader(IProgressStorage storage, GameSaveValidator validator, GameStateRestorer restorer)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _restorer = restorer ?? throw new ArgumentNullException(nameof(restorer));
        }

        public bool TryLoad()
        {
            if (_storage.TryReadState(out GameSave state) && _validator.IsValid(state))
            {
                Apply(state);

                return true;
            }

            if (_storage.TryReadBackup(out GameSave backup) && _validator.IsValid(backup))
            {
                Apply(backup);

                return true;
            }

            return false;
        }

        private void Apply(GameSave save)
        {
            _restorer.Restore(save);
            _storage.WriteBackup(save);
        }
    }
}
