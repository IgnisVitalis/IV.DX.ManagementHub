using IV.ManagementHub.Common.Models;

namespace IV.ManagementHub.ApiService.Contracts.Services
{
    public interface IDXUnitStructureService
    {
        Task<DXModelDefinition> GetAsync(string name, CancellationToken ct = default);

        Task<DXColumnDefinition> GetDXColumnDefinition(string dxObjectName, string dxColumnName, string? dxSqlFilter = null);
    }
}