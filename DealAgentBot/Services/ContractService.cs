using DealAgentBot.Models;
using System.Text.Json;

namespace DealAgentBot.Services
{
    class ContractService
    {
        private readonly string _contractsFilePath = "MockData/contracts.json";

        public async Task<IEnumerable<Contract>> GetAllContractsAsync()
        {
            return await LoadContractsFromFileAsync();
        }

        public async Task<Contract> GetContractByIdAsync(string contractId)
        {
            var contracts = await LoadContractsFromFileAsync();
            return contracts.FirstOrDefault(c => c.ContractId == contractId);
        }

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
