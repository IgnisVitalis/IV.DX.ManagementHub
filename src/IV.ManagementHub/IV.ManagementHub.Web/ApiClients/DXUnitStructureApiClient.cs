using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.Services;
using Microsoft.JSInterop;
using Newtonsoft.Json;

namespace IV.ManagementHub.Web.ApiClients
{
    public class DXUnitStructureApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime)
    {
        public virtual async Task<DXModelDefinition> GetAsync(string typeName, CancellationToken cancellationToken = default)
        {
            var http = await clientProvider.GetClientAsync(cancellationToken);
            var response = await http.GetAsync(clientProvider.GetUnitStructureUri(typeName), cancellationToken);

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
