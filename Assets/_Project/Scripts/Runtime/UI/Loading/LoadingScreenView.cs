using System;
using _Project.Core.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Loading
{
    public class LoadingScreenView : MonoBehaviour
    {
        [SerializeField] private Slider _progressBar;

        private LoadingProgress _progress;

        private void Start()
        {
            _progress.Changed += OnProgressChanged;
            ShowProgress(_progress.Value);
        }

        private void OnDestroy()
        {
            _progress.Changed -= OnProgressChanged;
        }

        [Inject]
        public void Construct(LoadingProgress progress)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        private void ShowProgress(float value)
        {
            _progressBar.value = value;
        }

        private void OnProgressChanged(float value)
        {
            ShowProgress(value);
        }
    }
}
