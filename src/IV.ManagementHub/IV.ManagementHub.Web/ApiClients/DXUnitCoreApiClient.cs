using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IV.ManagementHub.Web.ApiClients
{
    public class DXUnitCoreApiClient(HttpClient httpClient, IJSRuntime JSRuntime)
    {
        public virtual async Task<JObject> Get(string typeName, Guid id, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync($"api/v1.0/{typeName}/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync(cancellationToken);

            var item = JObject.Parse(str);

            return item;
        }

        public virtual async Task<JObject> GetDataDefinition(string typeName, CancellationToken cancellationToken = default)
        {
            var items = await this.GetItems("DXUnitDefinitionUnit", $"DXObjectDefinitionMainElement.Name = '{typeName}'");

            if (items.Count() == 0)
            {
                throw new Exception($"There are no dx unit definition with name '{typeName}'");
            }

            if (items.Count() > 1)
            {
                throw new Exception($"There are more than 1 entry of data definition with name '{typeName}'");
            }

            return items.Single();
        }


        public virtual async Task<JObject> GetEntityDataDefinition(string typeName, CancellationToken cancellationToken = default)
        {
            var items = await this.GetItems("DXUnitDefinitionUnit", $"DXObjectDefinitionMainElement.Name = '{typeName}'");

            if (items.Count() == 0)
            {
                throw new Exception($"There are no dx unit definition with name '{typeName}'");
            }

            if (items.Count() > 1)
            {
                throw new Exception($"There are more than 1 entry of data definition with name '{typeName}'");
            }

            return items.Single();
        }

        public virtual async Task<IEnumerable<JObject>> GetItems(string typeName, string query, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync($"api/v1.0/{typeName}?filter={query}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync(cancellationToken);

            var items = JArray.Parse(str);

            return items.Select(x => (JObject)x).ToList();
        }

        public virtual async Task DeleteAsync(string typeName, Guid itemID, CancellationToken cancellationToken = default)
        {
            var result = await httpClient.DeleteAsync($"api/v1.0/{typeName}/{itemID}", cancellationToken);
        }

        public async Task ExportAsync(string typeName, Guid id, CancellationToken cancellationToken = default)
        {
            var result = await httpClient.GetAsync($"api/v1.0/{typeName}/{id}", cancellationToken);

            var str = await result.Content.ReadAsStringAsync(cancellationToken);

            var jObject = JsonConvert.DeserializeObject<JObject>(str);

            var formatted = jObject.ToString(Formatting.Indented);

            await JSRuntime.InvokeVoidAsync("downloadJsonFile", $"01_01_0001_UIUX_{typeName}_{id}.dat", formatted);
        }
    }
}
