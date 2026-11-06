using System;
using _Project.Features.Save;

namespace _Project.Features.Session
{
    public class GameSession
    {
        private readonly ProgressLoader _loader;
        private readonly SaveScheduler _saveScheduler;
        private readonly NewGameSetup _newGameSetup;

        public GameSession(ProgressLoader loader, SaveScheduler saveScheduler, NewGameSetup newGameSetup)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _saveScheduler = saveScheduler ?? throw new ArgumentNullException(nameof(saveScheduler));
            _newGameSetup = newGameSetup ?? throw new ArgumentNullException(nameof(newGameSetup));
        }

        public bool IsOpen { get; private set; }

        public void Open()
        {
            if (IsOpen)
            {
                throw new InvalidOperationException(nameof(IsOpen));
            }

            IsOpen = true;
            bool isLoaded = _loader.TryLoad();
            _saveScheduler.StartWriting();

            if (isLoaded == false)
            {
                _newGameSetup.Apply();
            }
        }
    }
}
