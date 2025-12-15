using IV.DX.Kernel.Models;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace IV.DataProvider.WebApp.Services.Web.ApiClients
{
    internal abstract class DXUnitBaseApiClient<T>(HttpClient httpClient, IJSRuntime JSRuntime)
        : DXUnitGenericApiClient<T>(httpClient, JSRuntime)
        where T : DXUnit
    {
        private readonly string typeName = DXUnit.GetTypeName<T>();

        public virtual async Task<IEnumerable<T>> GetItemsAsync(string? dxFilter = default, CancellationToken cancellationToken = default)
        {
            var requestUri = $"api/v1.0/{typeName}";

            if (dxFilter != default)
            {
                requestUri += $"?filter={dxFilter}";
            }

            var result = await httpClient.GetAsync(requestUri, cancellationToken);

            var str = await result.Content.ReadAsStringAsync(cancellationToken);

            var items = DXUnit.ParseItems<T>(str);

            return items;
        }

        public virtual async Task<T> Get(Guid id, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync($"api/v1.0/{typeName}/{id}");

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

            var result = await httpClient.PostAsync($"api/v1.0/{typeName}", content, cancellationToken);

            var str = await result.Content.ReadAsStringAsync(cancellationToken);

            var item = DXUnit.Parse<T>(str);

            return item;
        }

        public virtual async Task DeleteAsync(T item, CancellationToken cancellationToken = default)
        {
            var id = item.ID;

            var result = await httpClient.DeleteAsync($"api/v1.0/{typeName}/{id}", cancellationToken);
        }

        public async Task ExportEntityAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await httpClient.GetAsync($"api/v1.0/{typeName}/{id}", cancellationToken);

            var str = await result.Content.ReadAsStringAsync(cancellationToken);

            var jObject = JsonConvert.DeserializeObject<JObject>(str);

            var formatted = jObject.ToString(Formatting.Indented);

            var name = (string)jObject["Name"];

            await JSRuntime.InvokeVoidAsync("downloadJsonFile", $"01_01_0001_UIUX_{name}.dat", formatted);
        }
    }
}