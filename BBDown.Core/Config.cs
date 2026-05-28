namespace BBDown.Core;

public static class Config
{
    private static AppSettings _settings = new();
    private static readonly object _lock = new();

    public static AppSettings Current
    {
        get { lock (_lock) { return _settings; } }
    }

    public static void Apply(AppSettings settings)
    {
        lock (_lock) { _settings = settings; }
    }

    public static string COOKIE { get => Current.Cookie; set => Apply(Current with { Cookie = value }); }
    public static string TOKEN { get => Current.Token; set => Apply(Current with { Token = value }); }
    public static bool DEBUG_LOG { get => Current.DebugLog; set => Apply(Current with { DebugLog = value }); }
    public static string HOST { get => Current.Host; set => Apply(Current with { Host = value }); }
    public static string EPHOST { get => Current.EpHost; set => Apply(Current with { EpHost = value }); }
    public static string TVHOST { get => Current.TvHost; set => Apply(Current with { TvHost = value }); }
    public static string AREA { get => Current.Area; set => Apply(Current with { Area = value }); }
    public static string WBI { get => Current.Wbi; set => Apply(Current with { Wbi = value }); }
    public static bool SKIP_SSL_CHECK { get => Current.SkipSslCheck; set => Apply(Current with { SkipSslCheck = value }); }

    public static readonly Dictionary<string, string> qualitys = AppSettings.QualityMap;
}
