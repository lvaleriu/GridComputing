namespace GridComputingSharedLib
{
    public class InitializationStatus
    {
        public static int NotInitialized = 0;
        public static int Initializing = 1;
        public static int Initialized = 2;
        public static int InitializationFailed = 3;
        public static int InitializedButNoWork = 4;
    }
}