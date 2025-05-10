using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectBigData.Utilities;
using ProjectBigData.Models;
using Microsoft.Extensions.Logging;

namespace ProjectBigData.Controllers
{
    public class SearchController : Controller
    {
        private readonly string _connectionString = "Server=DESKTOP-9AEHMMU\\UDEMY;Database=BigData;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly ILogger<SearchController> _logger;

        public SearchController(ILogger<SearchController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View(new List<LinkModel>());
        }

        [HttpPost]
      
        public IActionResult Index(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                _logger.LogWarning("Search called with empty or null keyword.");
                return View(new List<LinkModel>());
            }

            var results = SearchLinks(keyword);
            _logger.LogInformation($"Search for '{keyword}' returned {results.Count} links: {string.Join(", ", results.Select(l => l.Link))}");
            ViewBag.Keyword = keyword;

            // حساب عدد تكرار كلمة "link" في قيم الـ Link
            int linkCount = results.Sum(r => r.Link.Split(new[] { "link" }, StringSplitOptions.None).Length - 1);
            ViewBag.LinkWordCount = linkCount; // نضيف عدد التكرار لـ ViewBag

            return View(results);
        }

        private List<LinkModel> SearchLinks(string keyword)
        {
            var results = new List<LinkModel>();
            var keywords = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(k => k.Trim().ToLower())
                                  .ToArray();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                foreach (var word in keywords)
                {
                    var query = "SELECT [file_count] FROM world WHERE LOWER(pk) = @keyword";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@keyword", word);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            { 
                                var rawLinks = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                                var links = ExtractLinks(rawLinks);  // this returns List<LinkModel>
                                results.AddRange(links);
                            }
                        }
                    }
                }
            }

            // إزالة التكرارات حسب الرابط
            return results.GroupBy(l => l.Link)
                          .Select(g => g.First()) // ممكن تحبي تحتفظي بأكبر Count هنا لو حبيتي
                          .ToList();
        }

        private List<LinkModel> ExtractLinks(string rawLinks)
        {
            var cleanLinks = new List<LinkModel>();

            // Handle single link (for two keywords) or multiple links (for one keyword)
            var links = rawLinks.Contains(';') ? rawLinks.Split(';', StringSplitOptions.RemoveEmptyEntries) : new[] { rawLinks };

            foreach (var link in links)
            {
                var trimmedLink = link.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLink))
                {
                    _logger.LogWarning($"Empty or whitespace link in file_count: '{link}'");
                    continue;
                }

                // Find the last colon to separate link and count
                var lastColon = trimmedLink.LastIndexOf(':');
                if (lastColon > 0)
                {
                    var linkText = trimmedLink.Substring(0, lastColon).Trim();
                    var countText = trimmedLink.Substring(lastColon + 1).Trim();

                    if (!string.IsNullOrWhiteSpace(linkText))
                    {
                        if (int.TryParse(countText, out int count))
                        {
                            cleanLinks.Add(new LinkModel { Link = linkText, Count = count });
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to parse count '{countText}' in file_count: '{trimmedLink}'");
                            cleanLinks.Add(new LinkModel { Link = linkText, Count = 0 });
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid link text in file_count: '{trimmedLink}'");
                    }
                }
                else
                {
                    // No colon found, treat as link with count 0
                    if (!string.IsNullOrWhiteSpace(trimmedLink))
                    {
                        cleanLinks.Add(new LinkModel { Link = trimmedLink, Count = 0 });
                        _logger.LogWarning($"No colon found in file_count, treated as link with count 0: '{trimmedLink}'");
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid link in file_count: '{trimmedLink}'");
                    }
                }
            }

            return cleanLinks;
        }
        [HttpPost]
        public IActionResult PageRank(string keyword, List<LinkModel> links)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                _logger.LogWarning("PageRank called with empty or null keyword.");
                return View("~/Views/Shared/PageRank.cshtml", new List<(string Link, double PageRank)>());
            }

            if (links == null || !links.Any())
            {
                _logger.LogWarning("PageRank called with null or empty links.");
                return View("~/Views/Shared/PageRank.cshtml", new List<(string Link, double PageRank)>());
            }

            _logger.LogInformation($"PageRank called with keyword: '{keyword}', links count: {links.Count}, links: {string.Join(", ", links.Select(l => l.Link))}");

            // Clean links (less strict: only check for non-empty links)
            var cleanLinks = links.Where(l => !string.IsNullOrWhiteSpace(l.Link))
                                 .GroupBy(l => l.Link)
                                 .Select(g => g.First())
                                 .ToList();

            if (!cleanLinks.Any())
            {
                _logger.LogWarning("No valid links after cleaning.");
                return View("~/Views/Shared/PageRank.cshtml", new List<(string Link, double PageRank)>());
            }

            _logger.LogInformation($"After cleaning, {cleanLinks.Count} valid links remain: {string.Join(", ", cleanLinks.Select(l => l.Link))}");

            // Build graph
            var graph = BuildGraph(cleanLinks);

            // Calculate PageRank
            var pageRankScores = PageRankCalculators.CalculatePageRank(graph);

            // Convert results to list of (Link, PageRank)
            var results = cleanLinks.Select(l => (Link: l.Link, PageRank: pageRankScores.ContainsKey(l.Link) ? pageRankScores[l.Link] : 0.0))
                                   .OrderByDescending(x => x.PageRank)
                                   .ToList();

            _logger.LogInformation($"PageRank results: {results.Count} links with scores: {string.Join(", ", results.Select(r => $"{r.Link}: {r.PageRank:F4}"))}");

            ViewBag.Keyword = keyword;
            return View("~/Views/Shared/PageRank.cshtml", results);
        }

        private Dictionary<string, List<string>> BuildGraph(List<LinkModel> links)
        {
            var graph = new Dictionary<string, List<string>>();
            foreach (var link in links)
            {
                if (!graph.ContainsKey(link.Link))
                {
                    graph[link.Link] = new List<string>();
                }

                // Try to link based on Count
                var targets = links.Where(l => l.Count > link.Count && !string.IsNullOrWhiteSpace(l.Link))
                                  .Select(l => l.Link)
                                  .ToList();

                // Fallback: if no Count-based links, connect to all other links
                if (!targets.Any())
                {
                    targets = links.Where(l => l.Link != link.Link && !string.IsNullOrWhiteSpace(l.Link))
                                   .Select(l => l.Link)
                                   .ToList();
                }

                graph[link.Link].AddRange(targets);
            }

            _logger.LogInformation($"Graph built with {graph.Count} nodes and {graph.Values.Sum(list => list.Count)} edges.");
            return graph;
        }
    }
}