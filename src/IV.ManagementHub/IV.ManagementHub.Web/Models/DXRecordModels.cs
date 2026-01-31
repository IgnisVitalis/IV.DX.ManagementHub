namespace IV.ManagementHub.Web.Models
{
    public sealed class DXUnitRecordModel
    {
        public string Type { get; }
        public DXRecordItem MainItem { get; }
        public IReadOnlyDictionary<string, DXRecordSingleElement> SingleElements => _singleElements;
        public IReadOnlyDictionary<string, DXRecordMultiElement> MultiElements => _multiElements;

        private readonly Dictionary<string, DXRecordSingleElement> _singleElements;
        private readonly Dictionary<string, DXRecordMultiElement> _multiElements;

        public DXUnitRecordModel(
            string type,
            DXRecordItem mainItem,
            IDictionary<string, DXRecordSingleElement>? singleElements = null,
            IDictionary<string, DXRecordMultiElement>? multiElements = null)
        {
            Type = type;
            MainItem = mainItem;
            _singleElements = new Dictionary<string, DXRecordSingleElement>(
                singleElements ?? new Dictionary<string, DXRecordSingleElement>(),
                StringComparer.OrdinalIgnoreCase);
            _multiElements = new Dictionary<string, DXRecordMultiElement>(
                multiElements ?? new Dictionary<string, DXRecordMultiElement>(),
                StringComparer.OrdinalIgnoreCase);
        }

        public DXRecordSingleElement? GetSingleElement(string name)
            => _singleElements.TryGetValue(name, out var value) ? value : null;

        public DXRecordMultiElement? GetMultiElement(string name)
            => _multiElements.TryGetValue(name, out var value) ? value : null;

        public void SetSingleElement(DXRecordSingleElement element)
            => _singleElements[element.Name] = element;

        public void SetMultiElement(DXRecordMultiElement element)
            => _multiElements[element.Name] = element;
    }

    public sealed class DXRecordItem
    {
        public string Type { get; }
        public Guid ID { get; set; }
        public Guid DXUnitID { get; set; }
        public DateTime TimeStamp { get; set; }
        public IDictionary<string, object?> Content { get; }

        public DXRecordItem(
            string type,
            Guid id,
            Guid dxUnitId,
            DateTime timeStamp,
            IDictionary<string, object?> content)
        {
            Type = type;
            ID = id;
            DXUnitID = dxUnitId;
            TimeStamp = timeStamp;
            Content = content;
        }
    }

    public sealed class DXRecordSingleElement
    {
        public string Name { get; }
        public DXRecordItem? Item { get; set; }

        public DXRecordSingleElement(string name, DXRecordItem? item)
        {
            Name = name;
            Item = item;
        }
    }

    public sealed class DXRecordMultiElement
    {
        private readonly HashSet<Guid> _originalIds;

        public string Name { get; }
        public IList<DXRecordItem> Announced { get; }
        public IList<DXRecordItem> Deleted { get; }

        public DXRecordMultiElement(string name, IEnumerable<DXRecordItem>? announced = null, IEnumerable<DXRecordItem>? deleted = null)
        {
            Name = name;
            Announced = new List<DXRecordItem>(announced ?? Enumerable.Empty<DXRecordItem>());
            Deleted = new List<DXRecordItem>(deleted ?? Enumerable.Empty<DXRecordItem>());
            _originalIds = new HashSet<Guid>(Announced.Select(x => x.ID));
        }

        public void Add(DXRecordItem item)
        {
            Announced.Add(item);
        }

        public void Remove(DXRecordItem item)
        {
            if (_originalIds.Contains(item.ID))
            {
                if (!Deleted.Any(x => x.ID == item.ID))
                    Deleted.Add(item);
            }

            Announced.Remove(item);
        }
    }
}
