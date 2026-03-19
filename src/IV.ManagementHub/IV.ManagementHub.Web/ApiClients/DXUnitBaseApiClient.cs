using IV.DX.Kernel.Models;
using IV.ManagementHub.Web.Services;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace IV.DataProvider.WebApp.Services.Web.ApiClients
{
    internal abstract class DXUnitBaseApiClient<T>(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime)
        : DXUnitGenericApiClient<T>(clientProvider, JSRuntime)
        where T : DXUnit
    {
        private readonly string typeName = DXUnit.GetTypeName<T>();

        public virtual async Task<IEnumerable<T>> GetItemsAsync(string? dxFilter = default, CancellationToken cancellationToken = default)
        {
            var requestUri = $"api/management/{typeName}";

            if (dxFilter != default)
            {
                requestUri += $"?filter={dxFilter}";
            }

            var http = await ClientProvider.GetClientAsync(cancellationToken);
            var result = await http.GetAsync(requestUri, cancellationToken);
            result.EnsureSuccessStatusCode();

            var str = await result.Content.ReadAsStringAsync(cancellationToken);

            var items = DXUnit.ParseItems<T>(str);

            return items;
        }

        public virtual async Task<T> Get(Guid id, CancellationToken cancellationToken = default)
        {
            var http = await ClientProvider.GetClientAsync(cancellationToken);
            var response = await http.GetAsync($"api/management/{typeName}/{id}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync(cancellationToken);

            var item = DXUnit.Parse<T>(str);

            return item;
        }

        public virtual async Task<T> SaveAsync(T block, CancellationToken cancellationToken = default)
        {
            var serializedBlock = block.ToJObject().ToString();

            var content = new StringContent(serializedBlock, Encoding.UTF8, "application/json");

            var http = await ClientProvider.GetClientAsync(cancellationToken);
            var result = await http.PostAsync($"api/management/{typeName}", content, cancellationToken);

            var str = await result.Content.ReadAsStringAsync(cancellationToken);

            var item = DXUnit.Parse<T>(str);

            return item;
        }

        public virtual async Task DeleteAsync(T item, CancellationToken cancellationToken = default)
        {
            var id = item.ID;

            var http = await ClientProvider.GetClientAsync(cancellationToken);
            await http.DeleteAsync($"api/management/{typeName}/{id}", cancellationToken);
        }

        public async Task ExportEntityAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var http = await ClientProvider.GetClientAsync(cancellationToken);
            var result = await http.GetAsync($"api/management/{typeName}/{id}", cancellationToken);

            var str = await result.Content.ReadAsStringAsync(cancellationToken);

            var jObject = JsonConvert.DeserializeObject<JObject>(str);

            var formatted = jObject.ToString(Formatting.Indented);

            var name = (string)jObject["Name"];

            await JSRuntime.InvokeVoidAsync("downloadJsonFile", $"01_01_0001_UIUX_{name}.dat", formatted);
        }
    }
}
