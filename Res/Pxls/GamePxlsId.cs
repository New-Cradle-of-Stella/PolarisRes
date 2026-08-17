using System;

namespace Polaris.Res.Pxls
{
    /// <summary>
    /// 一个原版 PXLS 的逻辑标识：Bundle 逻辑路径 + PXLS 名，例如 <c>EvImg/__ev_n.pxls</c>。
    ///
    /// 刻意只接受逻辑路径，不接受任意磁盘绝对路径——借用原版资源是一个受控入口，
    /// 不是让模组读任意文件的口子。
    /// </summary>
    public readonly struct GamePxlsId : IEquatable<GamePxlsId>
    {
        /// <summary>归一化后的逻辑路径，分隔符统一为 <c>/</c>，不含扩展名。</summary>
        public string LogicalPath { get; }

        private GamePxlsId(string logicalPath) => LogicalPath = logicalPath;

        /// <summary>Bundle 目录部分；没有目录时为空串。</summary>
        public string Bundle
        {
            get
            {
                if (string.IsNullOrEmpty(LogicalPath))
                    return "";
                int slash = LogicalPath.LastIndexOf('/');
                return slash < 0 ? "" : LogicalPath.Substring(0, slash);
            }
        }

        /// <summary>PXLS 名（最后一段，不含扩展名）。</summary>
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(LogicalPath))
                    return "";
                int slash = LogicalPath.LastIndexOf('/');
                return slash < 0 ? LogicalPath : LogicalPath.Substring(slash + 1);
            }
        }

        public bool IsEmpty => string.IsNullOrEmpty(LogicalPath);

        /// <summary>
        /// 解析一个逻辑路径。拒绝绝对路径、盘符、空段和 <c>..</c>；末尾的 <c>.pxls</c> 会被去掉。
        /// </summary>
        public static bool TryParse(string value, out GamePxlsId id)
        {
            id = default;

            if (string.IsNullOrEmpty(value))
                return false;

            string path = value.Replace('\\', '/').Trim();

            if (path.Length == 0 || path[0] == '/' || path.IndexOf(':') >= 0)
                return false;

            if (path.EndsWith(".pxls", StringComparison.OrdinalIgnoreCase))
                path = path.Substring(0, path.Length - 5);

            if (path.Length == 0 || path[path.Length - 1] == '/')
                return false;

            foreach (string segment in path.Split('/'))
            {
                if (segment.Length == 0 || segment == "." || segment == "..")
                    return false;
            }

            id = new GamePxlsId(path);
            return true;
        }

        public static GamePxlsId Parse(string value) =>
            TryParse(value, out GamePxlsId id) ? id : throw new ArgumentException($"`{value}` is not a valid game PXLS logical path.", nameof(value));

        public bool Equals(GamePxlsId other) => string.Equals(LogicalPath, other.LogicalPath, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is GamePxlsId other && Equals(other);

        public override int GetHashCode() => LogicalPath?.GetHashCode() ?? 0;

        public override string ToString() => LogicalPath ?? "";
    }
}
