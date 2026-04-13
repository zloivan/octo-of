namespace OnlyFarms.Infrastructure.DataAccess
{
    public interface ILocationNarrativeSource
    {
        string GetOnEnterScript(string locationId);
        string GetOnEnterLabel(string locationId);
    }
    
    public class AlwaysNullNarrativeSource : ILocationNarrativeSource
    {
        public string GetOnEnterScript(string locationId) => null;

        public string GetOnEnterLabel(string locationId) =>
            null;
    }
    
    public class HardcodedNarrativeSource : ILocationNarrativeSource
    {
        private readonly string _locationId;
        private readonly string _script;
        private readonly string _label;

        public HardcodedNarrativeSource(string locationId, string script, string label = null)
        {
            _locationId = locationId;
            _script = script;
            _label = label;
        }

        public string GetOnEnterScript(string locationId)
        {
            return locationId == _locationId ? _script : null;
        }

        public string GetOnEnterLabel(string locationId)
        {
            return locationId == _locationId ? _label : null;
        }
    }
}