using System;
using UnityEngine;

[Serializable]
public class ItemContext
{
    [field: SerializeField] public ItemData ItemData { get; private set; }
    [field: SerializeField] public int Amount { get; set; }
}