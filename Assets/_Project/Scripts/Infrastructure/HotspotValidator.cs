using System;
using OnlyFarms.Infrastructure;

namespace OnlyFarms.Domain.Hotspots
{
    public class HotspotValidator : IHotspotValidator
    {
        private readonly IQuestStatusSource _questService;

        public HotspotValidator(IQuestStatusSource questService) =>
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