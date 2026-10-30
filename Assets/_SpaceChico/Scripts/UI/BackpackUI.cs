using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    /// <summary>
    /// Interface da Mochila / Inventário do Chico (GDD Seção 4 e 7.2: Tecla B).
    /// Congela o jogo em TimeScale = 0, exibe itens e detalhes minuciosos.
    /// </summary>
    public class BackpackUI : MonoBehaviour
    {
        [Header("Painel Principal")]
        public GameObject panelRoot;

        [Header("Lista de Slots")]
        public Transform slotsContainer;
        public GameObject slotPrefab;

        [Header("Painel de Detalhes")]
        public Image detailIcon;
        public Text detailName;
        public Text detailCategory;
        public Text detailDescription;
        public Text detailQuantity;
        public Button closeButton;

        private Inventory targetInventory;
        private readonly List<BackpackSlotUI> slotElements = new List<BackpackSlotUI>();
        private int selectedIndex;
        private bool isOpen;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        public void Open(Inventory inv, ItemData highlightItem = null)
        {
            targetInventory = inv;
            isOpen = true;
            if (panelRoot != null) panelRoot.SetActive(true);

            RefreshSlots();

            // Destaca item específico se informado
            if (highlightItem != null)
            {
                int idx = slotElements.FindIndex(s => s.Slot != null && s.Slot.item == highlightItem);
                if (idx >= 0) SelectSlot(idx);
                else SelectSlot(0);
            }
            else
            {
                SelectSlot(0);
            }
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            if (panelRoot != null) panelRoot.SetActive(false);

            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Backpack)
            {
                GameManager.Instance.CloseBackpack();
            }
        }

        private void Update()
        {
            if (!isOpen) return;

            // Fechar ao pressionar B, Esc ou Enter/Espaço
            if (GameInput.Pressed(GameAction.Backpack) || GameInput.Pressed(GameAction.Pause))
            {
                Close();
                return;
            }

            // Navegação de slots via teclado
            if (GameInput.Pressed(GameAction.Right))
            {
                SelectSlot((selectedIndex + 1) % Mathf.Max(1, slotElements.Count));
                AudioManager.Play(Sfx.UiMove);
            }
            else if (GameInput.Pressed(GameAction.Left))
            {
                SelectSlot((selectedIndex - 1 + slotElements.Count) % Mathf.Max(1, slotElements.Count));
                AudioManager.Play(Sfx.UiMove);
            }

            // Equipar poder com F
            if (GameInput.Pressed(GameAction.Power) && selectedIndex >= 0 && selectedIndex < slotElements.Count)
            {
                var slot = slotElements[selectedIndex].Slot;
                if (slot != null && slot.item.category == ItemCategory.Power && targetInventory != null)
                {
                    targetInventory.Equip(slot.item);
                    AudioManager.Play(Sfx.UiSelect);
                    RefreshSlots();
                }
            }
        }

        public void RefreshSlots()
        {
            if (slotsContainer == null) return;

            // Limpa elementos antigos
            foreach (var s in slotElements)
            {
                if (s != null) Destroy(s.gameObject);
            }
            slotElements.Clear();

            if (targetInventory == null || targetInventory.slots == null || targetInventory.slots.Count == 0)
            {
                ShowEmptyDetails();
                return;
            }

            for (int i = 0; i < targetInventory.slots.Count; i++)
            {
                var slot = targetInventory.slots[i];
                int index = i;

                GameObject obj = null;
                if (slotPrefab != null)
                {
                    obj = Instantiate(slotPrefab, slotsContainer);
                }
                else
                {
                    obj = CreateDefaultSlotObject(slotsContainer);
                }

                var slotUI = obj.GetComponent<BackpackSlotUI>();
                if (slotUI == null) slotUI = obj.AddComponent<BackpackSlotUI>();

                slotUI.Setup(slot, () => SelectSlot(index));
                slotElements.Add(slotUI);
            }

            if (selectedIndex >= slotElements.Count) selectedIndex = 0;
            SelectSlot(selectedIndex);
        }

        public void SelectSlot(int index)
        {
            if (slotElements.Count == 0)
            {
                ShowEmptyDetails();
                return;
            }

            selectedIndex = Mathf.Clamp(index, 0, slotElements.Count - 1);

            for (int i = 0; i < slotElements.Count; i++)
            {
                slotElements[i].SetSelected(i == selectedIndex);
            }

            var slot = slotElements[selectedIndex].Slot;
            if (slot != null && slot.item != null)
            {
                var item = slot.item;
                if (detailIcon != null)
                {
                    detailIcon.enabled = true;
                    if (item.icon != null) detailIcon.sprite = item.icon;
                    detailIcon.color = item.accent;
                }
                if (detailName != null) detailName.text = item.displayName;
                if (detailCategory != null) detailCategory.text = item.CategoryLabel;
                if (detailDescription != null) detailDescription.text = item.description;
                if (detailQuantity != null) detailQuantity.text = $"Quantidade: {slot.count}";
            }
        }

        private void ShowEmptyDetails()
        {
            if (detailIcon != null) detailIcon.enabled = false;
            if (detailName != null) detailName.text = "Mochila Vazia";
            if (detailCategory != null) detailCategory.text = "";
            if (detailDescription != null) detailDescription.text = "Nenhum item coletado ainda. Explore o bioma para recuperar componentes e recursos!";
            if (detailQuantity != null) detailQuantity.text = "";
        }

        private GameObject CreateDefaultSlotObject(Transform parent)
        {
            var go = new GameObject("Slot_Item", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(70, 70);
            return go;
        }
    }

    /// <summary>
    /// Componente de slot individual na grade da mochila.
    /// </summary>
    public class BackpackSlotUI : MonoBehaviour
    {
        public Image icon;
        public Text countText;
        public Image selectionBorder;
        public InventorySlot Slot { get; private set; }

        public void Setup(InventorySlot slot, System.Action onClick)
        {
            Slot = slot;
            var btn = GetComponent<Button>();
            if (btn != null && onClick != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => onClick());
            }

            if (icon == null) icon = GetComponent<Image>();
            if (slot != null && slot.item != null)
            {
                if (icon != null)
                {
                    if (slot.item.icon != null) icon.sprite = slot.item.icon;
                    icon.color = slot.item.accent;
                }
                if (countText != null)
                {
                    countText.text = slot.count > 1 ? slot.count.ToString() : "";
                }
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectionBorder != null)
            {
                selectionBorder.enabled = isSelected;
            }
            transform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;
        }
    }
}
