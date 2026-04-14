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
                    return _questService?.IsQuestCompleted(hotspotData.QuestDefinition.GetDisplayName()) ?? true;
                case ActivationCondition.RequiresFlag:
                    return false;
                default:
                    return true;
            }
        }
    }
}