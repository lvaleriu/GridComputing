using System;

namespace GridComputing
{
    /// <summary>
    /// An exception thrown by the Grid module.
    /// </summary>
    public class GridComputingException : ApplicationException
    {
        public GridComputingException(string message)
            : base(message)
        {
        }

        public GridComputingException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}