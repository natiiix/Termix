using System;

namespace Termix
{
    [Flags]
    public enum AssistantMode : ulong
    {
        None = 0,
        All = ~(ulong)0,
        Default = 1 << 0,
        Browser = 1 << 1,
        Messenger = 1 << 2
    }
}
