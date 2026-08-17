namespace Polaris.Res.Runtime
{
    /// <summary>资源子系统的初始化编排入口，由 <c>Polaris.Plugin.Start()</c> 调用（此时全部插件已 Awake）。</summary>
    internal static class ResRuntime
    {
        private static bool initialized;

        internal static void Init()
        {
            if (initialized)
            {
                Plugin.Logger.LogWarning("[PolarisRes] ResRuntime.Init was called more than once; ignored.");
                return;
            }

            initialized = true;
            ResHost.EnsureCreated();
            AutoBindScanner.ScanAll();

            // 借用原版资源不涉及挂载目录，所以走独立一遍扫描；放在模组资源之后，
            // 这样两类字段的绑定顺序和它们的日志顺序一致，排查时不会互相错位。
            GameResourceBinder.BindAll();

            Plugin.Logger.LogInfo("[PolarisRes] Resource runtime initialized.");
        }
    }
}
