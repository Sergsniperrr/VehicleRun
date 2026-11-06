using System;
using UnityEngine;
using VContainer;

namespace _Project.UI.Windows
{
    public class WindowEscapeInput : MonoBehaviour
    {
        private WindowQueue _windows;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _windows.DismissCurrent();
            }
        }

        [Inject]
        public void Construct(WindowQueue windows)
        {
            _windows = windows ?? throw new ArgumentNullException(nameof(windows));
        }
    }
}
