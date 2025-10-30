using System;
using System.Threading;

namespace StructuralPlanner.Tests
{
    public static class StaTestHelper
    {
        /// <summary>
        /// Executes the given action on a STA thread. Any exception thrown inside the action
        /// will be propagated to the calling thread.
        /// </summary>
        public static void Run(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            Exception exception = null;

            Thread staThread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();

            if (exception != null)
                throw exception;
        }
    }
}
