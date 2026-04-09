namespace IV.DX.ManagementHub.Web.Models.Tree
{
    public static class BiTreeUtils
    {      
        public static IReadOnlyList<BiTreeNode<T>> PathFromRoot<T>(this BiTreeNode<T> node)
        {
            if (node is null) throw new ArgumentNullException(nameof(node));

            var stack = new Stack<BiTreeNode<T>>();
            for (var n = node; n != null; n = n.Parent!)
                stack.Push(n);

            return stack.ToArray();
        }

        public static IReadOnlyList<BiTreeNode<T>> PathToRoot<T>(this BiTreeNode<T> node)
        {
            if (node is null) throw new ArgumentNullException(nameof(node));

            var list = new List<BiTreeNode<T>>();
            for (var n = node; n != null; n = n.Parent!)
                list.Add(n);

            return list;
        }
              
        public static IReadOnlyList<T> ItemPathFromRoot<T>(this BiTreeNode<T> node)
            => node.PathFromRoot().Select(x => x.Item).ToArray();
              
        public static BiTreeNode<T> Root<T>(this BiTreeNode<T> node)
        {
            if (node is null) throw new ArgumentNullException(nameof(node));

            var n = node;
            while (n.Parent != null) n = n.Parent;
            return n;
        }

        public static IReadOnlyDictionary<Guid, BiTreeNode<T>> BuildIndexById<T>(this IEnumerable<BiTreeNode<T>> roots)
        {
            if (roots is null) throw new ArgumentNullException(nameof(roots));

            var dict = new Dictionary<Guid, BiTreeNode<T>>();

            foreach (var root in roots)
            {
                foreach (var node in root.DepthFirst())
                {
                    if (!dict.TryAdd(node.Id, node))
                        throw new InvalidOperationException($"Duplicate node ID in forest: {node.Id}");
                }
            }

            return dict;
        }
        public static BiTreeNode<T> GetById<T>(this IReadOnlyDictionary<Guid, BiTreeNode<T>> index, Guid id)
            => index.TryGetValue(id, out var n) ? n : throw new KeyNotFoundException($"Node not found: {id}");

        public static bool TryGetById<T>(this IReadOnlyDictionary<Guid, BiTreeNode<T>> index, Guid id, out BiTreeNode<T>? node)
            => index.TryGetValue(id, out node);

        public static BiTreeNode<T>? FindById<T>(this IEnumerable<BiTreeNode<T>> roots, Guid id)
        {
            if (roots is null) throw new ArgumentNullException(nameof(roots));

            foreach (var r in roots)
                foreach (var n in r.DepthFirst())
                    if (n.Id == id) return n;

            return null;
        }

        public static IEnumerable<BiTreeNode<T>> Leaves<T>(this IEnumerable<BiTreeNode<T>> roots)
        {
            if (roots is null) throw new ArgumentNullException(nameof(roots));

            foreach (var r in roots)
                foreach (var n in r.DepthFirst())
                    if (n.Children.Count == 0)
                        yield return n;
        }
    }
}