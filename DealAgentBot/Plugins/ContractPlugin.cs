using DealAgentBot.Models;
using DealAgentBot.Services;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace DealAgentBot.Plugins
{
    internal sealed class ContractPlugin
    {
        private readonly ContractService _contractService;

        public ContractPlugin()
        {
            _contractService = new ContractService();
        }

        [KernelFunction("GetAllContracts")]
        [Description("Gets all contracts")]
        public async Task<IEnumerable<Contract>> GetAllContracts()
        {
            return await _contractService.GetAllContractsAsync();
        }

        [KernelFunction("GetContractById")]
        [Description("Gets the details of a contract by contract Id")]
        public async Task<Contract> GetContractById(string contractId)
        {
            return await _contractService.GetContractByIdAsync(contractId);
        }

        [KernelFunction("GetContractByName")]
        [Description("Gets the details of a contract by contract name")]
        public async Task<IEnumerable<Contract>> GetContractsByContractName(string contractName)
        {
            return await _contractService.GetContractsByContractNameAsync(contractName);
        }
    }
}
