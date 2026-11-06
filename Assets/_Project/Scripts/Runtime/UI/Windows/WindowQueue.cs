using System;
using System.Collections.Generic;
using _Project.Features.Garage;

namespace _Project.UI.Windows
{
    public class WindowQueue : IFieldInputBlocker
    {
        private readonly List<IWindow> _pending = new List<IWindow>();

        public event Action BlockingStarted;
        public event Action CurrentChanged;

        public IWindow Current { get; private set; }
        public bool IsBlocking => Current != null;
        public int PendingCount => _pending.Count;

        public bool Contains(IWindow window)
        {
            return Current == window || _pending.Contains(window);
        }

        public void Open(IWindow window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            if (Contains(window))
            {
                return;
            }

            if (IsBlocking)
            {
                _pending.Add(window);

                return;
            }

            ShowCurrent(window);
            BlockingStarted?.Invoke();
            CurrentChanged?.Invoke();
        }

        public void Close(IWindow window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            if (Current != window)
            {
                _pending.Remove(window);

                return;
            }

            Current = null;
            window.Hide();

            if (_pending.Count > 0)
            {
                IWindow next = _pending[0];
                _pending.RemoveAt(0);
                ShowCurrent(next);
            }

            CurrentChanged?.Invoke();
        }

        public void DismissCurrent()
        {
            if (IsBlocking)
            {
                Current.Dismiss();
            }
        }

        private void ShowCurrent(IWindow window)
        {
            Current = window;
            window.Show();
        }
    }
}
