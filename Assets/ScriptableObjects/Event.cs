using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Event", menuName = "Scriptable Objects/Event")]
public abstract class Event<T> : ScriptableObject
{
    public event Action<T> OnEventRaised;

    public void Raise(T payload)
    {
        OnEventRaised?.Invoke(payload);
    }
}