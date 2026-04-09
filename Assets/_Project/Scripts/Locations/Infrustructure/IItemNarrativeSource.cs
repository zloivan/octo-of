namespace OnlyFarms.Locations.Domain
{
    public interface IItemNarrativeSource
    {
        string GetOnUseScript(string itemId);
        string GetOnUseLabel(string itemId);
    }

    public class AlwaysNullItemNarrativeSource : IItemNarrativeSource
    {
        public string GetOnUseScript(string itemId) =>
            null;

        public string GetOnUseLabel(string itemId) =>
            null;
    }

    public class HardcodedItemNarrativeSource : IItemNarrativeSource
    {
        private readonly string _itemId;
        private readonly string _script;
        private readonly string _label;

        public HardcodedItemNarrativeSource(string itemId, string script, string label = null)
        {
            _itemId = itemId;
            _script = script;
            _label = label;
        }

        public HardcodedItemNarrativeSource()
        {
            _itemId = "hotspot3";
            _script = "Test_Continue";
            _label = "continue_here";
        }

        public string GetOnUseScript(string itemId)
        {
            return itemId == _itemId ? _script : null;
        }

        public string GetOnUseLabel(string itemId)
        {
            return itemId == _itemId ? _label : null;
        }
    }
}