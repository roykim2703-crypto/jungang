using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

/// <summary>Apply the same mobile layout to new builds and an existing web player.</summary>
public static class WebPlayerLayout
{
    public static void Apply(string buildPath)
    {
        const string source = "Assets/Editor/WebPlayer";
        string template = Path.Combine(buildPath, "TemplateData");
        Directory.CreateDirectory(template);
        File.Copy(Path.Combine(source, "mobile-layout.css"), Path.Combine(template, "mobile-layout.css"), true);
        File.Copy(Path.Combine(source, "mobile-layout.js"), Path.Combine(template, "mobile-layout.js"), true);
        string indexPath = Path.Combine(buildPath, "index.html");
        string html = File.ReadAllText(indexPath);
        if (!html.Contains("TemplateData/mobile-layout.css"))
            html = html.Replace("</head>", "<link rel=\"stylesheet\" href=\"TemplateData/mobile-layout.css\">\n<script src=\"TemplateData/mobile-layout.js\"></script>\n</head>");
        if (!html.Contains("JungangMobileLayout(config)"))
            html = html.Replace("var script = document.createElement(\"script\");", "window.JungangMobileLayout(config);\n      var script = document.createElement(\"script\");");
        html = Regex.Replace(html, @"TemplateData/mobile-layout\.(css|js)(\?[^""\s<>]*)?", "TemplateData/mobile-layout.$1?v=board169-v2");
        // New game code must not reuse a browser's cached copy of the previous player.
        html = Regex.Replace(html, @"(buildUrl\s*\+\s*""/)([^""?]+)(?:\?[^""]*)?("")", match =>
        {
            string file = Path.Combine(buildPath, "Build", match.Groups[2].Value);
            using (var stream = File.OpenRead(file))
            using (var sha = SHA256.Create())
            {
                string hash = System.BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").Substring(0, 12);
                return match.Groups[1].Value + match.Groups[2].Value + "?v=" + hash + match.Groups[3].Value;
            }
        });
        File.WriteAllText(indexPath, html);
    }

    public static void ApplyToPagesBuild() => Apply("Builds/PagesProject/Builds/WebGL");
}
