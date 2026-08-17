using System;
using System.Collections.Generic;
using System.Reflection;
using PixelLiner;
using Polaris.Res.Pxls;

namespace Polaris.Res.Runtime
{
    /// <summary>
    /// 回填 <see cref="PolarisGameResourceAttribute"/> 字段。
    ///
    /// 与 <see cref="AutoBindScanner"/> 的模组资源分支并行、互不干扰：借用原版资源不需要挂载目录，
    /// 因此这里既不看 <see cref="PolarisResourceFolderAttribute"/>，也不建 <c>ModResources</c>。
    ///
    /// 借用句柄一定能拿到；原版资源当时是否已经加载完由句柄自己的 <c>IsReady</c> 反映。
    /// 字段类型是 <c>PxlCharacter</c>/<c>MImage</c> 这类"取一次就定死"的形式时，
    /// 绑定时还没就绪就会留空，并记一条警告——那是使用方式选错了，不是加载失败。
    /// </summary>
    internal static class GameResourceBinder
    {
        /// <summary>本次绑定产生的全部借用句柄，供插件卸载时统一撤销。</summary>
        private static readonly List<GamePxlsLease> Bound = new List<GamePxlsLease>();

        internal static int BindAll()
        {
            int total = 0;

            foreach (Assembly assembly in PolarisAPI.Modules.PluginAssemblies)
            {
                try
                {
                    total += BindAssembly(assembly);
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"[PolarisRes] Failed to bind game resources in {assembly.GetName().Name}: {ex}");
                }
            }

            if (total > 0)
                Plugin.Logger.LogMessage($"[PolarisRes] Borrowed {total} vanilla game resources.");

            return total;
        }

        private static int BindAssembly(Assembly assembly)
        {
            int bound = 0;

            foreach (Type type in PolarisAPI.Types.Of(assembly))
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    PolarisGameResourceAttribute attr = field.GetCustomAttribute<PolarisGameResourceAttribute>();
                    if (attr == null)
                        continue;

                    if (field.IsInitOnly || field.IsLiteral)
                    {
                        Plugin.Logger.LogWarning(
                            $"[PolarisRes] {type.FullName}.{field.Name} is readonly/const and cannot be back-filled; skipped.");
                        continue;
                    }

                    if (!GamePxlsId.TryParse(attr.LogicalPath, out GamePxlsId id))
                    {
                        Plugin.Logger.LogError(
                            $"[PolarisRes] [PolarisGameResource(\"{attr.LogicalPath}\")] on {type.FullName}.{field.Name} " +
                            "is not a valid logical path (absolute paths, drive letters and \"..\" are rejected).");
                        continue;
                    }

                    try
                    {
                        if (BindField(field, id))
                            bound++;
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogError($"[PolarisRes] Failed to bind {type.FullName}.{field.Name}: {ex.Message}");
                    }
                }
            }

            return bound;
        }

        private static bool BindField(FieldInfo field, GamePxlsId id)
        {
            GamePxlsLease lease = GamePxlsBridge.Borrow(id);

            lock (Bound)
                Bound.Add(lease);

            Type fieldType = field.FieldType;

            if (fieldType == typeof(GamePxlsLease))
            {
                field.SetValue(null, lease);
                return true;
            }

            if (fieldType == typeof(PxlCharacter))
            {
                WarnIfNotReady(field, lease, nameof(PxlCharacter));
                field.SetValue(null, lease.Character);
                return true;
            }

            if (fieldType == typeof(XX.MImage))
            {
                WarnIfNotReady(field, lease, "MImage");
                field.SetValue(null, lease.Image);
                return true;
            }

            lease.Release();
            Plugin.Logger.LogError(
                $"[PolarisRes] {field.DeclaringType?.FullName}.{field.Name} has type {fieldType.Name}; " +
                $"[PolarisGameResource] supports {nameof(GamePxlsLease)}, {nameof(PxlCharacter)} and MImage.");
            return false;
        }

        private static void WarnIfNotReady(FieldInfo field, GamePxlsLease lease, string typeName)
        {
            if (lease.IsReady)
                return;

            Plugin.Logger.LogWarning(
                $"[PolarisRes] {field.DeclaringType?.FullName}.{field.Name} is a {typeName} but " +
                $"\"{lease.Id}\" is not loaded by the game yet, so the field stays null. " +
                $"Use {nameof(GamePxlsLease)} if the resource may load later.");
        }

        /// <summary>撤销全部借用并清空字段。插件卸载时调用。</summary>
        internal static void ReleaseAll()
        {
            List<GamePxlsLease> snapshot;
            lock (Bound)
            {
                snapshot = new List<GamePxlsLease>(Bound);
                Bound.Clear();
            }

            foreach (GamePxlsLease lease in snapshot)
                lease.Release();
        }
    }
}
