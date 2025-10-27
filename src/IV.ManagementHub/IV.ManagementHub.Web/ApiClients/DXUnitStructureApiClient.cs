using IV.ManagementHub.Common.Models;
using Microsoft.JSInterop;
using Newtonsoft.Json;

namespace IV.ManagementHub.Web.ApiClients
{
    public class DXUnitStructureApiClient(HttpClient httpClient, IJSRuntime JSRuntime)
    {
        public virtual async Task<DXModelDefinition> GetAsync(string typeName, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync($"api/v1.0/DXUnitStructure/{typeName}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            var item = JsonConvert.DeserializeObject<DXModelDefinition>(json);

            return item;
        }
    }
}
