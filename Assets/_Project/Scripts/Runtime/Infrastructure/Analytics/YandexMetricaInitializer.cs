using UnityEngine;

namespace _Project.Infrastructure.Analytics
{
    public class YandexMetricaInitializer : MonoBehaviour
    {
        [SerializeField] private string _counterId;

        private void Awake()
        {
            YandexMetrica.Init(_counterId);
        }
    }
}
