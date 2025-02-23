using DealAgentBot.Models;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace DealAgentBot.Plugins
{
    internal class ApprovalPlugin
    {
        private readonly string _approvalsFilePath = "MockData/approvals.json";

        [KernelFunction("GetApprovals")]
        public async Task<IEnumerable<Approval>> GetApprovalsAsync(string entityId, string entityType)
        {
            var approvals = await LoadApprovalsFromFileAsync();
            return approvals.Where(a => string.Equals(a.EntityId, entityId, StringComparison.OrdinalIgnoreCase) && string.Equals(a.EntityType, entityType, StringComparison.OrdinalIgnoreCase));
        }

        [KernelFunction("Approve")]
        [Description("Approves an approval for an entity type and entityid with comments")]
        public async Task<Approval> ApproveAsync(string approvalName, string entityType, string entityId, string comments)
        {
            var approvals = await LoadApprovalsFromFileAsync();
            var approval = approvals.FirstOrDefault(a => string.Equals(a.EntityType, entityType, StringComparison.OrdinalIgnoreCase) && string.Equals(a.EntityId, entityId, StringComparison.OrdinalIgnoreCase) && string.Equals(a.ApprovalName, approvalName, StringComparison.OrdinalIgnoreCase));
            if (approval != null)
            {
                approval.ApprovedBy = "Player 001";
                approval.ApprovedOn = DateTime.UtcNow;
                approval.Status = "Approved";
                approval.Comments = comments;
                await SaveApprovalsToFileAsync(approvals);
            }
            return approval;
        }

        [KernelFunction("Reject")]
        [Description("Rejects an approval for an entity type and entityid with comments")]
        public async Task<Approval> RejectAsync(string approvalName, string entityType, string entityId, string comments)
        {
            var approvals = await LoadApprovalsFromFileAsync();
            var approval = approvals.FirstOrDefault(a => string.Equals(a.EntityType, entityType, StringComparison.OrdinalIgnoreCase) && string.Equals(a.EntityId, entityId, StringComparison.OrdinalIgnoreCase) && string.Equals(a.ApprovalName, approvalName, StringComparison.OrdinalIgnoreCase));
            if (approval != null)
            {
                approval.ApprovedBy = "Player 001";
                approval.ApprovedOn = approval.ApprovedOn == null ? null : DateTime.UtcNow;
                approval.Status = "Rejected";
                approval.Comments = comments;
                await SaveApprovalsToFileAsync(approvals);
            }
            return approval;
        }

        private async Task<List<Approval>> LoadApprovalsFromFileAsync()
        {
            if (!File.Exists(_approvalsFilePath))
            {
                return new List<Approval>();
            }

            var json = await File.ReadAllTextAsync(_approvalsFilePath);
            return JsonSerializer.Deserialize<List<Approval>>(json) ?? new List<Approval>();
        }

        private async Task SaveApprovalsToFileAsync(List<Approval> approvals)
        {
            var json = JsonSerializer.Serialize(approvals, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_approvalsFilePath, json);
        }
    }
}
