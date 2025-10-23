namespace IV.DataProvider.WebApp.Services.Web.Contracts
{
    internal interface IApiClientResolver
    {
        T Get<T>(string sourceKey) where T : class;
    }
}