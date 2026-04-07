using System;
using Naninovel;
using Naninovel.Commands;
using OnlyFarms.Locations.Domain;

namespace OnlyFarms.Locations.Commands
{
    [CommandAlias("exitNarrative")]
    public class ExitNarativeCommand : Command
    {
        [ParameterAlias(NamelessParameterAlias)]
        public StringParameter Id;

        [ParameterAlias("returnScript"), RequiredParameter]
        public StringParameter ReturnScript;

        [ParameterAlias("returnLabel")]
        public StringParameter ReturnLabel;

        public override async UniTask Execute(AsyncToken token = default)
        {
            var locationService = Engine.GetService<LocationService>();
            if (locationService == null)
                throw new NullReferenceException("Location service not found");

            var questService = Engine.GetService<QuestService>();
            if (questService == null)
                throw new NullReferenceException("Quest service not found");

            var scriptPlayer = Engine.GetService<IScriptPlayer>();
            if (scriptPlayer == null)
                throw new NullReferenceException("Script player service not found");

            await new HideAllActors().Execute(token);

            var defaultLocationId = string.IsNullOrEmpty(locationService.GetCurrentLocationId())
                ? locationService.GetStartingLocation().Id
                : locationService.GetCurrentLocationId();
            
            var locationId = Assigned(Id) ? Id.Value : defaultLocationId;
            await locationService.Enter(locationId, token);
            locationService.SetFreeRoamMode(true);

            var returnLabel = Assigned(ReturnLabel) ? ReturnLabel.Value : null;
            questService.SetReturnPoint(ReturnScript.Value, returnLabel);

            scriptPlayer.Stop();
        }
    }
}