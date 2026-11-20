using System.Collections.Generic;
using System.Threading;
using _Project.Core.Leaderboards;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace _Project.Infrastructure.Leaderboards
{
    public class AvatarLoader : IAvatarLoader
    {
        private readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public async UniTask<Sprite> LoadAsync(string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            if (_cache.TryGetValue(url, out Sprite cached))
            {
                return cached;
            }

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                await request.SendWebRequest().WithCancellation(cancellationToken);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    return null;
                }

                Sprite sprite = CreateSprite(DownloadHandlerTexture.GetContent(request));
                _cache[url] = sprite;

                return sprite;
            }
        }

        private Sprite CreateSprite(Texture2D texture)
        {
            Rect rect = new Rect(0, 0, texture.width, texture.height);

            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
        }
    }
}
