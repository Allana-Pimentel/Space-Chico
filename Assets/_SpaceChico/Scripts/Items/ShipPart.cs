using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Peça principal da nave.
    /// Ao tocar:
    /// 1. Zera a velocidade do jogador e aciona estado de comemoração.
    /// 2. Adiciona o item ao inventário.
    /// 3. Congela o jogo (Time.timeScale = 0) e abre a mochila destacando o item.
    /// 4. Ao fechar a mochila, concede +1 de Vida Máxima permanente e conclui a fase.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ShipPart : MonoBehaviour
    {
        [Header("Dados")]
        public ItemData itemData;
        public Color glowColor = new Color(0.2f, 0.9f, 1f, 1f);

        [Header("Efeitos Visuais")]
        public GameObject visualObject;

        private bool collected;

        private void Reset()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;
            gameObject.layer = LayerMask.NameToLayer(Layers.Pickup);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;

            var player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                player = other.GetComponentInParent<PlayerController>();
            }

            if (player != null)
            {
                Collect(player);
            }
        }

        private void Collect(PlayerController player)
        {
            collected = true;

            //Zera a velocidade do Rigidbody2D do jogador e aciona comemoração
            player.TriggerCelebrate();

            // Adiciona o item ao inventário
            if (itemData != null && player.Inventory != null)
            {
                player.Inventory.Add(itemData, 1);
            }

            // Efeitos e som de coleta
            AudioManager.Play(Sfx.ShipPart);
            VFX.Burst(transform.position, glowColor, 30, 5f, 0.35f, 0.8f);
            VFX.PopText(transform.position + Vector3.up * 1f, "+1 PEÇA DA NAVE!", glowColor, 1.3f);

            // Desativa visual
            if (visualObject != null) visualObject.SetActive(false);
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;

            //Transfere para a UI da mochila, congelando a cena (TimeScale = 0)
            GameManager.Instance.OpenBackpack(itemData, () =>
            {
                // Seção 7.2 Passo 4: Concede +1 Vida Máxima e conclui a fase
                ProgressData.BonusMaxLife += 1;
                player.Stats.IncreaseMaxLife(1);
                GameManager.Instance.CompleteLevel();
            });

            Destroy(gameObject, 0.1f);
        }
    }
}
