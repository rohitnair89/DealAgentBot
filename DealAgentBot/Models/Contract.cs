namespace DealAgentBot.Models
{
    public class Contract
    {
        public string ContractId { get; set; }
        public string CustomerName { get; set; }
        public string ContractName { get; set; }
        public string ContractState { get; set; }
        public decimal Revenue { get; set; }
    }
}
