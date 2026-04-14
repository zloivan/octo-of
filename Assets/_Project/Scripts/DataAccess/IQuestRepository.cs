using OnlyFarms.Domain;

namespace OnlyFarms.DataAccess
{
    public interface IQuestRepository
    {
        QuestDefinition[] GetQuestsOfDay(string dayId);
    }
}