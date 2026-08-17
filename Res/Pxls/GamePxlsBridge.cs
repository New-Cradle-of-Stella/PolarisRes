using System;
using System.Collections.Generic;
using PixelLiner;

namespace Polaris.Res.Pxls
{
    /// <summary>
    /// 原版 PXLS 的只读借用桥。
    ///
    /// 原版自己通过 <c>MTI</c>/<c>MTRX</c>/<c>PxlsLoader</c> 链把 <c>EvImg/__ev_n.pxls</c> 之类的资源
    /// 加载进全局 title 字典；这里只按 title 查一下、把状态投影成统一句柄，既不重新加载、
    /// 不复制 atlas，也不把原版资源重新导出到模组目录。
    ///
    /// 本桥可以复用资源加载类，但不调用 <c>EV.readOneLine</c>、不创建 <c>EvReader</c>、
    /// 不提交任何 CMD 文本。
    /// </summary>
    public static class GamePxlsBridge
    {
        private static readonly Dictionary<GamePxlsId, List<GamePxlsLease>> Outstanding =
            new Dictionary<GamePxlsId, List<GamePxlsLease>>();

        /// <summary>
        /// 借用一个原版 PXLS。总是立刻返回句柄——原版资源可能还没加载完，调用方轮询
        /// <see cref="GamePxlsLease.IsReady"/>，或者交给 PolarisEvent 的资源等待去推进。
        /// </summary>
        public static GamePxlsLease Borrow(GamePxlsId id)
        {
            if (id.IsEmpty)
                throw new ArgumentException("Cannot borrow an empty game PXLS id.", nameof(id));

            var lease = new GamePxlsLease(id);

            lock (Outstanding)
            {
                if (!Outstanding.TryGetValue(id, out List<GamePxlsLease> leases))
                    Outstanding[id] = leases = new List<GamePxlsLease>();
                leases.Add(lease);
            }

            Refresh(lease);
            return lease;
        }

        /// <summary>逻辑路径重载；路径非法时抛 <see cref="ArgumentException"/>。</summary>
        public static GamePxlsLease Borrow(string logicalPath) => Borrow(GamePxlsId.Parse(logicalPath));

        /// <summary>
        /// 重新查一次原版字典。资源在借用之后才加载完是常态（事件先声明、原版随后才把 Bundle 拉进来），
        /// 所以每次轮询都要重查，不能只在借用那一刻看一眼。
        /// </summary>
        public static bool Refresh(GamePxlsLease lease)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));
            if (lease.IsReleased)
                return false;

            foreach (string title in CandidateTitles(lease.Id))
            {
                PxlCharacter character;
                try
                {
                    character = PxlsLoader.getPxlCharacter(title);
                }
                catch (Exception)
                {
                    // 原版字典在加载中途可能处于不一致状态；查失败当作"还没好"，下一帧再来。
                    continue;
                }

                if (character == null)
                    continue;

                XX.MImage image = null;
                try
                {
                    // no_make_mi: true —— 只取原版已经建好的 MImage，绝不替它新建一个。
                    image = XX.MTRX.getMI(character, no_make_mi: true);
                }
                catch (Exception)
                {
                    image = null;
                }

                lease.Title = title;
                lease.Bind(character, image);
                return true;
            }

            return false;
        }

        /// <summary>
        /// title 候选链。原版对不同资源族用的键不完全一致（有的是带目录的完整逻辑路径，
        /// 有的只用 PXLS 名），所以按"最具体优先"依次试，命中即止；
        /// <see cref="GamePxlsLease.Title"/> 会记下真正命中的那个，便于排查。
        /// </summary>
        public static IEnumerable<string> CandidateTitles(GamePxlsId id)
        {
            if (id.IsEmpty)
                yield break;

            yield return id.LogicalPath;

            string name = id.Name;
            if (!string.Equals(name, id.LogicalPath, StringComparison.Ordinal))
                yield return name;
        }

        /// <summary>当前还没释放的借用数量，用于诊断"事件结束后有没有漏放"。</summary>
        public static int OutstandingCount(GamePxlsId id)
        {
            lock (Outstanding)
            {
                return Outstanding.TryGetValue(id, out List<GamePxlsLease> leases) ? leases.Count : 0;
            }
        }

        internal static void Forget(GamePxlsLease lease)
        {
            lock (Outstanding)
            {
                if (!Outstanding.TryGetValue(lease.Id, out List<GamePxlsLease> leases))
                    return;

                leases.Remove(lease);
                if (leases.Count == 0)
                    Outstanding.Remove(lease.Id);
            }
        }

        /// <summary>
        /// 撤销全部借用。插件卸载时调用——同样只丢引用，不碰原版对象的生命周期。
        /// </summary>
        public static int ReleaseAll()
        {
            List<GamePxlsLease> all = new List<GamePxlsLease>();

            lock (Outstanding)
            {
                foreach (List<GamePxlsLease> leases in Outstanding.Values)
                    all.AddRange(leases);
                Outstanding.Clear();
            }

            foreach (GamePxlsLease lease in all)
                lease.Release();

            return all.Count;
        }
    }
}
