namespace _Project.Core.Localization
{
    public interface ILocalizationService
    {
        string Get(string key);

        string Localize(string russian, string english);
    }
}
