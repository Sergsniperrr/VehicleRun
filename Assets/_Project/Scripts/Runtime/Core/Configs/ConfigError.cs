using Object = UnityEngine.Object;

namespace _Project.Core.Configs
{
    public class ConfigError
    {
        public ConfigError(Object source, string message)
        {
            Source = source;
            Message = message;
        }

        public Object Source { get; }
        public string Message { get; }

        public override string ToString()
        {
            return Message;
        }
    }
}
