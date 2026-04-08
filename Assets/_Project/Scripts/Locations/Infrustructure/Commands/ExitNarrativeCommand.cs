using System;
using JetBrains.Annotations;
using Naninovel;
using Naninovel.Commands;
using OnlyFarms.Locations.Domain;
using OnlyFarms.Locations.Services;

namespace OnlyFarms.Locations.Commands
{
    [CommandAlias("exitNarrative")]
    public class ExitNarrativeCommand : Command
    {
        [UsedImplicitly]
        [ParameterAlias(NamelessParameterAlias)]
        public StringParameter Id;

        [UsedImplicitly]
        [ParameterAlias("returnScript")]
        public StringParameter ReturnScript;

        [UsedImplicitly]
        [ParameterAlias("returnLabel")]
        public StringParameter ReturnLabel;

        public override async UniTask Execute(AsyncToken token = default)
        {
            var locationService = Engine.GetService<LocationService>();
            if (locationService == null)
                throw new NullReferenceException("Location service not found");

            var gameFlowService = Engine.GetService<GameFlowService>();
            if (gameFlowService == null)
                throw new NullReferenceException("Game flow service not found");

            var scriptPlayer = Engine.GetService<IScriptPlayer>();
            if (scriptPlayer == null)
                throw new NullReferenceException("Script player service not found");

            await new HideAllActors().Execute(token);

            var locationId = Assigned(Id)
                ? Id.Value
                : locationService.GetCurrentLocationId() ?? locationService.GetStartingLocation().Id;

            await locationService.Enter(locationId, token);
            locationService.SetFreeRoamMode(true).Forget();

            if (Assigned(ReturnScript))
                gameFlowService.SetSessionSource(new HardcodedSessionsSource(
                    ReturnScript.Value,
                    Assigned(ReturnLabel) ? ReturnLabel.Value : null));

            scriptPlayer.Stop();
        }
    }
}