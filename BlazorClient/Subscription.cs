namespace BlazorClient.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Duration { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
