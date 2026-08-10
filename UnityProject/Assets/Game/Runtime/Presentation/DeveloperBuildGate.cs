namespace OCC.Combat.Presentation
{
    public static class DeveloperBuildGate
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool IsEnabled
        {
            get
            {
#if OCC_DEVELOPER_TOOLS
                return true;
#else
                return false;
#endif
            }
        }
#else
        public static bool IsEnabled => false;
#endif
    }
}
