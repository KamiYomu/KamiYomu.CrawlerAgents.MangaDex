using KamiYomu.CrawlerAgents.Core;
using KamiYomu.CrawlerAgents.Core.Catalog;
using KamiYomu.CrawlerAgents.Core.Catalog.Builders;
using KamiYomu.CrawlerAgents.Core.Catalog.Definitions;
using KamiYomu.CrawlerAgents.Core.Inputs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace KamiYomu.CrawlerAgents.MangaDex
{
    [DisplayName("KamiYomu Crawler Agent – mangadex.org")]
    [CrawlerCheckBox("ContentRating", "The content rating for a series is based on the highest level of sexual content in the series", true, 2, ["safe", "suggestive", "erotica", "pornographic"])]
    [CrawlerSelect("Language", "Chapter Translation language, translated fields such as Titles and Descriptions", true, 1, [
        "en", "pt", "pt-br", "it", "de", "ru", "aa", "ab", "ae", "af", "ak", "am", "an", "ar-ae", "ar-bh", "ar-dz", "ar-eg", "ar-iq", "ar-jo", "ar-kw", "ar-lb", "ar-ly", "ar-ma", "ar-om",
        "ar-qa", "ar-sa", "ar-sy", "ar-tn", "ar-ye", "ar", "as", "av", "ay", "az", "ba", "be", "bg", "bh", "bi", "bm", "bn", "bo", "br", "bs", "ca", "ce", "ch", "co",
        "cr", "cs", "cu", "cv", "cy", "da", "de-at", "de-ch", "de-de", "de-li", "de-lu", "dv", "dz", "ee", "el", "en-au", "en-bz", "en-ca", "en-cb", "en-gb", "en-ie", "en-jm", "en-nz",
        "en-ph", "en-tt", "en-us", "en-za", "en-zw", "eo", "es-ar", "es-bo", "es-cl", "es-co", "es-cr", "es-do", "es-ec", "es-es", "es-gt", "es-hn", "es-la", "es-mx", "es-ni", "es-pa", "es-pe", "es-pr", "es-py", "es-sv",
        "es-us", "es-uy", "es-ve", "es", "et", "eu", "fa", "ff", "fi", "fj", "fo", "fr-be", "fr-ca", "fr-ch", "fr-fr", "fr-lu", "fr-mc", "fr", "fy", "ga", "gd", "gl", "gn", "gu",
        "gv", "ha", "he", "hi", "ho", "hr-ba", "hr-hr", "hr", "ht", "hu", "hy", "hz", "ia", "id", "ie", "ig", "ii", "ik", "in", "io", "is", "it-ch", "it-it", "iu",
        "iw", "ja", "ja-ro", "ji", "jv", "jw", "ka", "kg", "ki", "kj", "kk", "kl", "km", "kn", "ko", "ko-ro", "kr", "ks", "ku", "kv", "kw", "ky", "kz", "la",
        "lb", "lg", "li", "ln", "lo", "ls", "lt", "lu", "lv", "mg", "mh", "mi", "mk", "ml", "mn", "mo", "mr", "ms-bn", "ms-my", "ms", "mt", "my", "na", "nb",
        "nd", "ne", "ng", "nl-be", "nl-nl", "nl", "nn", "no", "nr", "ns", "nv", "ny", "oc", "oj", "om", "or", "os", "pa", "pi", "pl", "ps", "pt-pt", "qu-bo", "qu-ec",
        "qu-pe", "qu", "rm", "rn", "ro", "rw", "sa", "sb", "sc", "sd", "se-fi", "se-no", "se-se", "se", "sg", "sh", "si", "sk", "sl", "sm", "sn", "so", "sq", "sr-ba",
        "sr-sp", "sr", "ss", "st", "su", "sv-fi", "sv-se", "sv", "sw", "sx", "syr", "ta", "te", "tg", "th", "ti", "tk", "tl", "tn", "to", "tr", "ts", "tt", "tw",
        "ty", "ug", "uk", "ur", "us", "uz", "ve", "vi", "vo", "wa", "wo", "xh", "yi", "yo", "za", "zh-cn", "zh-hk", "zh-mo", "zh-ro", "zh-sg", "zh-tw", "zh", "zu"
    ])]
    public class MangaDexCrawlerAgent : AbstractCrawlerAgent, ICrawlerAgent
    {
        private bool _disposed = false;
        private HttpClient _httpClient;
        private string _language = CultureInfo.CurrentCulture.Name;

        public MangaDexCrawlerAgent(IDictionary<string, object> options) : base(options)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.mangadex.org")
            };

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(CrawlerAgentSettings.HttpUserAgent);

            if (Options.TryGetValue("Language", out var language) && language is string languageValue)
            {
                _language = languageValue;
            }
            else
            {
                _language = "en";
            }
        }

        /// <inheritdoc/>
        public Task<Uri> GetFaviconAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return null;

            return Task.FromResult(new Uri("https://mangadex.org/favicon.svg"));
        }


        /// <inheritdoc/>
        public async Task<PagedResult<Manga>> SearchAsync(string titleName, PaginationOptions paginationOptions, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return null;

            var mangaList = new List<Manga>();

            int total;

            var queryBuilder = new StringBuilder()
              .Append("manga")
              .Append($"?limit={paginationOptions.Limit}")
              .Append($"&offset={paginationOptions.OffSet}")
              .Append($"&title={titleName}")
              .Append($"&includes%5B%5D=manga")
              .Append($"&includes%5B%5D=cover_art")
              .Append($"&includes%5B%5D=author")
              .Append($"&includes%5B%5D=artist")
              .Append($"&includes%5B%5D=tag");

            if (Options.TryGetValue("ContentRating.safe", out var safe) && safe is bool safeValue && safeValue)
            {
                queryBuilder.Append($"&contentRating%5B%5D=safe");
            }
            if (Options.TryGetValue("ContentRating.suggestive", out var suggestive) && suggestive is bool suggestiveValue && suggestiveValue)
            {
                queryBuilder.Append($"&contentRating%5B%5D=safe");
            }
            if (Options.TryGetValue("ContentRating.erotica", out var erotica) && erotica is bool eroticaValue && eroticaValue)
            {
                queryBuilder.Append($"&contentRating%5B%5D=erotica");
            }
            if (Options.TryGetValue("ContentRating.pornographic", out var pornographic) && pornographic is bool pornographicValue && pornographicValue)
            {
                queryBuilder.Append($"&contentRating%5B%5D=pornographic");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, queryBuilder.ToString());
            var response = await _httpClient.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var rootNode = JsonNode.Parse(json);

            total = rootNode["total"].GetValue<int>();
            foreach (var item in rootNode["data"]?.AsArray() ?? [])
            {
                var manga = ConvertToManga(item);
                if (!string.IsNullOrEmpty(manga?.Title))
                {
                    mangaList.Add(ConvertToManga(item));
                }

            }
            return PagedResultBuilder<Manga>.Create()
                                            .WithData(mangaList)
                                            .WithPaginationOptions(new PaginationOptions(paginationOptions.OffSet, paginationOptions.Limit, total))
                                            .Build();
        }

        /// <inheritdoc/>
        public async Task<Manga> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return null;

            var queryBuilder = new StringBuilder()
           .Append($"manga/{id}")
           .Append($"?includes%5B%5D=manga")
           .Append($"&includes%5B%5D=cover_art")
           .Append($"&includes%5B%5D=author")
           .Append($"&includes%5B%5D=artist")
           .Append($"&includes%5B%5D=tag");

            var request = new HttpRequestMessage(HttpMethod.Get, queryBuilder.ToString());
            var response = await _httpClient.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var rootNode = JsonNode.Parse(json);

            return ConvertToManga(rootNode["data"].AsObject());
        }

        /// <inheritdoc/>
        public async Task<PagedResult<Chapter>> GetChaptersAsync(Manga manga, PaginationOptions paginationOptions, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return null;

            var queryBuilder = new StringBuilder()
                                   .Append($"manga/{manga.Id}/feed")
                                   .Append($"?limit={paginationOptions.Limit}")
                                   .Append($"&offset={paginationOptions.OffSet}")
                                   .Append($"&translatedLanguage%5B%5D={_language}")
                                   .Append($"&contentRating%5B%5D=safe")
                                   .Append($"&contentRating%5B%5D=suggestive")
                                   .Append($"&contentRating%5B%5D=erotica")
                                   .Append($"&contentRating%5B%5D=pornographic");

            var request = new HttpRequestMessage(HttpMethod.Get, queryBuilder.ToString());
            var response = await _httpClient.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var rootNode = JsonNode.Parse(json);
            var total = rootNode["total"].GetValue<int>();
            var chapters = new List<Chapter>();

            foreach (var item in rootNode["data"]?.AsArray() ?? [])
            {
                var chapter = ConvertToChapter(manga, item);
                if (chapter.Pages > 0)
                {
                    chapters.Add(chapter);
                }
            }

            return PagedResultBuilder<Chapter>.Create()
                                              .WithData(chapters)
                                              .WithPaginationOptions(new PaginationOptions(paginationOptions.OffSet, paginationOptions.Limit, total))
                                              .Build();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Page>> GetChapterPagesAsync(Chapter chapter, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return null;

            using var request = new HttpRequestMessage(HttpMethod.Get, chapter.Uri);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var rootNode = JsonNode.Parse(json);

            var baseUrl = rootNode?["baseUrl"]?.GetValue<string>();
            var chapterNode = rootNode?["chapter"];
            var hash = chapterNode?["hash"]?.GetValue<string>();
            var dataArray = chapterNode?["data"]?.AsArray();

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(hash) || dataArray is null)
                throw new InvalidOperationException("Invalid chapter metadata or missing image data.");

            var parentId = chapter.ParentManga?.Id ?? "unknown";
            var pages = new List<Page>(dataArray.Count);

            foreach (var item in dataArray)
            {
                var fileName = item?.ToString();
                if (!string.IsNullOrEmpty(fileName))
                {
                    var uri = new Uri($"{baseUrl}/data/{hash}/{fileName}");
                    var pageBuilder = PageBuilder.Create()
                        .WithChapterId(chapter.ParentManga.Id)
                        .WithId(DateTime.UtcNow.Ticks.ToString())
                        .WithImageUrl(uri)
                        .WithPageNumber(ExtractPageNumber(fileName))
                        .WithParentChapter(chapter);
                    pages.Add(pageBuilder.Build());
                }
            }

            return pages;
        }

        public static decimal ExtractPageNumber(string fileName)
        {
            var dashIndex = fileName.IndexOf('-');
            if (dashIndex > 0 && decimal.TryParse(fileName.Substring(0, dashIndex), out var pageNumber))
            {
                return pageNumber;
            }

            return 0m;
        }

        private Manga ConvertToManga(JsonNode item, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return null;

            var attributes = item?["attributes"];
            var tags = attributes?["tags"].AsArray();
            // ALT TITLES
            var altTitles = attributes?["altTitles"]?.AsArray();
            var localizedAltTitle = altTitles?
                .Select(t => t?[_language]?.ToString())
                .FirstOrDefault(t => !string.IsNullOrEmpty(t));

            var altTitleDict = altTitles?
                .Select(innerArray => innerArray?.AsObject())
                .Where(obj => obj != null && obj.Count > 0)
                .OfType<JsonObject>()
                .Select<JsonObject, KeyValuePair<string, string>?>(obj =>
                {
                    var firstProp = obj.FirstOrDefault();
                    var propertyName = firstProp.Key;
                    var value = obj.TryGetPropertyValue(propertyName, out var titleNode) && titleNode != null
                        ? titleNode.ToString()
                        : string.Empty;

                    return string.IsNullOrEmpty(propertyName)
                        ? null
                        : new KeyValuePair<string, string>(propertyName, value);
                })
                .Where(p => p != null)
                .GroupBy(p => p!.Value.Key)
                .ToDictionary(g => g.Key, g => g.First().Value.Value);

            // TITLE
            var titleObj = attributes?["title"]?.AsObject();
            var fallbackTitle = titleObj?.FirstOrDefault().Value?.ToString();

            // DESCRIPTION
            var descriptionObj = attributes?["description"]?.AsObject();
            var localizedDescription = descriptionObj?[_language]?.ToString();
            localizedDescription ??= descriptionObj?.FirstOrDefault(kvp => !string.IsNullOrEmpty(kvp.Value?.ToString())).Value?.ToString();
            var relationships = item?["relationships"]?.AsArray();

            var altDescriptionDict = descriptionObj?
                .Where(kvp => kvp.Key != null && kvp.Value != null)
                .Select(kvp =>
                {
                    var propertyName = kvp.Key;
                    var value = kvp.Value?.ToString() ?? string.Empty;
                    return new KeyValuePair<string, string>(propertyName, value);
                })
                .GroupBy(p => p.Key)
                .ToDictionary(g => g.Key, g => g.First().Value);

            var tagList = new List<string>();
            foreach (var tag in tags)
            {
                var tagAttr = tag?["attributes"]?.AsObject();
                var tagAttrName = tagAttr?["name"]?.AsObject();
                var tagAttrNameLocalized = tagAttrName?[_language]?.ToString();
                tagAttrNameLocalized ??= tagAttrName?.FirstOrDefault(kvp => !string.IsNullOrEmpty(kvp.Value.ToString())).Value?.ToString();
                tagList.Add(tagAttrNameLocalized);
            }

            var authorList = relationships?.Where(r => r?["type"]?.ToString() == "author").Select(p => p?["attributes"]?["name"]?.ToString());
            var artistList = relationships?.Where(r => r?["type"]?.ToString() == "artist").Select(p => p?["attributes"]?["name"]?.ToString());
            var builder = MangaBuilder.Create();
            var mangaId = item?["id"]?.GetValue<string>();
            var coverFileName = relationships?.FirstOrDefault(r => r?["type"]?.ToString() == "cover_art")?["attributes"]?["fileName"]?.ToString();
            var unsafeRatings = new[] { "erotica", "pornographic" };
            var contentRating = attributes?["contentRating"]?.ToString();
            var isFamilySafe = string.IsNullOrWhiteSpace(contentRating) || !unsafeRatings.Contains(contentRating, StringComparer.OrdinalIgnoreCase);
            builder.WithId(mangaId)
                   .WithTitle(!string.IsNullOrEmpty(localizedAltTitle) ? localizedAltTitle : fallbackTitle)
                   .WithAlternativeTitles(altTitleDict)
                   .WithAlternativeDescriptions(altDescriptionDict)
                   .WithAuthors([.. authorList])
                   .WithArtists([.. artistList])
                   .WithDescription(localizedDescription ?? string.Empty)
                   .WithOriginalLanguage(attributes?["originalLanguage"]?.ToString())
                   .WithTags([.. tagList])
                   .WithLastVolumeAvailable(decimal.TryParse(attributes?["lastVolume"]?.ToString(), out var lastVolume) ? lastVolume : 0m)
                   .WithLatestChapterAvailable(decimal.TryParse(attributes?["lastChapter"]?.ToString(), out var lastChapter) ? lastChapter : 0m)
                   .WithReleaseStatus(attributes?["status"]?.ToString().ToLower() switch
                   {
                       "ongoing" => ReleaseStatus.Continuing,
                       "completed" => ReleaseStatus.Completed,
                       "hiatus" => ReleaseStatus.OnHiatus,
                       "cancelled" => ReleaseStatus.Cancelled,
                       _ => ReleaseStatus.Unreleased
                   })
                   .WithYear(attributes?["year"]?.GetValue<int?>() ?? 0)
                   .WithIsFamilySafe(isFamilySafe)
                   .WithWebsiteUrl($"https://mangadex.org/title/{mangaId}")
                   .WithCoverFileName(coverFileName)
                   .WithCoverUrl(!string.IsNullOrEmpty(coverFileName) ? new Uri($"https://uploads.mangadex.org/covers/{mangaId}/{coverFileName}") : null);
            return builder.Build();
        }

        private Chapter ConvertToChapter(Manga manga, JsonNode item)
        {
            var attributes = item?["attributes"];

            var builder = ChapterBuilder.Create()
                                        .WithParentManga(manga)
                                        .WithId(item?["id"]?.GetValue<string>() ?? "")
                                        .WithTitle(attributes?["title"]?.GetValue<string>() ?? "")
                                        .WithVolume(decimal.TryParse(attributes?["volume"]?.GetValue<string>(), out var volume) ? volume : 0m)
                                        .WithNumber(decimal.TryParse(attributes?["chapter"]?.GetValue<string>(), out var number) ? number : 0m)
                                        .WithReleaseDate(attributes?["publishAt"]?.GetValue<DateTime>() ?? DateTime.MinValue)
                                        .WithTranslatedLanguage(attributes?["translatedLanguage"]?.ToString() ?? "")
                                        .WithPages(attributes?["pages"]?.GetValue<int?>().GetValueOrDefault(0) ?? 0)
                                        .WithUri(new Uri($"at-home/server/{item?["id"]?.GetValue<string>()}?forcePort443=false", UriKind.Relative));

            return builder.Build();
        }



        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _httpClient?.Dispose();
            }
            _disposed = true;
        }

        ~MangaDexCrawlerAgent()
        {
            Dispose(false);
        }

    }
}
