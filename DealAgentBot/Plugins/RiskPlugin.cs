using DealAgentBot.Models;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace DealAgentBot.Plugins
{
    internal class RiskPlugin
    {
        private readonly string _risksFilePath = "MockData/risks.json";

        [KernelFunction("GetRisk")]
        [Description("Gets the risk details of an entity by entityType and entityId")]
        public async Task<Risk> GetRiskAsync(string entityId, string entityType)
        {
            var risks = await LoadRisksFromFileAsync();
            return risks.FirstOrDefault(r => string.Equals(r.EntityId, entityId, StringComparison.OrdinalIgnoreCase) && string.Equals(r.EntityType, entityType, StringComparison.OrdinalIgnoreCase));
        }

        //[KernelFunction("GetRiskById")]
        //[Description("Gets the risk details by RiskId")]
        public async Task<Risk> GetRiskByIdAsync(string riskId)
        {
            var risks = await LoadRisksFromFileAsync();
            return risks.FirstOrDefault(r => r.RiskId == riskId);
        }

        private async Task<List<Risk>> LoadRisksFromFileAsync()
        {
            if (!File.Exists(_risksFilePath))
            {
                return new List<Risk>();
            }

            var json = await File.ReadAllTextAsync(_risksFilePath);
            return JsonSerializer.Deserialize<List<Risk>>(json) ?? new List<Risk>();
        }
    }
}
