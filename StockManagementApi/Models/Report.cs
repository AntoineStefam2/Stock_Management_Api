public class Report
{
    public int Id { get; set; }

    public string Title { get; set; }
    public string Content { get; set; }

    public DateTime GeneratedAt { get; set; }

    public int GeneratedById { get; set; }
    public User GeneratedBy { get; set; }
}