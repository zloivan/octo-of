using System;
using System.Collections.Generic;
using System.Linq;

namespace OnlyFarms.Domain.Hotspots
{
    public class HotspotLogic
    {
        private HashSet<string> _consumedHotspotIds = new();
        private readonly HotspotData[] _allHotspots;
        private readonly IHotspotValidator _validator;

        public HotspotLogic(IHotspotRepository hotspotRepository, IHotspotValidator validator)
        {
            _validator = validator;
            _allHotspots = hotspotRepository.GetAllHotspots();
        }

        public bool TryConsume(string id)
        {
            var hotspot = Array.Find(_allHotspots, h => h.Id == id);
            if (hotspot is not { Type: HotspotType.Item }) 
                return false;
            
            _consumedHotspotIds.Add(id);
            return true;
        }
        
        public HotspotData GetHotspotData(string id) =>
            Array.Find(_allHotspots, h => h.Id == id);

        public bool IsConsumed(string hotspotId) =>
            _consumedHotspotIds.Contains(hotspotId);

        public string[] GetConsumedHotspotIds() =>
            _consumedHotspotIds.ToArray();

        public HotspotData[] GetAvailableHotspots(string locationId)
        {
            var available = _allHotspots
                .Where(h => h.LocationId == locationId
                            && !IsConsumed(h.Id)
                            && _validator.IsAvailable(h))
                .ToArray();

            return available;
        }

        public void LoadSnapshot(HotspotLogicSnapshot snapshot) =>
            _consumedHotspotIds = new HashSet<string>(snapshot.ConsumedItemIds);

        public HotspotLogicSnapshot GetSnapshot() =>
            new(GetConsumedHotspotIds());

        public void Reset() =>
            _consumedHotspotIds.Clear();
    }

    public readonly struct HotspotLogicSnapshot
    {
        public readonly string[] ConsumedItemIds;

        public HotspotLogicSnapshot(string[] consumedItemIds) =>
            ConsumedItemIds = consumedItemIds;
    }
}