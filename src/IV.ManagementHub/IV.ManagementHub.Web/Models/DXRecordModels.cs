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
        private readonly Dictionary<Guid, OriginalItem> _originalItems;
        private readonly bool _trackOriginal;

        public string Name { get; }
        public IList<DXRecordItem> Announced { get; }
        public IList<DXRecordItem> Deleted { get; }

        public DXRecordMultiElement(
            string name,
            IEnumerable<DXRecordItem>? announced = null,
            IEnumerable<DXRecordItem>? deleted = null,
            bool trackOriginal = true)
        {
            Name = name;
            Announced = new List<DXRecordItem>(announced ?? Enumerable.Empty<DXRecordItem>());
            Deleted = new List<DXRecordItem>(deleted ?? Enumerable.Empty<DXRecordItem>());
            _trackOriginal = trackOriginal;
            _originalIds = trackOriginal
                ? new HashSet<Guid>(Announced.Select(x => x.ID))
                : new HashSet<Guid>();
            _originalItems = trackOriginal
                ? Announced.ToDictionary(x => x.ID, x => OriginalItem.From(x))
                : new Dictionary<Guid, OriginalItem>();
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

        public IEnumerable<DXRecordItem> GetItems()
        {
            foreach (var item in Announced)
            {
                if (IsModified(item))
                    yield return item;
            }
        }

        private bool IsModified(DXRecordItem item)
        {
            if (!_trackOriginal)
                return true;

            if (!_originalItems.TryGetValue(item.ID, out var original))
                return true;

            return !original.EqualsItem(item);
        }

        private sealed class OriginalItem
        {
            public Guid DXUnitID { get; }
            public DateTime TimeStamp { get; }
            public IDictionary<string, object?> Content { get; }

            private OriginalItem(Guid dxUnitId, DateTime timeStamp, IDictionary<string, object?> content)
            {
                DXUnitID = dxUnitId;
                TimeStamp = timeStamp;
                Content = content;
            }

            public static OriginalItem From(DXRecordItem item)
            {
                return new OriginalItem(
                    item.DXUnitID,
                    item.TimeStamp,
                    CopyContent(item.Content));
            }

            public bool EqualsItem(DXRecordItem item)
            {
                if (item.DXUnitID != DXUnitID)
                    return false;

                if (item.TimeStamp != TimeStamp)
                    return false;

                return ContentEquals(Content, item.Content);
            }

            private static IDictionary<string, object?> CopyContent(IDictionary<string, object?> content)
            {
                var copy = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in content)
                {
                    copy[kvp.Key] = CopyValue(kvp.Value);
                }

                return copy;
            }

            private static object? CopyValue(object? value)
            {
                if (value == null)
                    return null;

                if (value is byte[] bytes)
                    return bytes.ToArray();

                if (value is ICloneable cloneable)
                    return cloneable.Clone();

                return value;
            }

            private static bool ContentEquals(IDictionary<string, object?> left, IDictionary<string, object?> right)
            {
                if (left.Count != right.Count)
                    return false;

                foreach (var kvp in left)
                {
                    if (!right.TryGetValue(kvp.Key, out var rightValue))
                        return false;

                    if (!ValuesEqual(kvp.Value, rightValue))
                        return false;
                }

                return true;
            }

            private static bool ValuesEqual(object? left, object? right)
            {
                if (ReferenceEquals(left, right))
                    return true;

                if (left == null || right == null)
                    return false;

                if (left is byte[] leftBytes && right is byte[] rightBytes)
                    return leftBytes.SequenceEqual(rightBytes);

                return left.Equals(right);
            }
        }
    }
}
