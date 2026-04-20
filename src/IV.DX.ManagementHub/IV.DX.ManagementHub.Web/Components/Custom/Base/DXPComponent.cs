using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Presentation.Application.Contracts.Models;
using Microsoft.AspNetCore.Components;

namespace IV.DX.ManagementHub.Web.Components.Custom.Base
{
    public abstract class DXPComponent<TUnit, TClient> : ManagementHubComponentBase
        where TUnit : DXPComponentUnit
        where TClient : DXUnitBaseApiClient<TUnit>
    {
        [Inject]
        protected IApiClientResolver Resolver { get; set; } = default!;

        protected TClient ApiClient { get; private set; } = default!;

        protected Guid _dxpComponentID;

        [Parameter, EditorRequired]
        public string DXPComponentID
        {
            get => _dxpComponentID.ToString();
            set => _dxpComponentID = Guid.Parse(value);
        }

        protected TUnit? ComponentUnit { get; private set; }
        protected bool IsInitialLoading { get; private set; }
        protected bool IsRefreshing { get; private set; }
        protected string? LoadErrorMessage { get; private set; }

        protected override async Task OnParametersSetAsync()
        {
            ApiClient = await Resolver.GetAsync<TClient>(AppKey);

            IsInitialLoading = true;
            LoadErrorMessage = null;

            try
            {
                ComponentUnit = await ApiClient.Get(_dxpComponentID);
                if (ComponentUnit is null)
                {
                    LoadErrorMessage = "Component definition not found.";
                    return;
                }
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                LoadErrorMessage = ex.Message;
            }
            finally
            {
                IsInitialLoading = false;
                StateHasChanged();
            }
        }

        protected abstract Task LoadDataAsync();

        protected async Task ReloadAsync()
        {
            IsRefreshing = true;
            LoadErrorMessage = null;
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                LoadErrorMessage = ex.Message;
            }
            finally
            {
                IsRefreshing = false;
                StateHasChanged();
            }
        }
    }
}
