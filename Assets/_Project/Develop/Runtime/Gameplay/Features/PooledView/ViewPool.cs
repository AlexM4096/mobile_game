using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace _Project.Gameplay.Features.PooledView
{
    public sealed class ViewPool : IViewPool, IStartable, IDisposable
    {
        private enum InstanceStatus
        {
            Inactive,
            Pending,
            Active
        }

        private sealed class Pool
        {
            public Pool(PooledViewCatalog.Entry configuration)
            {
                Configuration = configuration;
                DefaultLocalPosition = configuration.Prefab.transform.localPosition;
                DefaultLocalRotation = configuration.Prefab.transform.localRotation;
                DefaultLocalScale = configuration.Prefab.transform.localScale;
            }

            public PooledViewCatalog.Entry Configuration { get; }
            public Queue<GameObject> Inactive { get; } = new();
            public Vector3 DefaultLocalPosition { get; }
            public Quaternion DefaultLocalRotation { get; }
            public Vector3 DefaultLocalScale { get; }
        }

        private sealed class Instance
        {
            public Instance(GameObject gameObject, Pool pool, IViewPoolCallbacks[] callbacks)
            {
                GameObject = gameObject;
                Pool = pool;
                Callbacks = callbacks;
                Status = InstanceStatus.Inactive;
            }

            public GameObject GameObject { get; }
            public Pool Pool { get; }
            public IViewPoolCallbacks[] Callbacks { get; }
            public InstanceStatus Status { get; set; }
        }

        private readonly PooledViewCatalog _catalog;
        private readonly Dictionary<int, Pool> _pools = new();
        private readonly Dictionary<int, Instance> _instances = new();
        private Transform _root;
        private bool _started;
        private bool _disposed;

        public ViewPool(PooledViewCatalog catalog)
        {
            _catalog = catalog;
            _catalog.ValidateOrThrow();

            foreach (var entry in _catalog.Entries)
            {
                _pools.Add(entry.PoolId, new Pool(entry));
            }
        }

        public void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _root = new GameObject("Pooled Views").transform;

            foreach (var pool in _pools.Values)
            {
                for (var index = 0; index < pool.Configuration.PreloadCount; index++)
                {
                    var instance = CreateInstance(pool);
                    pool.Inactive.Enqueue(instance.GameObject);
                }
            }
        }

        public bool TryGetReleaseDelay(int poolId, out float releaseDelay)
        {
            if (_pools.TryGetValue(poolId, out var pool))
            {
                releaseDelay = pool.Configuration.ReleaseDelay;
                return true;
            }

            releaseDelay = 0f;
            return false;
        }

        public GameObject Get(int poolId)
        {
            EnsureStarted();
            if (!_pools.TryGetValue(poolId, out var pool))
            {
                Debug.LogWarning($"Cannot get a pooled view: pool ID {poolId} is not registered.");
                return null;
            }

            Instance instance = null;
            while (pool.Inactive.Count > 0 && instance == null)
            {
                var candidate = pool.Inactive.Dequeue();
                if (candidate == null)
                {
                    continue;
                }

                _instances.TryGetValue(candidate.GetInstanceID(), out instance);
            }

            instance ??= CreateInstance(pool);
            ResetTransform(instance);
            instance.GameObject.SetActive(false);
            instance.Status = InstanceStatus.Pending;
            return instance.GameObject;
        }

        public bool ActivateIfPending(GameObject view)
        {
            if (view == null)
            {
                return false;
            }

            var instanceId = view.GetInstanceID();
            if (!_instances.TryGetValue(instanceId, out var instance) ||
                instance.Status != InstanceStatus.Pending)
            {
                return false;
            }

            InvokeCallbacks(instance, callback => callback.OnGet());
            if (view == null)
            {
                _instances.Remove(instanceId);
                Debug.LogWarning("A pooled view was destroyed by an OnGet callback.");
                return false;
            }

            view.SetActive(true);
            instance.Status = InstanceStatus.Active;
            return true;
        }

        public bool Release(GameObject view)
        {
            if (view == null)
            {
                Debug.LogWarning("Cannot release a null or destroyed pooled view.");
                return false;
            }

            var instanceId = view.GetInstanceID();
            if (!_instances.TryGetValue(instanceId, out var instance))
            {
                Debug.LogWarning($"Cannot release untracked view '{view.name}'.");
                return false;
            }

            if (instance.Status == InstanceStatus.Inactive)
            {
                Debug.LogWarning($"Cannot release view '{view.name}' twice.");
                return false;
            }

            if (instance.Status == InstanceStatus.Active)
            {
                InvokeCallbacks(instance, callback => callback.OnRelease());
            }

            view.SetActive(false);
            view.transform.SetParent(_root, false);

            if (instance.Pool.Inactive.Count >= instance.Pool.Configuration.MaxRetained)
            {
                _instances.Remove(instanceId);
                Object.Destroy(view);
                return true;
            }

            instance.Status = InstanceStatus.Inactive;
            instance.Pool.Inactive.Enqueue(view);
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }

            _instances.Clear();
            _pools.Clear();
        }

        private Instance CreateInstance(Pool pool)
        {
            var view = Object.Instantiate(pool.Configuration.Prefab, _root, false);
            view.name = $"{pool.Configuration.Prefab.name} (Pooled)";
            view.SetActive(false);

            var behaviours = view.GetComponentsInChildren<MonoBehaviour>(true);
            var callbacks = new List<IViewPoolCallbacks>(behaviours.Length);
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IViewPoolCallbacks callback)
                {
                    callbacks.Add(callback);
                }
            }

            var instance = new Instance(view, pool, callbacks.ToArray());
            _instances.Add(view.GetInstanceID(), instance);
            ResetTransform(instance);
            return instance;
        }

        private void ResetTransform(Instance instance)
        {
            var transform = instance.GameObject.transform;
            transform.SetParent(_root, false);
            transform.localPosition = instance.Pool.DefaultLocalPosition;
            transform.localRotation = instance.Pool.DefaultLocalRotation;
            transform.localScale = instance.Pool.DefaultLocalScale;
        }

        private static void InvokeCallbacks(Instance instance, Action<IViewPoolCallbacks> invocation)
        {
            foreach (var callback in instance.Callbacks)
            {
                try
                {
                    invocation(callback);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, instance.GameObject);
                }
            }
        }

        private void EnsureStarted()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ViewPool));
            }

            if (!_started)
            {
                Start();
            }
        }
    }
}
