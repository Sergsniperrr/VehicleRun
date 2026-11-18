using System;

namespace _Project.Core.SceneManagement
{
    public class LoadingProgress : IProgress<float>
    {
        private const float MinValue = 0f;
        private const float MaxValue = 1f;

        public event Action<float> Changed;

        public float Value { get; private set; }

        public void Report(float value)
        {
            if (float.IsNaN(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Value = Math.Min(Math.Max(value, MinValue), MaxValue);
            Changed?.Invoke(Value);
        }
    }
}
