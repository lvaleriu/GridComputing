using System;

namespace UnitTests
{
    public class RemoteObject : MarshalByRefObject
    {
        private int _callCount;

        public int GetCount()
        {
            Console.WriteLine("GetCount has been called.");
            _callCount++;
            return (_callCount);
        }
    }
}