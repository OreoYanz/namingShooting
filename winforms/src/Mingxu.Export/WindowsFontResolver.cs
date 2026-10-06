using System;
using System.Collections.Generic;
using System.IO;
using PdfSharpCore.Fonts;

namespace Mingxu.Export
{

/// <summary>
/// Loads fonts from Windows\Fonts as raw bytes so PdfSharpCore
/// does not use its default FontResolver (which needs SixLabors SystemFonts).
/// Prefer .ttf faces; .ttc collections are less reliable when loaded as bytes.
/// </summary>
public sealed class WindowsFontResolver : IFontResolver
{
    private static readonly object Gate = new object();
    private static bool _installed;

    private readonly Dictionary<string, string> _faceToFile =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public string DefaultFontName
    {
        get { return "DFKai-SB"; }
    }

    public static void EnsureInstalled()
    {
        lock (Gate)
        {
            if (_installed) return;
            GlobalFontSettings.FontResolver = new WindowsFontResolver();
            _installed = true;
        }
    }

    public WindowsFontResolver()
    {
        var fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        // 標楷體 — true TTF, best CJK choice for PdfSharpCore on Windows
        Register("DFKai-SB", Path.Combine(fonts, "kaiu.ttf"));
        Register("標楷體", Path.Combine(fonts, "kaiu.ttf"));
        Register("Arial", Path.Combine(fonts, "arial.ttf"));
        Register("Arial Bold", Path.Combine(fonts, "arialbd.ttf"));
        Register("Segoe UI", Path.Combine(fonts, "segoeui.ttf"));
        Register("Segoe UI Bold", Path.Combine(fonts, "segoeuib.ttf"));
        // Fallback TTC if kaiu missing (some stripped images)
        Register("Microsoft JhengHei", Path.Combine(fonts, "msjh.ttc"));
        Register("Microsoft JhengHei Bold", Path.Combine(fonts, "msjhbd.ttc"));
        Register("Microsoft YaHei", Path.Combine(fonts, "msyh.ttc"));
        Register("MingLiU", Path.Combine(fonts, "mingliu.ttc"));
    }

    private void Register(string face, string path)
    {
        if (File.Exists(path))
            _faceToFile[face] = path;
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var family = string.IsNullOrWhiteSpace(familyName) ? DefaultFontName : familyName.Trim();
        string face;

        if (LooksLikeKai(family) || LooksLikeJhengHei(family) || LooksLikeMing(family) || LooksLikeYaHei(family))
            face = PreferChineseFace(isBold);
        else if (LooksLikeArial(family))
            face = isBold && _faceToFile.ContainsKey("Arial Bold") ? "Arial Bold" : "Arial";
        else if (LooksLikeSegoe(family))
            face = isBold && _faceToFile.ContainsKey("Segoe UI Bold") ? "Segoe UI Bold" : "Segoe UI";
        else
            face = PreferChineseFace(isBold);

        if (!_faceToFile.ContainsKey(face))
            face = FirstAvailableFace();

        if (face == null)
            return null;

        return new FontResolverInfo(face);
    }

    public byte[] GetFont(string faceName)
    {
        string path;
        if (!_faceToFile.TryGetValue(faceName, out path) || !File.Exists(path))
            throw new InvalidOperationException("找不到字型檔：" + faceName);
        return File.ReadAllBytes(path);
    }

    private string PreferChineseFace(bool bold)
    {
        if (_faceToFile.ContainsKey("DFKai-SB"))
            return "DFKai-SB";
        if (bold && _faceToFile.ContainsKey("Microsoft JhengHei Bold"))
            return "Microsoft JhengHei Bold";
        if (_faceToFile.ContainsKey("Microsoft JhengHei"))
            return "Microsoft JhengHei";
        if (_faceToFile.ContainsKey("Microsoft YaHei"))
            return "Microsoft YaHei";
        if (_faceToFile.ContainsKey("MingLiU"))
            return "MingLiU";
        return FirstAvailableFace();
    }

    private string FirstAvailableFace()
    {
        foreach (var key in _faceToFile.Keys)
            return key;
        return null;
    }

    private static bool LooksLikeKai(string family)
    {
        var n = family.ToLowerInvariant();
        return n.Contains("dfkai") || n.Contains("kai") || family.Contains("楷");
    }

    private static bool LooksLikeJhengHei(string family)
    {
        var n = family.ToLowerInvariant();
        return n.Contains("jhenghei") || family.Contains("正黑");
    }

    private static bool LooksLikeYaHei(string family)
    {
        var n = family.ToLowerInvariant();
        return n.Contains("yahei") || family.Contains("雅黑");
    }

    private static bool LooksLikeMing(string family)
    {
        var n = family.ToLowerInvariant();
        return n.Contains("mingliu") || n.Contains("pmingliu") || family.Contains("細明");
    }

    private static bool LooksLikeArial(string family)
    {
        return family.IndexOf("arial", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool LooksLikeSegoe(string family)
    {
        return family.IndexOf("segoe", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
}
