using System;

namespace OnlyFarms.Locations.Domain
{
    public interface IHotspotValidator
    {
        bool IsAvailable(HotspotData hotspotData);
    }
    
    public class AlwaysAvailableHotspotValidator : IHotspotValidator
    {
        public bool IsAvailable(HotspotData hotspotData) => true;
    }

    public class HotspotValidator : IHotspotValidator
    {
        private readonly QuestService _questService;

        public HotspotValidator(QuestService questService) =>
            _questService = questService;

        public bool IsAvailable(HotspotData hotspotData)
        {
            switch (hotspotData.Condition)
            {
                case ActivationCondition.Always:
                     return true;
                case ActivationCondition.RequiresQuestId:
                    return _questService?.IsQuestCompleted(hotspotData.ConditionValue) ?? true;
                case ActivationCondition.RequiresFlag:
                        return false; // TODO: custom flag system
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}