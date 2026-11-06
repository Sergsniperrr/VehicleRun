namespace _Project.UI.Windows
{
    public interface IWindow
    {
        bool IsFullscreen { get; }

        void Show();

        void Hide();

        void Dismiss();
    }
}
