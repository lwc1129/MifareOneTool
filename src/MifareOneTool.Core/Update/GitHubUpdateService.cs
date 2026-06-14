using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace MifareOneTool.Core.Update
{
    public class GitHubUpdateService
    {
        private static readonly HttpClient _http = new HttpClient();

        static GitHubUpdateService()
        {
            _http.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("MifareOneTool", "1.0"));
        }

        public Version LocalVersion { get; } =
            Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);

        public string RemoteVersion { get; private set; } = "Unknown";

        public bool HasUpdate =>
            Version.TryParse(RemoteVersion.TrimStart('v'), out var remote) &&
            remote > LocalVersion;

        public async Task CheckAsync(string githubRepo)
        {
            try
            {
                string url = $"https://api.github.com/repos/{githubRepo}/releases/latest";
                string json = await _http.GetStringAsync(url).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("message", out _))
                {
                    // API error (e.g. rate limit, repo not found)
                    return;
                }

                bool prerelease = root.TryGetProperty("prerelease", out var pre) && pre.GetBoolean();
                if (!prerelease && root.TryGetProperty("tag_name", out var tag))
                {
                    RemoteVersion = tag.GetString() ?? RemoteVersion;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Update check failed: {ex.Message}");
            }
        }
    }
}
