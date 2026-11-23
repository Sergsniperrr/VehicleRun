using System;
using _Project.Features.Save;
using _Project.Features.Tutorial;

namespace _Project.Features.Session
{
    public class GameSession
    {
        private readonly ProgressLoader _loader;
        private readonly SaveScheduler _saveScheduler;
        private readonly NewGameSetup _newGameSetup;
        private readonly TutorialService _tutorial;

        public GameSession(ProgressLoader loader, SaveScheduler saveScheduler, NewGameSetup newGameSetup, TutorialService tutorial)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _saveScheduler = saveScheduler ?? throw new ArgumentNullException(nameof(saveScheduler));
            _newGameSetup = newGameSetup ?? throw new ArgumentNullException(nameof(newGameSetup));
            _tutorial = tutorial ?? throw new ArgumentNullException(nameof(tutorial));
        }

        public bool IsOpen { get; private set; }
        public LoadOutcome LoadOutcome { get; private set; }
        public bool IsNewGame => LoadOutcome == LoadOutcome.NotFound || LoadOutcome == LoadOutcome.Broken;

        public void Open()
        {
            if (IsOpen)
            {
                throw new InvalidOperationException(nameof(IsOpen));
            }

            IsOpen = true;
            LoadOutcome = _loader.Load();
            _saveScheduler.StartWriting();

            if (IsNewGame)
            {
                _newGameSetup.Apply();
                _tutorial.Begin();
            }
        }
    }
}
