namespace OnlyFarms.Infrastructure
{
    public interface IFreeRoamSessionSource
    {
        string GetReturnScript();
        string GetReturnLabel();
    }

    public class HardcodedSessionsSource : IFreeRoamSessionSource
    {
        private readonly string _returnScript;
        private readonly string _returnLabel;

        public HardcodedSessionsSource(string returnScript, string returnLabel)
        {
            _returnScript = returnScript;
            _returnLabel = returnLabel;
        }

        public string GetReturnScript() =>
            _returnScript;

        public string GetReturnLabel() =>
            _returnLabel;
    }
}