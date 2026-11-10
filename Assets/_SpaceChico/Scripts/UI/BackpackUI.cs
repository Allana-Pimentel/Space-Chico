using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    // Mantido apenas para compatibilidade com a cena (sistema de mochila desativado no modo Arcade)
    public class BackpackUI : MonoBehaviour
    {
        public GameObject panelRoot;
        public Transform slotsContainer;
        public GameObject slotPrefab;
        public Image detailIcon;
        public Text detailName;
        public Text detailCategory;
        public Text detailDescription;
        public Text detailQuantity;
        public Button closeButton;

        private void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void Open(Inventory inv, ItemData highlightItem = null)
        {
            Close();
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Backpack)
                GameManager.Instance.CloseBackpack();
        }
    }

    public class BackpackSlotUI : MonoBehaviour
    {
        public Image icon;
        public Text countText;
        public Image selectionBorder;
    }
}
