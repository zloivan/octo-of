using System;
using Naninovel;
using OnlyFarms.DataAccess;
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
            var questService = Engine.GetService<QuestService>();

            if (questService == null)
                throw new NullReferenceException("Quest service is null");

            var gameFlowService = Engine.GetService<GameFlowService>();

            if (gameFlowService == null)
                throw new NullReferenceException("Game flow service is null");

            var config = Engine.GetConfiguration<GameConfig>();

            if (config == null)
                throw new NullReferenceException("Game config is null");

            var script = config.QuestConfig.GetDayConfig(Day.Value).GetReturnScript();
            var label = config.QuestConfig.GetDayConfig(Day.Value).GetReturnLabel();
            questService.ActivateDaySession(Day.Value);
            gameFlowService.SetSessionSource(new DaySessionSource(script, label));
            
            return UniTask.CompletedTask;
        }
    }
}