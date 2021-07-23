using System;

namespace SubSolution.Utils
{
    public class Disposable : IDisposable
    {
        private readonly Action _disposeAction;

        public Disposable(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose() => _disposeAction.Invoke();
    }
}