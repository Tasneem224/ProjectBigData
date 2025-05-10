namespace ProjectBigData.Models
{
    public class SearchViewModel
    {
        public List<LinkModel> SearchResults { get; set; } = new List<LinkModel>();
        public List<(string Url, double Score)> PageRankResults { get; set; } = new List<(string Url, double Score)>();
    }
}