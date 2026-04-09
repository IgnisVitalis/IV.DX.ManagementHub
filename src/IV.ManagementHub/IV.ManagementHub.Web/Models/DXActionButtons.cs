using IV.DX.Presentation.Application.Contracts.Models.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace IV.ManagementHub.Web.Models
{
    public sealed class DXActionButton
    {
        public string Key { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string? IconKey { get; init; }
        public Appearance Appearance { get; init; } = Appearance.Neutral;
        public Color IconColor { get; init; } = Color.Fill;
        public bool Visible { get; init; } = true;
        public bool Disabled { get; init; }
        public EventCallback OnClick { get; init; }
    }

    public sealed class DXActionButtonContext
    {
        public Guid EntityID { get; init; }
        public string TypeName { get; init; } = string.Empty;
        public string AppKey { get; init; } = string.Empty;
        public EventCallback OnEdit { get; init; }
        public EventCallback OnExport { get; init; }
        public EventCallback OnDelete { get; init; }
    }

    public static class DXActionButtonKeys
    {
        public const string Edit = "edit";
        public const string Export = "export";
        public const string Delete = "delete";
        public const string Navigate = "navigate";
        public const string Add = "add";
        public const string Refresh = "refresh";
        public const string Settings = "settings";
        public const string View = "view";
        public const string Search = "search";
        public const string Archive = "archive";
    }

    public static class DXActionButtonMapper
    {
        public static string ToIconKey(DXPActionIconEnum icon) => icon switch
        {
            DXPActionIconEnum.Edit     => DXActionButtonKeys.Edit,
            DXPActionIconEnum.Delete   => DXActionButtonKeys.Delete,
            DXPActionIconEnum.Export   => DXActionButtonKeys.Export,
            DXPActionIconEnum.Navigate => DXActionButtonKeys.Navigate,
            DXPActionIconEnum.Add      => DXActionButtonKeys.Add,
            DXPActionIconEnum.Refresh  => DXActionButtonKeys.Refresh,
            DXPActionIconEnum.Settings => DXActionButtonKeys.Settings,
            DXPActionIconEnum.View     => DXActionButtonKeys.View,
            DXPActionIconEnum.Search   => DXActionButtonKeys.Search,
            DXPActionIconEnum.Archive  => DXActionButtonKeys.Archive,
            _                          => string.Empty
        };

        public static Appearance ToAppearance(DXPActionEmphasisEnum emphasis) => emphasis switch
        {
            DXPActionEmphasisEnum.Accent  => Appearance.Accent,
            DXPActionEmphasisEnum.Danger  => Appearance.Neutral,
            _                             => Appearance.Neutral
        };

        public static Color ToIconColor(DXPActionEmphasisEnum emphasis) => emphasis switch
        {
            DXPActionEmphasisEnum.Accent   => Color.Fill,    // white/contrast on filled accent background
            DXPActionEmphasisEnum.Danger   => Color.Error,
            DXPActionEmphasisEnum.Info     => Color.Info,
            DXPActionEmphasisEnum.Warning  => Color.Warning,
            DXPActionEmphasisEnum.Success  => Color.Success,
            _                              => Color.Fill
        };
    }

    public static class DXActionButtonRegistry
    {
        private static readonly string[] _defaultActionKeys =
        [
            DXActionButtonKeys.Edit,
            DXActionButtonKeys.Export,
            DXActionButtonKeys.Delete
        ];

        private static readonly IReadOnlyDictionary<string, Func<DXActionButtonContext, DXActionButton?>> _factories =
            new Dictionary<string, Func<DXActionButtonContext, DXActionButton?>>(StringComparer.OrdinalIgnoreCase)
            {
                [DXActionButtonKeys.Edit] = context => !context.OnEdit.HasDelegate
                    ? null
                    : new DXActionButton
                    {
                        Key = DXActionButtonKeys.Edit,
                        Label = "Edit",
                        IconKey = DXActionButtonKeys.Edit,
                        IconColor = Color.Accent,
                        OnClick = context.OnEdit
                    },
                [DXActionButtonKeys.Export] = context => !context.OnExport.HasDelegate
                    ? null
                    : new DXActionButton
                    {
                        Key = DXActionButtonKeys.Export,
                        Label = "Export",
                        IconKey = DXActionButtonKeys.Export,
                        IconColor = Color.Info,
                        OnClick = context.OnExport
                    },
                [DXActionButtonKeys.Delete] = context => !context.OnDelete.HasDelegate
                    ? null
                    : new DXActionButton
                    {
                        Key = DXActionButtonKeys.Delete,
                        Label = "Delete",
                        IconKey = DXActionButtonKeys.Delete,
                        IconColor = Color.Error,
                        OnClick = context.OnDelete
                    }
            };

        public static IReadOnlyList<string> DefaultActionKeys => _defaultActionKeys;

        public static IReadOnlyList<DXActionButton> Build(IEnumerable<string>? actionKeys, DXActionButtonContext context)
        {
            if (actionKeys is null)
                return [];

            var actions = new List<DXActionButton>();

            foreach (var actionKey in actionKeys)
            {
                if (string.IsNullOrWhiteSpace(actionKey))
                    continue;

                if (!_factories.TryGetValue(actionKey, out var factory))
                    continue;

                var action = factory(context);
                if (action is not null)
                    actions.Add(action);
            }

            return actions;
        }
    }
}
