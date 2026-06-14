using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using System;
using System.Collections.Generic;

namespace MifareOneTool.UI.Services;

/// <summary>
/// Manages UI language by swapping the active localization ResourceDictionary.
/// </summary>
public class LocalizationService
{
    public static LocalizationService Instance { get; } = new();

    // Map: culture code → display name shown in the Language combo
    public static readonly IReadOnlyDictionary<string, string> CultureToDisplay =
        new Dictionary<string, string>
        {
            { "",     "English" },
            { "zh",   "中文" },
            { "zh-TW","繁體中文" },
            { "ru",   "Русский" },
        };

    // Reverse: display name → culture code
    public static readonly IReadOnlyDictionary<string, string> DisplayToCulture =
        new Dictionary<string, string>
        {
            { "English",  "" },
            { "中文",      "zh" },
            { "繁體中文",  "zh-TW" },
            { "Русский",  "ru" },
        };

    private ResourceInclude? _current;

    public string CurrentCulture { get; private set; } = "";

    /// <summary>
    /// Applies the localization for the given culture code immediately.
    /// Call once on startup, then again when the user switches language.
    /// </summary>
    public void Apply(string culture)
    {
        CurrentCulture = culture;
        string file = culture switch
        {
            "zh"    => "zh",
            "zh-TW" => "zh-TW",
            "ru"    => "ru",
            _       => "en",
        };

        var uri = new Uri($"avares://MifareOneTool.UI/Assets/Localization/{file}.axaml");
        var next = new ResourceInclude(uri) { Source = uri };

        var merged = Application.Current!.Resources.MergedDictionaries;
        if (_current != null) merged.Remove(_current);
        merged.Add(next);
        _current = next;
    }

    /// <summary>
    /// Resolves a localized string by resource key, falling back to the key itself.
    /// </summary>
    public string Get(string key)
    {
        if (Application.Current!.TryGetResource(key, null, out var val) && val is string s)
            return s;
        return key;
    }
}
