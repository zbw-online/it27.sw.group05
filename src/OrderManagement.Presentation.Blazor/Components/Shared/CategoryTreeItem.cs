namespace OrderManagement.Presentation.Blazor.Components.Shared
{
    public sealed class CategoryTreeItem(int id, string name, string path)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
        public string Path { get; } = path;
        public List<CategoryTreeItem> Children { get; } = [];
    }
}
