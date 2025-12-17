using System;

namespace OnlineOrderFulfillmentOptimizer.Exceptions
{
    public class ConcurrentValidationException : Exception
    {
        public IReadOnlyList<Exception> InnerExceptions { get; private set; }

        public ConcurrentValidationException(string v, IReadOnlyList<Exception> innerExceptions)
        : base($"Multiple validation errors occurred: {innerExceptions.Count} exceptions.")
        {
            InnerExceptions = innerExceptions;
        }
    }
}