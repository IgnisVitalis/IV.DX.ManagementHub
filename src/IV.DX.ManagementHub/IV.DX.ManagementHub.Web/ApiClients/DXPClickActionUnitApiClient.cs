using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DX.Presentation.Application.Contracts.Models;
using IV.DX.ManagementHub.Web.Services;
using Microsoft.JSInterop;

internal class DXPClickActionUnitApiClient(IInstanceClientProvider clientProvider, IJSRuntime JSRuntime)
    : DXUnitBaseApiClient<DXPClickActionUnit>(clientProvider, JSRuntime) { }
