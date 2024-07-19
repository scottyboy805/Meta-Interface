using System;

namespace UltimateReplay.Storage
{
    public interface IReplayTokenSerializeProvider
    {
        // Properties
        IReplayTokenSerialize SerializeTarget { get; set; }
    }
}
