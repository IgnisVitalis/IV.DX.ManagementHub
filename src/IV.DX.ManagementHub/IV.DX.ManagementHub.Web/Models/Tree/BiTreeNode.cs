namespace IV.DX.ManagementHub.Web.Models.Tree
{
    public sealed class BiTreeNode<T>
    {
        private readonly List<BiTreeNode<T>> _children = new();

        internal BiTreeNode(T item, Guid id, Guid? parentId, int order)
        {
            Item = item;
            Id = id;
            ParentId = parentId;
            Order = order;
        }

        public T Item { get; }
        public Guid Id { get; }
        public Guid? ParentId { get; }
        public int Order { get; }

        public BiTreeNode<T>? Parent { get; internal set; }
        public IReadOnlyList<BiTreeNode<T>> Children => _children;

        internal void AddChild(BiTreeNode<T> child) => _children.Add(child);

        public IEnumerable<BiTreeNode<T>> Ancestors()
        {
            for (var p = Parent; p != null; p = p.Parent)
                yield return p;
        }

        public IEnumerable<BiTreeNode<T>> DescendantsDepthFirst()
        {
            foreach (var c in _children)
            {
                yield return c;
                foreach (var d in c.DescendantsDepthFirst())
                    yield return d;
            }
        }

        public IEnumerable<BiTreeNode<T>> DepthFirst()
        {
            yield return this;
            foreach (var d in DescendantsDepthFirst())
                yield return d;
        }

        public IEnumerable<BiTreeNode<T>> BreadthFirst()
        {
            var q = new Queue<BiTreeNode<T>>();
            q.Enqueue(this);

            while (q.Count > 0)
            {
                var n = q.Dequeue();
                yield return n;

                foreach (var c in n._children)
                    q.Enqueue(c);
            }
        }

        internal void SortChildrenRecursively(Comparison<BiTreeNode<T>> comparison)
        {
            _children.Sort(comparison);
            foreach (var c in _children)
                c.SortChildrenRecursively(comparison);
        }
    }
}
