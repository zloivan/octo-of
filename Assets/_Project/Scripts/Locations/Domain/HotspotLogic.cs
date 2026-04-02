using System;
using System.Collections.Generic;
using OnlyFarms.Utilities;

namespace OnlyFarms.Locations.Domain
{
    public class HotspotLogic
    {
        public string[] GetActiveHotspotIds(HotspotData[] hotspots, HashSet<string> consumedIds, Func<ActivationCondition, string, bool> conditionEvaluator)
        {
            OFLogger.Log("Evaluating hotspots...");
            return null;
        }
    }
}