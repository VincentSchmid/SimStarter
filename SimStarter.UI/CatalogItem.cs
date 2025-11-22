namespace SimStarter.UI
{
    public sealed class CatalogItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public bool RunAsAdmin { get; set; }
        public bool WaitForExit { get; set; }
    }
}
