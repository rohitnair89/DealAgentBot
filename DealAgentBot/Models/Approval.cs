namespace DealAgentBot.Models
{
    public class Approval
    {
        public string Id { get; set; }
        public string ApprovalName { get; set; }
        public string EntityId { get; set; }
        public string EntityType { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public string Status { get; set; }
        public string Comments { get; set; }
    }
}
