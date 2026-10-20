namespace BlazorClient.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }
}