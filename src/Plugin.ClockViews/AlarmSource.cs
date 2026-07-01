namespace Plugin.ClockViews;

/// <summary>Where an <see cref="AlarmSource"/> loads its audio from.</summary>
public enum AlarmSourceKind
{
    /// <summary>A file bundled in the app package (MAUI Raw asset).</summary>
    Resources,
    /// <summary>A file on the local file system.</summary>
    File,
    /// <summary>A remote URL.</summary>
    Url,
}

/// <summary>
/// Identifies the alarm sound a countdown clock plays when it reaches zero. Create one with
/// <see cref="FromResources"/>, <see cref="FromFile"/>, or <see cref="FromUrl"/> — it is up to
/// the consumer to supply the audio. Modelled on MAUI's <c>ImageSource</c> factory pattern.
/// </summary>
public sealed class AlarmSource
{
    AlarmSource(AlarmSourceKind kind, string value)
    {
        Kind = kind;
        Value = value;
    }

    /// <summary>The kind of source.</summary>
    public AlarmSourceKind Kind { get; }

    /// <summary>The resource file name, file path, or URL, depending on <see cref="Kind"/>.</summary>
    public string Value { get; }

    /// <summary>An audio file bundled in the app package (e.g. a <c>Resources/Raw</c> file name).</summary>
    public static AlarmSource FromResources(string fileName) => new(AlarmSourceKind.Resources, fileName);

    /// <summary>An audio file on the local file system.</summary>
    public static AlarmSource FromFile(string filePath) => new(AlarmSourceKind.File, filePath);

    /// <summary>An audio file at a remote URL.</summary>
    public static AlarmSource FromUrl(string url) => new(AlarmSourceKind.Url, url);

    public override string ToString() => $"{Kind}:{Value}";
}
