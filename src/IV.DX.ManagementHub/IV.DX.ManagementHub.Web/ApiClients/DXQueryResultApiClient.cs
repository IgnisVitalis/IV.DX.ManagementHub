using IV.DX.ManagementHub.Web.Models;
using IV.DX.ManagementHub.Web.Services;
using Microsoft.JSInterop;

internal class DXQueryResultApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime)
{
    public virtual async Task<DXQueryResult> GetAsync(Guid dxQueryId, Guid? dxFilterId, CancellationToken ct = default)
    {
        var requestUri = clientProvider.GetQueryResultUri(dxQueryId, dxFilterId);
        var http = await clientProvider.GetClientAsync(ct);
        var result = await http.GetAsync(requestUri, ct);

        var str = await result.Content.ReadAsStringAsync(ct);

        return DXQueryResult.Parse(str);
    }
}
