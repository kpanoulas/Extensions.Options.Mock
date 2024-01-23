using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace Extensions.Options.Mock
{
    public sealed class OptionsMonitorMock<T> : IOptionsMonitor<T>
    {
        private T currentValue;
        private ConcurrentDictionary<string, OptionsMonitorListenerMock> listenerRegistry;
        private ConcurrentDictionary<string, T> optionsInstanceRegistry;

        public OptionsMonitorMock(T initialValue)
        {
            currentValue = initialValue;
            listenerRegistry = new ConcurrentDictionary<string, OptionsMonitorListenerMock>();
            optionsInstanceRegistry = new ConcurrentDictionary<string, T>();
        }


        public T CurrentValue
        {
            get { return currentValue; }

            set
            {
                currentValue = value;
                var key = Guid.NewGuid().ToString();
                optionsInstanceRegistry.AddOrUpdate(key, (v) => value, (nv, v) => v);
                foreach (var kvp in listenerRegistry)
                {
                    kvp.Value.Listener(currentValue, kvp.Key);
                }
            }
        }

        public T Get(string? name)
        {
            if (name is null)
            {
                return currentValue;
            }

            if (optionsInstanceRegistry.TryGetValue(name, out var value))
            {
                return value;
            }

            return currentValue;
        }

        public void Set(string? name, T value)
        {
            CurrentValue = value;
            if (!(name is null))
            {
                optionsInstanceRegistry.AddOrUpdate(name, (v) => value, (nv, v) => v);
            }
        }

        public IDisposable? OnChange(Action<T, string?> listener)
        {
            return new OptionsMonitorListenerMock(Guid.NewGuid().ToString(), listenerRegistry, listener);
        }


        private class OptionsMonitorListenerMock : IDisposable
        {
            private readonly string id;
            private readonly ConcurrentDictionary<string, OptionsMonitorListenerMock> registry;
            private readonly Action<T, string?> listener;

            public Action<T, string?> Listener => listener;

            public OptionsMonitorListenerMock(string id, ConcurrentDictionary<string, OptionsMonitorListenerMock> registry, Action<T, string?> listener)
            {
                this.id = id;
                this.registry = registry;
                this.listener = listener;
                registry.AddOrUpdate(id, (v) => this, (nv, v) => v);
            }

            public void Dispose()
            {
                registry.TryRemove(id, out _);
            }
        }
    }
}
