public class AuditLog
{
    public int Id { get; set; }

    public string Action { get; set; }
    public string EntityName { get; set; }
    public int EntityId { get; set; }

    public int PerformedById { get; set; }
    public User PerformedBy { get; set; }

    public DateTime Timestamp { get; set; }
}