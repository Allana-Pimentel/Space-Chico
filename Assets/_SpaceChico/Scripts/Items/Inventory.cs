using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceChico
{
    public enum ItemCategory { ShipPart, Power, Consumable }
    public enum PowerType { None, Shield, DoubleJump }

    [CreateAssetMenu(menuName = "Space Chico/Item", fileName = "NewItem")]
    public class ItemData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public Sprite icon;
        public ItemCategory category;
        public PowerType power = PowerType.None;
        public float powerDuration = 6f;
        public Color accent = new Color(0.4f, 1f, 0.9f);
        public string CategoryLabel => category.ToString();
    }

    [Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int count;
    }

    // Mantido apenas para compatibilidade com a cena e prefabs
    public class Inventory : MonoBehaviour
    {
        public int capacity = 8;
        public List<InventorySlot> slots = new List<InventorySlot>();
        public ItemData equippedPower;
        public event Action Changed;

        public bool Add(ItemData item, int amount = 1)
        {
            Changed?.Invoke();
            return true;
        }
    }
}
