using System.Text.RegularExpressions;

namespace BlazorClient.Tests
{
    /// <summary>Усі тексти інтерфейсу мають жити в .resx, а не в розмітці чи коді.</summary>
    public class HardcodedTextTests
    {
        private static readonly Regex Cyrillic = new("[\u0400-\u04FF]");

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "BlazorApp.sln")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName ?? throw new InvalidOperationException("BlazorApp.sln не знайдено.");
        }

        /// <summary>Прибирає коментарі (HTML, Razor, //, ///), бо українською можна коментувати.</summary>
        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"<!--.*?-->", "", RegexOptions.Singleline);
            text = Regex.Replace(text, @"@\*.*?\*@", "", RegexOptions.Singleline);
            text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
            return Regex.Replace(text, @"^\s*//.*$", "", RegexOptions.Multiline);
        }

        private static bool HasHardcodedText(string path) =>
            Cyrillic.IsMatch(StripComments(File.ReadAllText(path)));

        [Fact]
        public void RazorFiles_ContainNoHardcodedUkrainianText()
        {
            var sep = Path.DirectorySeparatorChar;
            var offenders = Directory
                .GetFiles(Path.Combine(FindRepoRoot(), "BlazorClient"), "*.razor", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{sep}obj{sep}") && !f.Contains($"{sep}bin{sep}"))
                .Where(HasHardcodedText)
                .Select(Path.GetFileName)
                .ToList();

            Assert.Empty(offenders);
        }

        [Fact]
        public void CatalogueApiSource_ContainsNoHardcodedUkrainianText()
        {
            var path = Path.Combine(FindRepoRoot(), "BlazorClient", "Services", "CatalogueApi.cs");

            Assert.False(HasHardcodedText(path));
        }
    }
}
