using System;

#if !ULTIMATEREPLAY_DISABLE_UNITYPOOL
using UnityEngine.Pool;
#endif

namespace UltimateReplay.Lifecycle
{
#if ULTIMATEREPLAY_DISABLE_UNITYPOOL
    internal sealed class ReplayObjectPool
#else
    internal sealed class ReplayObjectPool : IObjectPool<ReplayObject>
#endif
    {
#if !ULTIMATEREPLAY_DISABLE_UNITYPOOL
        // Private
        private ObjectPool<ReplayObject> pool = null;

        // Properties
        int IObjectPool<ReplayObject>.CountInactive
        {
            get { return pool.CountInactive; }
        }
#else
        private Func<ReplayObject> createdPooled;
        private Action<ReplayObject> destroyPooled;
#endif

        // Constructor
        public ReplayObjectPool(Func<ReplayObject> createPooled, Action<ReplayObject> destroyPooled)
        {
#if ULTIMATEREPLAY_DISABLE_UNITYPOOL
            this.createdPooled = createPooled;
            this.destroyPooled = destroyPooled;
#else
            this.pool = new ObjectPool<ReplayObject>(createPooled, OnTakeFromPool, OnReturnToPool, destroyPooled);
#endif
        }

        // Methods
#if !ULTIMATEREPLAY_DISABLE_UNITYPOOL
        private void OnReturnToPool(ReplayObject instance)
        {
            instance.gameObject.SetActive(false);
        }

        private void OnTakeFromPool(ReplayObject instance)
        {
            instance.gameObject.SetActive(true);
        }
#endif

        public void Clear()
        {
#if !ULTIMATEREPLAY_DISABLE_UNITYPOOL
            pool.Clear();
#endif
        }

        public ReplayObject Get()
        {
#if ULTIMATEREPLAY_DISABLE_UNITYPOOL
            return createdPooled();
#else
            return pool.Get();
#endif
        }

#if !ULTIMATEREPLAY_DISABLE_UNITYPOOL
        PooledObject<ReplayObject> IObjectPool<ReplayObject>.Get(out ReplayObject v)
        {
            return pool.Get(out v);
        }
#endif

        public void Release(ReplayObject element)
        {
#if ULTIMATEREPLAY_DISABLE_UNITYPOOL
            destroyPooled(element);
#else
            pool.Release(element);
#endif
        }
    }
}
