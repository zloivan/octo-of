using Naninovel;
using OnlyFarms.Infrastructure.Services;

namespace OnlyFarms.Infrastructure.Commands
{
    [CommandAlias("enterLocation")]
    public class EnterLocationCommand : Command
    {
        [RequiredParameter]
        public StringParameter LocationId;

        public override UniTask Execute(AsyncToken token = default)
        {
            Engine.GetService<LocationService>()?.Enter(LocationId.Value, token);

            return UniTask.CompletedTask;
        }
    }
}