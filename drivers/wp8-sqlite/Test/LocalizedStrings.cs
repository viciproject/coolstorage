using Vici.CoolStorage.WP8.Sqlite.Test.Resources;

namespace Vici.CoolStorage.WP8.Sqlite.Test
{
    /// <summary>
    /// Provides access to string resources.
    /// </summary>
    public class LocalizedStrings
    {
        private static AppResources _localizedResources = new AppResources();

        public AppResources LocalizedResources { get { return _localizedResources; } }
    }
}