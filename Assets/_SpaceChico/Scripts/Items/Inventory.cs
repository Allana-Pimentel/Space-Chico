using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceChico
{
    public enum ItemCategory { ShipPart, Power, Consumable }
    public enum PowerType { None, Shield, DoubleJump }

    /// <summary>
    /// Definição de um item (ScriptableObject). Crie novos itens em
    /// Assets ▸ Create ▸ Space Chico ▸ Item.
    /// </summary>
    [CreateAssetMenu(menuName = "Space Chico/Item", fileName = "NewItem")]
    public class ItemData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public Sprite icon;
        public ItemCategory category;
        public PowerType power = PowerType.None;
        [Tooltip("Duração do poder em segundos (apenas para Power)")]
        public float powerDuration = 6f;
        public Color accent = new Color(0.4f, 1f, 0.9f);

        public string CategoryLabel
        {
            get
            {
                switch (category)
                {
                    case ItemCategory.ShipPart: return "PEÇA DA NAVE";
                    case ItemCategory.Power: return "PODER  [F]";
                    default: return "CONSUMÍVEL";
                }
            }
        }
    }

    [Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int count;
    }

    /// <summary>A mochila do Chico.</summary>
    public class Inventory : MonoBehaviour
    {
        public int capacity = 8;
        public List<InventorySlot> slots = new List<InventorySlot>();
        public ItemData equippedPower;

        public event Action Changed;

        public bool Add(ItemData item, int amount = 1)
        {
            if (item == null) return false;
            var slot = slots.Find(s => s.item == item);
            if (slot == null)
            {
                if (slots.Count >= capacity) return false;
                slot = new InventorySlot { item = item, count = 0 };
                slots.Add(slot);
            }
            slot.count += amount;
            if (item.category == ItemCategory.Power && equippedPower == null) equippedPower = item;
            Changed?.Invoke();
            return true;
        }

        public int Count(ItemData item)
        {
            var s = slots.Find(x => x.item == item);
            return s != null ? s.count : 0;
        }

        public bool Consume(ItemData item)
        {
            var s = slots.Find(x => x.item == item);
            if (s == null || s.count <= 0) return false;
            s.count--;
            if (s.count <= 0)
            {
                slots.Remove(s);
                if (equippedPower == item) equippedPower = FirstPower();
            }
            Changed?.Invoke();
            return true;
        }

        public ItemData FirstPower()
        {
            foreach (var s in slots) if (s.item.category == ItemCategory.Power && s.count > 0) return s.item;
            return null;
        }

        public void Equip(ItemData item)
        {
            if (item != null && item.category == ItemCategory.Power) { equippedPower = item; Changed?.Invoke(); }
        }

        public bool HasShipPart(ItemData item) => Count(item) > 0;
    }
}
