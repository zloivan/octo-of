using System.Collections.Generic;

namespace OnlyFarms.Infrastructure
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

        public string GetOnUseScript(string itemId) =>
            itemId == _itemId ? _script : null;

        public string GetOnUseLabel(string itemId) =>
            itemId == _itemId ? _label : null;
    }

    public class HardcodedItemNarrativeSourceLivingRoom : IItemNarrativeSource
    {
        private readonly Dictionary<string, (string, string)> _map = new()
        {
            { "living_room_secret_2", ("Test_Continue", "secret_2") },
            { "living_room_secret_3", ("Test_Continue", "secret_3") },
            { "living_room_secret_4", ("Test_Continue", "secret_4") },
        };

        public string GetOnUseScript(string itemId) =>
            _map.TryGetValue(itemId, out var result) ? result.Item1 : null;

        public string GetOnUseLabel(string itemId) =>
            _map.TryGetValue(itemId, out var result) ? result.Item2 : null;
    }
}