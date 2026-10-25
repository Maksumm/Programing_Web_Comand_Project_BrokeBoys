namespace BlazorClient.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal? Threshold { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
