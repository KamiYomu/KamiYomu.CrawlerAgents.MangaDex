using KamiYomu.CrawlerAgents.Core;
using KamiYomu.CrawlerAgents.Core.Catalog;
using KamiYomu.CrawlerAgents.MangaDex;
using Spectre.Console;
using System.Net.Http;

AnsiConsole.MarkupLine("[bold underline green]KamiYomu AgentCrawler Validator[/]\n");

ICrawlerAgent crawler = new MangaDexCrawlerAgent(null);

var results = new List<(string Method, bool Success, string Message)>();

// Test GetFaviconAsync
try
{
    var favicon = await crawler.GetFaviconAsync(CancellationToken.None);
    results.Add((nameof(ICrawlerAgent.GetFaviconAsync), favicon != null && favicon.IsAbsoluteUri, favicon?.ToString() ?? "Returned null"));
}
catch (Exception ex)
{
    results.Add((nameof(ICrawlerAgent.GetFaviconAsync), false, ex.Message));
}


// Test SearchAsync
try
{
    Thread.Sleep(1000);
    var result = await crawler.SearchAsync("Hole", new PaginationOptions(1, 1), CancellationToken.None);
    var count = result.Data?.Count() ?? 0;
    results.Add((nameof(ICrawlerAgent.SearchAsync), count > 0, $"Returned {count} result(s)"));
}
catch (Exception ex)
{
    results.Add((nameof(ICrawlerAgent.SearchAsync), false, ex.Message));
}


// Test GetByIdAsync
try
{
    Thread.Sleep(1000);
    var result = await crawler.SearchAsync("One Piece", new PaginationOptions(0, 1, 30), CancellationToken.None);
    Thread.Sleep(1000);
    var manga = await crawler.GetByIdAsync(result.Data.ElementAt(0)?.Id, CancellationToken.None);
    var any = result.Data?.Any() ?? false;
    results.Add((nameof(ICrawlerAgent.GetByIdAsync), any, $"Returning {manga.Title} and its cover {manga.CoverUrl}."));
}
catch (Exception ex)
{
    results.Add((nameof(ICrawlerAgent.GetByIdAsync), false, ex.Message));
}


// Test GetChaptersAsync
try
{
    Thread.Sleep(1000);
    var mangaResult = await crawler.SearchAsync("One Piece", new PaginationOptions(3, 1), CancellationToken.None);
    Thread.Sleep(1000);
    var chaptersResult = await crawler.GetChaptersAsync(mangaResult.Data.ElementAt(0), new PaginationOptions(0, 1, 30), CancellationToken.None);
    Thread.Sleep(1000);
    var count = chaptersResult?.Data.Count() ?? 0;
    results.Add((nameof(ICrawlerAgent.GetChaptersAsync), !string.IsNullOrWhiteSpace(chaptersResult.Data.FirstOrDefault()?.Id), $"Returned {count} result(s)"));
}
catch (Exception ex)
{
    results.Add((nameof(ICrawlerAgent.GetChaptersAsync), false, ex.Message));
}

// Test GetChaptersAsync
try
{
    Thread.Sleep(1000);
    var mangaResult = await crawler.SearchAsync("One Piece", new PaginationOptions(3,1), CancellationToken.None);
    Thread.Sleep(1000);
    var chaptersResult = await crawler.GetChaptersAsync(mangaResult.Data.ElementAt(0), new PaginationOptions(0, 1), CancellationToken.None);
    Thread.Sleep(1000);
    var chapterImages = await crawler.GetChapterPagesAsync(chaptersResult.Data.ElementAt(0), CancellationToken.None);
    results.Add((nameof(ICrawlerAgent.GetChaptersAsync), chapterImages.Any(), $"Returned {chapterImages.Count()} result(s)"));
}
catch (Exception ex)
{
    results.Add((nameof(ICrawlerAgent.GetChaptersAsync), false, ex.Message));
}

// Test GetByteArrayAsync
try
{
    Thread.Sleep(1000);
    var mangaResult = await crawler.SearchAsync("One Piece", new PaginationOptions(3, 1), CancellationToken.None);
    Thread.Sleep(1000);
    var chaptersResult = await crawler.GetChaptersAsync(mangaResult.Data.ElementAt(0), new PaginationOptions(0,1), CancellationToken.None);
    Thread.Sleep(1000);
    var chapterImages = await crawler.GetChapterPagesAsync(chaptersResult.Data.ElementAt(0), CancellationToken.None);

    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(CrawlerAgentSettings.HttpUserAgent);
    Thread.Sleep(1000);
    var imageBytes = await httpClient.GetByteArrayAsync(chapterImages.ElementAt(0).ImageUrl);
    results.Add((nameof(HttpClient.GetByteArrayAsync), chapterImages.Any(), $"Returned {imageBytes.Count()} bytes as result(s)"));
}
catch (Exception ex)
{
    results.Add((nameof(HttpClient.GetByteArrayAsync), false, ex.Message));
}

// Display results
var table = new Table()
    .Title("[yellow]Test Results[/]")
    .Border(TableBorder.Rounded)
    .AddColumn("Method")
    .AddColumn("Status")
    .AddColumn("Message");

foreach (var (method, success, message) in results)
{
    table.AddRow(
        $"[bold]{method}[/]",
        success ? "[green]✔ Passed[/]" : "[red]✘ Failed[/]",
        message
    );
}

AnsiConsole.Write(table);
