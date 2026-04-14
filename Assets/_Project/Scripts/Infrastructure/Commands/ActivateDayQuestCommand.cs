using Naninovel;
using OnlyFarms.Infrastructure.Services;

namespace OnlyFarms.Infrastructure.Commands
{
    [CommandAlias("activateDayQuests")]
    public class ActivateDayQuestCommand : Command
    {
        [RequiredParameter]
        [ParameterAlias(NamelessParameterAlias)]
        public StringParameter Day;

        public override UniTask Execute(AsyncToken token = default)
        {
            Engine.GetService<DaySessionOrchestratorService>()?.StartDay(Day.Value);

            return UniTask.CompletedTask;
        }
    }
}