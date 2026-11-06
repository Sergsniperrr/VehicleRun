using System;
using _Project.Features.Session;
using VContainer.Unity;

namespace _Project.Composition.Startup
{
    public class GameStartup : IInitializable
    {
        private readonly GameSession _session;

        public GameStartup(GameSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public void Initialize()
        {
            _session.Open();
        }
    }
}
