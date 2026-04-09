namespace IV.DX.ManagementHub.Web.Models.Tree
{
    public static class BiTreeBuilder
    {        
        public static IReadOnlyList<BiTreeNode<T>> BuildForest<T>(
            IEnumerable<T> items,
            Func<T, Guid> idSelector,
            Func<T, Guid?> parentIdSelector,
            Func<T, int> orderSelector,
            OrphanPolicy orphanPolicy = OrphanPolicy.TreatAsRoot)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (idSelector is null) throw new ArgumentNullException(nameof(idSelector));
            if (parentIdSelector is null) throw new ArgumentNullException(nameof(parentIdSelector));
            if (orderSelector is null) throw new ArgumentNullException(nameof(orderSelector));
                      
            var nodes = new Dictionary<Guid, BiTreeNode<T>>();
            foreach (var item in items)
            {
                var id = idSelector(item);
                if (nodes.ContainsKey(id))
                    throw new InvalidOperationException($"Duplicate ID detected: {id}");

                nodes[id] = new BiTreeNode<T>(
                    item,
                    id,
                    parentIdSelector(item),
                    orderSelector(item));
            }
         
            var roots = new List<BiTreeNode<T>>();

            foreach (var node in nodes.Values)
            {
                if (node.ParentId is null)
                {
                    roots.Add(node);
                    continue;
                }

                if (nodes.TryGetValue(node.ParentId.Value, out var parent))
                {
                    node.Parent = parent;
                    parent.AddChild(node);
                    continue;
                }

                switch (orphanPolicy)
                {
                    case OrphanPolicy.TreatAsRoot:
                        roots.Add(node);
                        break;
                    case OrphanPolicy.Skip:
                        // ничего
                        break;
                    case OrphanPolicy.Throw:
                        throw new InvalidOperationException(
                            $"Orphan node {node.Id}: parent {node.ParentId.Value} not found.");
                }
            }

            static int Compare(BiTreeNode<T> a, BiTreeNode<T> b)
            {
                var c = a.Order.CompareTo(b.Order);
                return c != 0 ? c : a.Id.CompareTo(b.Id);
            }

            roots.Sort(Compare);
            foreach (var r in roots)
                r.SortChildrenRecursively(Compare);

            return roots;
        }

        public static IEnumerable<BiTreeNode<T>> TraverseDepthFirst<T>(IEnumerable<BiTreeNode<T>> roots)
        {
            foreach (var r in roots)
                foreach (var n in r.DepthFirst())
                    yield return n;
        }
    }
}
