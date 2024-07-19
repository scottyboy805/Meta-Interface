using System;
using System.Collections.Generic;

namespace UltimateReplay.Storage
{
    public interface IReplayTokenSerialize
    {
        // Methods
        IEnumerable<ReplayToken> GetSerializeTokens(bool includeOptional = false);
    }
}
