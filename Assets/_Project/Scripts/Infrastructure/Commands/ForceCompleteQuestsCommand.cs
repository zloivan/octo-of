using Naninovel;
using OnlyFarms.Infrastructure.Services;

namespace OnlyFarms.Infrastructure.Commands
{
    [CommandAlias("forceCompleteQuests")]
    public class ForceCompleteQuestsCommand : Command
    {
        public override UniTask Execute(AsyncToken asyncToken = default)
        {
            Engine.GetService<QuestService>()?.ForceComplete();
            
            return UniTask.CompletedTask;
        }
    }
}