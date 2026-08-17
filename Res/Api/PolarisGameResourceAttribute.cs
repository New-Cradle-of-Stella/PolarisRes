using System;

namespace Polaris.Res
{
    /// <summary>
    /// 标在 static 字段上，声明"把原版已经加载的资源借用到这个字段"。
    ///
    /// 与 <see cref="PolarisResourceAttribute"/> 的区别只有资源来源：那个从模组自己的资源目录
    /// 加载文件，PolarisRes 拥有并负责释放；这个只借用原版通过 <c>MTI</c>/<c>MTRX</c>/<c>PxlsLoader</c>
    /// 链加载好的对象，PolarisRes 不拥有、不释放、也不重新导出。
    ///
    /// 因此它<b>不需要</b> <see cref="PolarisResourceFolderAttribute"/>：借用不涉及任何挂载目录。
    ///
    /// 字段类型决定拿到什么：
    /// <list type="bullet">
    /// <item><see cref="Pxls.GamePxlsLease"/>：完整借用句柄，可以查 <c>IsReady</c>、取 pose/frame、显式 <c>Release()</c>。</item>
    /// <item><c>PixelLiner.PxlCharacter</c>：直接拿原版角色对象；资源还没加载完时为 <c>null</c>。</item>
    /// <item><c>XX.MImage</c>：直接拿原版图像；资源还没加载完时为 <c>null</c>。</item>
    /// </list>
    ///
    /// 后两种是"取一次就定死"的便捷形式：绑定时原版还没加载完就会一直是 <c>null</c>。
    /// 事件演出这类需要等待的场景应该用 <see cref="Pxls.GamePxlsLease"/>。
    /// </summary>
    /// <example>
    /// <code>
    /// static class VanillaPortraits
    /// {
    ///     // 借用原版诺艾尔的事件立绘；不需要 [PolarisResourceFolder]。
    ///     [PolarisGameResource("EvImg/__ev_n.pxls")]
    ///     public static Polaris.Res.Pxls.GamePxlsLease NoelPortrait;
    ///
    ///     [PolarisGameResource("MapChars/sub_a.pxls")]
    ///     public static PixelLiner.PxlCharacter AliceMapChar;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class PolarisGameResourceAttribute : Attribute
    {
        public PolarisGameResourceAttribute(string logicalPath)
        {
            LogicalPath = logicalPath;
        }

        /// <summary>
        /// 原版 Bundle 逻辑路径 + PXLS 名，例如 <c>EvImg/__ev_n.pxls</c>。
        /// 扩展名可省略；不接受绝对路径、盘符和 <c>..</c>。
        /// </summary>
        public string LogicalPath { get; }
    }
}
