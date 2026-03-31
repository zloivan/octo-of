using Naninovel;

namespace Locations.Commands
{
    [CommandAlias("enterLocation")]
    public class EnterLocationCommand : Command
    {
        [RequiredParameter]
        public StringParameter LocationId;

        public override UniTask Execute(AsyncToken token = default)
        {
            Engine.GetService<LocationService>()?.Enter(LocationId.Value);

            return UniTask.CompletedTask;
        }
    }
}