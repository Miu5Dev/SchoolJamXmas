using System;
using System.Collections.Generic;

public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> handlers = new();
    
    public static void Subscribe<T>(Action<T> handler) where T : class
    {
        Type eventType = typeof(T);
        
        if (!handlers.ContainsKey(eventType))
            handlers[eventType] = new List<Delegate>();
        
        handlers[eventType].Add(handler);
    }
    
    public static void Unsubscribe<T>(Action<T> handler) where T : class
    {
        Type eventType = typeof(T);
        
        if (handlers.TryGetValue(eventType, out var list))
        {
            list.Remove(handler);
        }
    }
    
    public static void Raise<T>(T eventData) where T : class
    {
        Type eventType = typeof(T);
        
        if (handlers.TryGetValue(eventType, out var list))
        {
            foreach (var handler in list.ToArray()) // ToArray to avoid modification during iteration
                ((Action<T>)handler)?.Invoke(eventData);
        }
    }
}