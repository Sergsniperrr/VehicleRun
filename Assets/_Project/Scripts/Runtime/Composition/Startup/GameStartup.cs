using System;
using _Project.Features.Save;
using VContainer.Unity;

namespace _Project.Composition.Startup
{
    public class GameStartup : IInitializable
    {
        private readonly ProgressLoader _loader;
        private readonly SaveScheduler _saveScheduler;
        private readonly SampleStateSeeder _seeder;

        public GameStartup(ProgressLoader loader, SaveScheduler saveScheduler, SampleStateSeeder seeder)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _saveScheduler = saveScheduler ?? throw new ArgumentNullException(nameof(saveScheduler));
            _seeder = seeder ?? throw new ArgumentNullException(nameof(seeder));
        }

        public void Initialize()
        {
            bool isLoaded = _loader.TryLoad();
            _saveScheduler.StartWriting();

            if (isLoaded == false)
            {
                _seeder.Seed();
            }
        }
    }
}
