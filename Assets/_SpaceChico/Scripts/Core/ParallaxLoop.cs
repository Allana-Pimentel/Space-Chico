using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Gerenciador de Paralaxe com Loop Infinito (Esteira de Fundo).
    /// Organiza 3 painéis lado a lado e os reposiciona automaticamente conforme
    /// a câmera se move, garantindo um fundo contínuo sem emendas e sem fim.
    /// </summary>
    public class ParallaxLoop : MonoBehaviour
    {
        [Header("Configurações da Câmera")]
        [Tooltip("Câmera principal que o fundo deve acompanhar")]
        public Transform cam;

        [Header("Efeito de Profundidade")]
        [Tooltip("0 = fundo anda junto com o jogador; 1 = fundo fica estático; 0.7 = efeito de profundidade suave")]
        [Range(0f, 1f)]
        public float parallaxFactor = 0.65f;

        [Tooltip("Acompanha suavemente o movimento vertical da câmera")]
        public float verticalFollowFactor = 0.3f;

        [Header("Painéis de Fundo")]
        public SpriteRenderer[] panels;

        private float panelWidth;
        private Vector3 lastCamPos;
        private float initialY;

        private void Start()
        {
            if (cam == null && Camera.main != null)
            {
                cam = Camera.main.transform;
            }

            if (cam != null)
            {
                lastCamPos = cam.position;
            }

            initialY = transform.position.y;

            // Se os painéis não foram atribuídos manualmente, busca nos filhos
            if (panels == null || panels.Length == 0)
            {
                panels = GetComponentsInChildren<SpriteRenderer>();
            }

            if (panels != null && panels.Length > 0 && panels[0].sprite != null)
            {
                // Calcula a largura real do sprite no mundo
                panelWidth = panels[0].bounds.size.x;

                // Organiza os 3 painéis perfeitamente alinhados lado a lado no início
                // Painel 0 (Esquerda), Painel 1 (Centro), Painel 2 (Direita)
                float startCenterX = cam != null ? cam.position.x : transform.position.x;
                if (panels.Length >= 3)
                {
                    panels[0].transform.position = new Vector3(startCenterX - panelWidth, initialY, transform.position.z);
                    panels[1].transform.position = new Vector3(startCenterX, initialY, transform.position.z);
                    panels[2].transform.position = new Vector3(startCenterX + panelWidth, initialY, transform.position.z);
                }
            }
        }

        private void LateUpdate()
        {
            if (cam == null || panels == null || panels.Length < 3 || panelWidth <= 0f)
                return;

            // 1. Calcula o quanto a câmera se moveu neste frame
            Vector3 camDelta = cam.position - lastCamPos;

            // 2. Aplica o movimento de paralaxe (o fundo anda uma fração do movimento do jogador)
            float moveX = camDelta.x * parallaxFactor;
            float moveY = camDelta.y * verticalFollowFactor;

            foreach (var panel in panels)
            {
                panel.transform.position += new Vector3(moveX, moveY, 0f);

                // 3. Efeito Esteira (Loop):
                // Se a câmera andou para a direita e o painel ficou para trás (saiu da tela pela esquerda)
                float totalWidth = panelWidth * panels.Length;
                float distFromCam = cam.position.x - panel.transform.position.x;

                if (distFromCam > panelWidth * 1.5f)
                {
                    // Move o painel para a frente da fila
                    panel.transform.position += new Vector3(totalWidth, 0f, 0f);
                }
                else if (distFromCam < -panelWidth * 1.5f)
                {
                    // Move o painel para trás da fila (caso o jogador volte)
                    panel.transform.position -= new Vector3(totalWidth, 0f, 0f);
                }
            }

            lastCamPos = cam.position;
        }
    }
}

