using Naninovel;
using OnlyFarms.Infrastructure.Services;

namespace OnlyFarms.Infrastructure.Commands
{
    [CommandAlias("reportQuestEvent")]
    public class ReportQuestEventCommand : Command
    {
        [RequiredParameter]
        public StringParameter Id;
        
        public override UniTask Execute(AsyncToken token = default)
        {
            
            Engine.GetService<QuestService>()?.ReportEvent(Id);
            return UniTask.CompletedTask;
        }
    }
}