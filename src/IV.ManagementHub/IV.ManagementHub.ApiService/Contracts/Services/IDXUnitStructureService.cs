using IV.ManagementHub.Common.Models;

namespace IV.ManagementHub.ApiService.Contracts.Services
{
    public interface IDXUnitStructureService
    {
        Task<DXUnitDefinitionStructure> GetAsync(string name, CancellationToken ct = default);
    }
}
