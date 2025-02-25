using DealAgentBot.Models;
using DealAgentBot.Services;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace DealAgentBot.Plugins
{
    internal class ApprovalPlugin
    {
        private readonly ApprovalService _approvalService;

        public ApprovalPlugin()
        {
            _approvalService = new ApprovalService();
        }

        [KernelFunction("GetApprovals")]
        public async Task<IEnumerable<Approval>> GetApprovalsAsync(string entityId, string entityType)
        {
            return await _approvalService.GetApprovalsAsync(entityId, entityType);
        }

        [KernelFunction("Approve")]
        [Description("Approves an approval for an entity type and entityid with comments")]
        public async Task<Approval> ApproveAsync(string approvalName, string entityType, string entityId, string comments)
        {
            return await _approvalService.ApproveAsync(approvalName, entityType, entityId, comments);
        }

        [KernelFunction("Reject")]
        [Description("Rejects an approval for an entity type and entityid with comments")]
        public async Task<Approval> RejectAsync(string approvalName, string entityType, string entityId, string comments)
        {
            return await _approvalService.RejectAsync(approvalName, entityType, entityId, comments);
        }
    }
}
