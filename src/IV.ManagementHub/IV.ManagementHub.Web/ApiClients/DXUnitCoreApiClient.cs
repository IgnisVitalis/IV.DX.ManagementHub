using IV.DX.Kernel.Models;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace IV.ManagementHub.Web.ApiClients
{
    public sealed class DXUnitCoreApiClient(HttpClient httpClient, IJSRuntime JSRuntime)
    {
        public async Task<JObject> SaveAsync(JObject jObject, CancellationToken cancellationToken = default)
        {
            string typeName = jObject.Value<string>("S_Type");

            var content = new StringContent(jObject.ToString(), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"api/v1.0/{typeName}", content, cancellationToken);

            var str = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JObject.Parse(str);

            return result;
        }
        public async Task<DXDataBlock<DXUnitRecord>?> SaveRecordAsync(DXDataBlock<DXUnitRecord> block, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(block);

            var typeName = block.Meta?.Type;
            if (string.IsNullOrWhiteSpace(typeName))
                throw new InvalidOperationException("DXDataBlock.Meta.Type is required.");

            var json = JsonConvert.SerializeObject(block);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"api/v1.0/{typeName}", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(str))
                return null;

            return JsonConvert.DeserializeObject<DXDataBlock<DXUnitRecord>>(str);
        }
        public async Task<JObject> Get(string typeName, Guid id, CancellationToken cancellationToken = default)
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
        public async Task<DXDataBlock<DXUnitRecord>?> GetRecord(string typeName, Guid id, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync($"api/v1.0/{typeName}/{id}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(str))
                return null;

            return JsonConvert.DeserializeObject<DXDataBlock<DXUnitRecord>>(str);
        }
        public async Task<JObject> GetDataDefinition(string typeName, CancellationToken cancellationToken = default)
        {
            var items = await this.GetItems("DXUnitDefinitionUnit", $"Name = '{typeName}'");

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


        public async Task<JObject> GetEntityDataDefinition(string typeName, CancellationToken cancellationToken = default)
        {
            var items = await this.GetItems("DXUnitDefinitionUnit", $"Name = '{typeName}'");

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

        public async Task<IEnumerable<JObject>> GetItems(string typeName, string? query = default, CancellationToken cancellationToken = default)
        {
            var request = $"api/v1.0/{typeName}";

            if(!string.IsNullOrEmpty(query))
            {
                request += $"?filter={query}";
            }

            var response = await httpClient.GetAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync(cancellationToken);

            var items = JArray.Parse(str);

            return items.Select(x => (JObject)x).ToList();
        }      

        public async Task DeleteAsync(string typeName, Guid itemID, CancellationToken cancellationToken = default)
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

        public async Task ExportAsync(string typeName, IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idArray = ids?.Where(x => x != default).Distinct().ToArray() ?? Array.Empty<Guid>();
            if (idArray.Length == 0)
                return;

            var items = await GetByIdsAsync(typeName, idArray, cancellationToken);
            var jArray = new JArray(items);

            var formatted = jArray.ToString(Formatting.Indented);
            var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");

            await JSRuntime.InvokeVoidAsync(
                "downloadJsonFile",
                $"01_01_0001_UIUX_{typeName}_{idArray.Length}_items_{stamp}.dat",
                formatted);
        }

        public async Task<IReadOnlyList<JObject>> GetByIdsAsync(string typeName, IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idArray = ids?.Where(x => x != default).Distinct().ToArray() ?? Array.Empty<Guid>();
            if (idArray.Length == 0)
                return Array.Empty<JObject>();

            var content = new StringContent(JsonConvert.SerializeObject(idArray), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"api/v1.0/{typeName}/by-ids", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync(cancellationToken);

            var jArray = JsonConvert.DeserializeObject<JArray>(str) ?? new JArray();
            return jArray.OfType<JObject>().ToList();
        }
    }
}
