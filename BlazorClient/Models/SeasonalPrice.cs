namespace BlazorClient.Models
{
    public class SeasonalPrice
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Month { get; set; } = string.Empty;
    }
}