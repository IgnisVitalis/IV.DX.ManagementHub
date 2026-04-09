using IV.DX.ManagementHub.Web.Models;
using IV.DX.ManagementHub.Web.Services;
using Microsoft.JSInterop;

internal class DXQueryResultApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime)
{
    public virtual async Task<DXQueryResult> GetAsync(Guid dxQueryID, Guid? dxFilterID, CancellationToken ct = default)
    {
        var requestUri = clientProvider.GetQueryResultUri(dxQueryID, dxFilterID);
        var http = await clientProvider.GetClientAsync(ct);
        var result = await http.GetAsync(requestUri, ct);

        var str = await result.Content.ReadAsStringAsync(ct);

        return DXQueryResult.Parse(str);
    }
}
