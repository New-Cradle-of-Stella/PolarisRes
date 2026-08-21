using System;
using PixelLiner;

namespace Polaris.Res.Pxls
{
    /// <summary>
    /// 借用一个原版 PXLS 的只读句柄。
    ///
    /// 与 <see cref="PxlsCharacterHandle"/> 的关键区别是所有权：那个是 PolarisRes 自己加载、
    /// 自己释放的模组资源；这个只是把原版已经加载好的 <see cref="PxlCharacter"/> 投影出来。
    /// 释放本句柄只撤销 PolarisEvent 自己的引用，绝不调用 <c>disposeCharacter</c> 或
    /// <c>releaseMI</c>——原版 Bundle、PxlCharacter 和 MImage 的生命周期仍归游戏自己管。
    /// </summary>
    public sealed class GamePxlsLease
    {
        private bool released;

        public GamePxlsId Id { get; }

        /// <summary>解析用的 <c>PxlsLoader</c> title；诊断时能看出到底试的是哪个键。</summary>
        public string Title { get; internal set; }

        /// <summary>原版资源尚未加载时为 <c>null</c>。</summary>
        public PxlCharacter Character { get; private set; }

        /// <summary>原版资源尚未加载时为 <c>null</c>。</summary>
        public XX.MImage Image { get; private set; }

        public bool IsReady => !released && Character != null;

        /// <summary>借用已经撤销；此后 <see cref="IsReady"/> 恒为 false。</summary>
        public bool IsReleased => released;

        internal GamePxlsLease(GamePxlsId id) => Id = id;

        internal void Bind(PxlCharacter character, XX.MImage image)
        {
            if (released)
                return;

            Character = character;
            Image = image;
        }

        /// <summary>永远走当前 <see cref="Character"/>——不要跨帧缓存返回值。</summary>
        public PxlPose GetPose(string name) => IsReady ? Character.getPoseByName(name) : null;

        /// <summary>原版帧名直接就是全局键，不像模组资源那样需要加前缀。</summary>
        public PxlFrame GetFrame(string frameName) => IsReady ? XX.MTRX.getPF(frameName) : null;

        /// <summary>
        /// 撤销借用。幂等；只把本句柄对原版对象的引用置空，不动原版自己的引用计数。
        /// </summary>
        public void Release()
        {
            if (released)
                return;

            released = true;
            Character = null;
            Image = null;
            GamePxlsBridge.Forget(this);
        }

        public override string ToString()
        {
            if (released)
                return $"{Id} (released)";

            return IsReady ? $"{Id} (ready)" : $"{Id} (loading)";
        }
    }
}
