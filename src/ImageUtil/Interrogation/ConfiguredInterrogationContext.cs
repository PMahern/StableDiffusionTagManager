namespace ImageUtil.Interrogation
{
    public class ConfiguredInterrogationContext<TResult> : IDisposable
    {
        bool isDisposed = false;
        private readonly IDisposable wrapped;

        public Func<Action<string>, Action<string>, Task> InitializeOperation { get; }
        public Func<byte[], Action<string>, Action<string>, Task<TResult>> InterrogateOperation { get; }

        public ConfiguredInterrogationContext(IDisposable wrapped, 
                                                Func<Action<string>, Action<string>, Task> initializeOperation, 
                                                Func<byte[], Action<string>, Action<string>, Task<TResult>> interrogateOperation) 
        {
            this.wrapped = wrapped;
            InitializeOperation = initializeOperation;
            InterrogateOperation = interrogateOperation;
        }

        public void Dispose()
        {
            if(isDisposed)
            {
                throw new ObjectDisposedException("Attempted to dispose of an already disposed interrogation context.");
            }
            isDisposed = true;
            wrapped.Dispose();
        }
    }
}
