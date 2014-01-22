namespace GridComputingSharedLib
{
    /// <summary>
    /// NOTE : Could change this to protected/private since it is used only by the BaseDistribMasterTask class
    /// </summary>
    public enum JobState
    {
        Waiting,
        InTreatment,
        Done
    }
}