using IV.ManagementHub.Web.Models;
using Microsoft.JSInterop;

internal class DXQueryResultApiClient(HttpClient httpClient, IJSRuntime JSRuntime)
{
    public virtual async Task<DXQueryResult> GetAsync(Guid dxQueryID, Guid? dxFilterID, CancellationToken ct = default)
    {
        var requestUri = $"api/DXQueryResult/{dxQueryID}";

        if (dxFilterID.HasValue)
        {
            requestUri += $"/{dxFilterID}";
        }

        var result = await httpClient.GetAsync(requestUri, ct);

        var str = await result.Content.ReadAsStringAsync(ct);

        return DXQueryResult.Parse(str);
    }
}