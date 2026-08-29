using System;
using UnityEngine;

[Serializable]
public struct ActivityModifierData
{
    [SerializeField] private string id;
    [SerializeField] private float value;

    public string Id => id;
    public float Value => value;
}
