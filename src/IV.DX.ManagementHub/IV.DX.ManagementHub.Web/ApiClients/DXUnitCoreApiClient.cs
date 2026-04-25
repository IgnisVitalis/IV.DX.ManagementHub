using IV.DX.Kernel.Models;
using IV.DX.ManagementHub.Web.Services;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace IV.DX.ManagementHub.Web.ApiClients
{
    public sealed class DXUnitCoreApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime)
    {
        public async Task<JObject> SaveAsync(JObject jObject, CancellationToken cancellationToken = default)
        {
            string typeName = jObject.Value<string>("S_Type");

            var content = new StringContent(jObject.ToString(), Encoding.UTF8, "application/json");

            var http = await clientProvider.GetClientAsync(cancellationToken);
            var response = await http.PostAsync(clientProvider.GetSaveUri(typeName), content, cancellationToken);

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

            var http = await clientProvider.GetClientAsync(cancellationToken);
            var response = await http.PostAsync(clientProvider.GetSaveUri(typeName), content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(str))
                return null;

            return JsonConvert.DeserializeObject<DXDataBlock<DXUnitRecord>>(str);
        }

        public async Task<JObject> Get(string typeName, Guid id, CancellationToken cancellationToken = default)
        {
            var http = await clientProvider.GetClientAsync(cancellationToken);
            var response = await http.GetAsync(clientProvider.GetItemUri(typeName, id), cancellationToken);

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
            var http = await clientProvider.GetClientAsync(cancellationToken);
            var response = await http.GetAsync(clientProvider.GetItemUri(typeName, id), cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(str))
                return null;

            return JsonConvert.DeserializeObject<DXDataBlock<DXUnitRecord>>(str);
        }

        public async Task<JObject> GetItems(string typeName, string? query = default, CancellationToken cancellationToken = default)
        {
            var request = clientProvider.GetCollectionUri(typeName, query);
            var http = await clientProvider.GetClientAsync(cancellationToken);
            var response = await http.GetAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync(cancellationToken);

            var items = JObject.Parse(str);

            return items;
        }

        public async Task DeleteAsync(string typeName, Guid itemId, CancellationToken cancellationToken = default)
        {
            var http = await clientProvider.GetClientAsync(cancellationToken);
            var result = await http.DeleteAsync(clientProvider.GetDeleteUri(typeName, itemId), cancellationToken);
            result.EnsureSuccessStatusCode();
        }

        public async Task ExportAsync(string typeName, Guid id, CancellationToken cancellationToken = default)
        {
            var http = await clientProvider.GetClientAsync(cancellationToken);
            var result = await http.GetAsync(clientProvider.GetItemUri(typeName, id), cancellationToken);

            var str = await result.Content.ReadAsStringAsync(cancellationToken);

            var jObject = JsonConvert.DeserializeObject<JObject>(str);

            var formatted = jObject.ToString(Formatting.Indented);

            await JSRuntime.InvokeVoidAsync("downloadJsonFile", $"01_01_0001_UIUX_{typeName}.dx", formatted);
        }

        public async Task<JObject?> GetItemByDefinitionAsync(Guid definitionId, Guid id, CancellationToken cancellationToken = default)
        {
            var http = await clientProvider.GetClientAsync(cancellationToken);
            var response = await http.GetAsync(clientProvider.GetByDefinitionItemUri(definitionId, id), cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync(cancellationToken);
            return JObject.Parse(str);
        }

        public async Task DeleteByDefinitionAsync(Guid definitionId, Guid id, CancellationToken cancellationToken = default)
        {
            var http = await clientProvider.GetClientAsync(cancellationToken);
            var response = await http.DeleteAsync(clientProvider.GetByDefinitionItemUri(definitionId, id), cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task<JObject> GetItemsByDefinitionAsync(Guid definitionId, CancellationToken cancellationToken = default)
        {
            var http = await clientProvider.GetClientAsync(cancellationToken);
            var response = await http.GetAsync(clientProvider.GetByDefinitionUri(definitionId), cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            
                return [];

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JObject.Parse(str);
         
            return result;
        }

        public async Task ExportAsync(string typeName, IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idArray = ids?.Where(x => x != default).Distinct().ToArray() ?? Array.Empty<Guid>();
            if (idArray.Length == 0)
                return;

            var items = await GetByIdsAsync(typeName, idArray, cancellationToken);
            var jArray = new JArray(items);

            var formatted = jArray.ToString(Formatting.Indented);

            await JSRuntime.InvokeVoidAsync(
                "downloadJsonFile",
                $"01_01_0001_UIUX_{typeName}.dx",
                formatted);
        }

        public async Task<IReadOnlyList<JObject>> GetByIdsAsync(string typeName, IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idArray = ids?.Where(x => x != default).Distinct().ToArray() ?? Array.Empty<Guid>();
            if (idArray.Length == 0)
                return Array.Empty<JObject>();

            var content = new StringContent(JsonConvert.SerializeObject(idArray), Encoding.UTF8, "application/json");

            var http = await clientProvider.GetClientAsync(cancellationToken);
            var response = await http.PostAsync(clientProvider.GetByIdsUri(typeName), content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync(cancellationToken);

            var jArray = JsonConvert.DeserializeObject<JArray>(str) ?? new JArray();
            return jArray.OfType<JObject>().ToList();
        }
    }
}
