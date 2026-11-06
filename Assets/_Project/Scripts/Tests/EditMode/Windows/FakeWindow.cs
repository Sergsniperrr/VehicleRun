using _Project.UI.Windows;

namespace _Project.Tests.EditMode.Windows
{
    public class FakeWindow : IWindow
    {
        public FakeWindow(bool isFullscreen)
        {
            IsFullscreen = isFullscreen;
        }

        public bool IsFullscreen { get; }
        public bool IsShown { get; private set; }
        public int ShowCount { get; private set; }
        public int DismissCount { get; private set; }

        public void Show()
        {
            IsShown = true;
            ShowCount++;
        }

        public void Hide()
        {
            IsShown = false;
        }

        public void Dismiss()
        {
            DismissCount++;
        }
    }
}
