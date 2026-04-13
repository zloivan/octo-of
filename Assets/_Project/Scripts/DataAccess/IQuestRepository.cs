namespace OnlyFarms.DataAccess
{
    public interface IQuestRepository
    {
        DayConfigSO GetDayConfig(string dayId);
    }
}