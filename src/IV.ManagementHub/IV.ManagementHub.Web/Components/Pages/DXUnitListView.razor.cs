using IV.DataProvider.WebApp.Services.Web.ApiClients;
using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DX.Kernel.Models;
using IV.ManagementHub.Web.ApiClients;
using IV.ManagementHub.Web.Components.Custom.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Newtonsoft.Json.Linq;
using System.Data;

namespace IV.ManagementHub.Web.Components.Pages
{
    public partial class DXUnitListView : ManagementHubComponentBase
    {
        [Inject]
        IApiClientResolver Resolver { get; set; } = default!;

        DXUnitCoreApiClient coreApi = default!;


        [Parameter, EditorRequired] public string DXUnitTypeName { get; set; } = default!;


        protected override async Task OnParametersSetAsync()
        {
            coreApi = Resolver.Get<DXUnitCoreApiClient>(base.AppKey);

            await LoadDataAsync(true);
        }

        private bool _isInitialLoading;
        private bool _isRefreshing;
        private bool _isSaving;
        private bool _collapse = true;

        private IEnumerable<JObject> dxUnits = new List<JObject>();
        private DataTable values;

        private Guid selectedItemID { get; set; }


        private bool isEditing = false;
        private bool showDetails = false;
        private string editBlockName = string.Empty;

        private async Task LoadDataAsync(bool initial)
        {
            if (initial) _isInitialLoading = true; else _isRefreshing = true;

            try
            {
                dxUnits = await coreApi.GetItems(DXUnitTypeName, string.Empty);

                values = ToTable(dxUnits, new[] { "ID", "DXObjectDefinitionMainElement.Name" });
            }
            finally
            {
                _isInitialLoading = false;
                _isRefreshing = false;
                StateHasChanged();
            }
        }

        private async Task OnDeleted()
        {
            await this.LoadDataAsync(false);

            this.OnClosed();
        }

        private async Task OpenEditDialog(Guid id)
        {
            if (id != default(Guid))
            {
                selectedItemID = id;
            }
            else
            {
                selectedItemID = Guid.NewGuid();
            }

            _showDialog = true;
        }

        private async Task OpenPanelRightAsync(Guid selectedBlockID)
        {
            //selectedBlock = await LoadDetailsAsync(selectedBlockID);

            _collapse = false;
        }

        private async Task<JObject> LoadDetailsAsync(Guid id)
        {
            var item = await coreApi.Get(DXUnitTypeName, id);
            return item;
        }

        private void OnClosed()
        {
            _collapse = true;
        }

        bool _showDialog = false;

        private void CloseDialog()
        {
            _showDialog = false;
        }

        private async Task OnSaved()
        {
            await this.LoadDataAsync(false);

            this.CloseDialog();
        }

        Orientation orientation = Orientation.Horizontal;

        private void OnResizedHandler(SplitterResizedEventArgs args)
        {

        }

        public static DataTable ToTable(IEnumerable<JObject> array, IEnumerable<string> dotPaths)
        {
            if (array is null) throw new ArgumentNullException(nameof(array));
            if (dotPaths is null) throw new ArgumentNullException(nameof(dotPaths));

            var paths = dotPaths.ToList();
            var colNames = MakeUniqueColumnNames(paths);

            var table = new DataTable();
            foreach (var name in colNames.Values)
                table.Columns.Add(name, typeof(object));

            foreach (var item in array)
            {
                var row = table.NewRow();
                foreach (var p in paths)
                {
                    var token = item.SelectToken(p);
                    row[colNames[p]] = token is JValue v ? v.Value ?? DBNull.Value
                                      : token is null ? DBNull.Value
                                      : token.ToString();
                }
                table.Rows.Add(row);
            }

            return table;
        }

        private static Dictionary<string, string> MakeUniqueColumnNames(IList<string> paths)
        {
            // базовые хвосты
            var tails = paths.ToDictionary(p => p, p => p.Split('.').Last());

            // ищем дубликаты
            var groups = tails.GroupBy(kv => kv.Value)
                              .Where(g => g.Count() > 1)
                              .ToList();

            if (!groups.Any()) return tails;

            var result = new Dictionary<string, string>(tails);
            foreach (var g in groups)
            {
                // для каждой группы с одинаковым хвостом расширяем имя слева направо
                var members = g.Select(kv => kv.Key).ToList();
                var expanded = members.ToDictionary(m => m, m => new List<string> { g.Key }); // начально = хвост

                int step = 2; // берём предпоследний сегмент, потом ещё и т.д.
                bool unique = false;

                while (!unique)
                {
                    var proposals = new Dictionary<string, string>();
                    foreach (var m in members)
                    {
                        var segs = m.Split('.');
                        var take = Math.Min(step, segs.Length);
                        var name = string.Join("_", segs.Skip(segs.Length - take)); // last, prev.last, ...
                        proposals[m] = name;
                    }

                    // проверяем уникальность
                    if (proposals.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count() == proposals.Count)
                    {
                        foreach (var m in members) result[m] = proposals[m];
                        unique = true;
                    }
                    else
                    {
                        step++;
                        // на всякий пожарный предел
                        if (step > 10) // достаточный предел для обычных путей
                        {
                            foreach (var m in members) result[m] = m.Replace('.', '_');
                            unique = true;
                        }
                    }
                }
            }

            return result;
        }

        public Guid GetGuid(DataRow row, string columnName)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            if (!row.Table.Columns.Contains(columnName))
                throw new ArgumentException($"Column '{columnName}' not found.", nameof(columnName));

            var value = row[columnName];

            if (value == DBNull.Value || value == null)
                return default(Guid);

            // если уже Guid
            if (value is Guid g)
                return g;

            // если строка
            if (value is string s && Guid.TryParse(s, out g))
                return g;

            // если байты
            if (value is byte[] bytes && bytes.Length == 16)
                return new Guid(bytes);

            throw new InvalidCastException($"Cannot convert value in '{columnName}' to Guid.");
        }

        public IQueryable<DataRow> AsQueryableRows(DataTable table)
        {
            return table?.AsEnumerable()?.AsQueryable() ?? Enumerable.Empty<DataRow>().AsQueryable();
        }
    }
}