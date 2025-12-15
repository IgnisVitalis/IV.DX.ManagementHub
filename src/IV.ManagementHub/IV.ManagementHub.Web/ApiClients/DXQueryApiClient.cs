using IV.ManagementHub.Web.Models;
using Microsoft.JSInterop;

internal class DXQueryApiClient(HttpClient httpClient, IJSRuntime JSRuntime)
{
    public virtual async Task<DXQueryResult> GetAsync(Guid dxQueryID, CancellationToken ct = default)
    {
        var requestUri = $"api/v1.0/DXQuery/{dxQueryID}";        

        var result = await httpClient.GetAsync(requestUri, ct);

        var str = await result.Content.ReadAsStringAsync(ct);

        return DXQueryResult.Parse(str);
    }
}