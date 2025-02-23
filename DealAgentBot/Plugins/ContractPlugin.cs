using DealAgentBot.Models;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace DealAgentBot.Plugins
{
    internal sealed class ContractPlugin
    {
        private readonly string _contractsFilePath = "MockData/contracts.json";


        [KernelFunction("GetAllContracts")]
        [Description("Gets all contracts")]
        public async Task<IEnumerable<Contract>> GetAllContractsAsync()
        {
            return await LoadContractsFromFileAsync();
        }

        [KernelFunction("GetContractById")]
        [Description("Gets the details of a contract by contract Id")]
        public async Task<Contract> GetContractByIdAsync(string contractId)
        {
            var contracts = await LoadContractsFromFileAsync();
            return contracts.FirstOrDefault(c => c.ContractId == contractId);
        }

        [KernelFunction("GetContractByName")]
        [Description("Gets the details of a contract by contract name")]
        public async Task<IEnumerable<Contract>> GetContractsByContractNameAsync(string contractName)
        {
            var contracts = await LoadContractsFromFileAsync();
            return contracts.Where(c => string.Equals(c.ContractName, contractName, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<List<Contract>> LoadContractsFromFileAsync()
        {
            if (!File.Exists(_contractsFilePath))
            {
                return new List<Contract>();
            }

            var json = await File.ReadAllTextAsync(_contractsFilePath);
            return JsonSerializer.Deserialize<List<Contract>>(json) ?? new List<Contract>();
        }
    }
}
