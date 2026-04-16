using OnlyFarms.Domain.Quests;

namespace OnlyFarms.DataAccess
{
    public interface IQuestRepository
    {
        QuestDefinition[] GetQuestsOfDay(string dayId);
    }
}