namespace BBDown.Core;

public static class Config
{
    private static AppSettings _settings = new();

    public static AppSettings Current => _settings;

    public static void Apply(AppSettings settings) => _settings = settings;

    public static string COOKIE { get => _settings.Cookie; set => _settings = _settings with { Cookie = value }; }
    public static string TOKEN { get => _settings.Token; set => _settings = _settings with { Token = value }; }
    public static bool DEBUG_LOG { get => _settings.DebugLog; set => _settings = _settings with { DebugLog = value }; }
    public static string HOST { get => _settings.Host; set => _settings = _settings with { Host = value }; }
    public static string EPHOST { get => _settings.EpHost; set => _settings = _settings with { EpHost = value }; }
    public static string TVHOST { get => _settings.TvHost; set => _settings = _settings with { TvHost = value }; }
    public static string AREA { get => _settings.Area; set => _settings = _settings with { Area = value }; }
    public static string WBI { get => _settings.Wbi; set => _settings = _settings with { Wbi = value }; }
    public static bool SKIP_SSL_CHECK { get => _settings.SkipSslCheck; set => _settings = _settings with { SkipSslCheck = value }; }

    public static readonly Dictionary<string, string> qualitys = AppSettings.QualityMap;
}
