using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    // Cutscene inicial mostrando o Chico feliz na nave e caindo no planeta
    public class IntroShipCutscene : MonoBehaviour
    {
        private static bool jaMostrouIntro = false;

        private GameObject canvasObj;
        private RawImage cenaImage;
        private Image alertaOverlay;
        private Image flashBranco;
        private Text legendaText;
        private Text pularText;

        private Texture2D texNaveFeliz;
        private Texture2D texNaveCaindo;
        private bool pulouCutscene = false;

        public static bool DeveTocarIntro()
        {
            return !jaMostrouIntro;
        }

        public void IniciarCutscene()
        {
            jaMostrouIntro = true;
            texNaveFeliz = Resources.Load<Texture2D>("Intro/intro_ship_happy");
            texNaveCaindo = Resources.Load<Texture2D>("Intro/intro_ship_falling");

            CriarInterfaceCutscene();
            StartCoroutine(RotinaCutscene());
        }

        private void Update()
        {
            if (canvasObj != null && !pulouCutscene)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
                {
                    pulouCutscene = true;
                }
            }
        }

        private void CriarInterfaceCutscene()
        {
            canvasObj = new GameObject("Canvas_IntroVideo");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Fundo escuro
            GameObject bgObj = new GameObject("Fundo");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.03f, 0.02f, 0.07f, 1f);
            RectTransform bgRt = bgImg.rectTransform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Imagem principal do vídeo/cutscene
            GameObject imgObj = new GameObject("CenaIlustrada");
            imgObj.transform.SetParent(canvasObj.transform, false);
            cenaImage = imgObj.AddComponent<RawImage>();
            cenaImage.texture = texNaveFeliz;
            RectTransform imgRt = cenaImage.rectTransform;
            imgRt.anchorMin = Vector2.zero;
            imgRt.anchorMax = Vector2.one;
            imgRt.offsetMin = new Vector2(-40f, -40f);
            imgRt.offsetMax = new Vector2(40f, 40f);

            // Overlay de alerta vermelho (para a pane na nave)
            GameObject alertObj = new GameObject("AlertaVermelho");
            alertObj.transform.SetParent(canvasObj.transform, false);
            alertaOverlay = alertObj.AddComponent<Image>();
            alertaOverlay.color = new Color(1f, 0.1f, 0.1f, 0f);
            RectTransform alertRt = alertaOverlay.rectTransform;
            alertRt.anchorMin = Vector2.zero;
            alertRt.anchorMax = Vector2.one;
            alertRt.offsetMin = Vector2.zero;
            alertRt.offsetMax = Vector2.zero;

            // Barra inferior de cinema para legenda
            GameObject barraInf = new GameObject("BarraLegenda");
            barraInf.transform.SetParent(canvasObj.transform, false);
            Image barraImg = barraInf.AddComponent<Image>();
            barraImg.color = new Color(0.02f, 0.02f, 0.06f, 0.88f);
            RectTransform barraRt = barraImg.rectTransform;
            barraRt.anchorMin = new Vector2(0f, 0f);
            barraRt.anchorMax = new Vector2(1f, 0.16f);
            barraRt.offsetMin = Vector2.zero;
            barraRt.offsetMax = Vector2.zero;

            Font fontePadrao = Resources.Load<Font>("Fonts/RussoOne-Regular");
            if (fontePadrao == null) fontePadrao = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Texto da legenda
            GameObject legObj = new GameObject("TextoLegenda");
            legObj.transform.SetParent(barraInf.transform, false);
            legendaText = legObj.AddComponent<Text>();
            legendaText.font = fontePadrao;
            legendaText.fontSize = 34;
            legendaText.alignment = TextAnchor.MiddleCenter;
            legendaText.color = new Color(0.95f, 0.98f, 1f, 1f);
            RectTransform legRt = legendaText.rectTransform;
            legRt.anchorMin = Vector2.zero;
            legRt.anchorMax = Vector2.one;
            legRt.offsetMin = new Vector2(60f, 10f);
            legRt.offsetMax = new Vector2(-60f, -10f);

            // Aviso para pular a abertura
            GameObject skipObj = new GameObject("AvisoPular");
            skipObj.transform.SetParent(canvasObj.transform, false);
            pularText = skipObj.AddComponent<Text>();
            pularText.font = fontePadrao;
            pularText.fontSize = 22;
            pularText.alignment = TextAnchor.UpperRight;
            pularText.color = new Color(0.8f, 0.95f, 1f, 0.85f);
            pularText.text = "[ESPAÇO] Pular Abertura";
            RectTransform skipRt = pularText.rectTransform;
            skipRt.anchorMin = new Vector2(1f, 1f);
            skipRt.anchorMax = new Vector2(1f, 1f);
            skipRt.pivot = new Vector2(1f, 1f);
            skipRt.anchoredPosition = new Vector2(-36f, -28f);
            skipRt.sizeDelta = new Vector2(450f, 50f);

            // Flash branco de transição
            GameObject flashObj = new GameObject("FlashBranco");
            flashObj.transform.SetParent(canvasObj.transform, false);
            flashBranco = flashObj.AddComponent<Image>();
            flashBranco.color = new Color(1f, 1f, 1f, 0f);
            RectTransform flashRt = flashBranco.rectTransform;
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;
            flashRt.offsetMin = Vector2.zero;
            flashRt.offsetMax = Vector2.zero;
        }

        private IEnumerator RotinaCutscene()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Cutscene);

            PlayerController player = GameManager.Instance != null ? GameManager.Instance.player : FindFirstObjectByType<PlayerController>();
            Vector3 posInicialPlayer = player != null ? player.transform.position : new Vector3(-1.5f, -0.95f, 0f);

            // ATO 1: Chico feliz pilotando a nave pelo espaço
            cenaImage.texture = texNaveFeliz;
            legendaText.text = "Chico viajava feliz pelo espaço em sua nave exploradora...";
            AudioManager.Play(Sfx.PowerUp, 0.7f);

            float t = 0f;
            while (t < 3.4f && !pulouCutscene)
            {
                t += Time.unscaledDeltaTime;
                float zoom = 1.0f + (t / 3.4f) * 0.06f;
                float flutuacaoY = Mathf.Sin(t * 2.8f) * 14f;
                cenaImage.rectTransform.localScale = new Vector3(zoom, zoom, 1f);
                cenaImage.rectTransform.anchoredPosition = new Vector2(0f, flutuacaoY);
                yield return null;
            }

            // ATO 2: Alerta de pane na cabine!
            if (!pulouCutscene)
            {
                legendaText.text = "De repente, uma pane no propulsor fez a nave perder o controle!";
                AudioManager.Play(Sfx.Alert, 0.9f);

                t = 0f;
                float proxAlarme = 0.65f;
                while (t < 2.2f && !pulouCutscene)
                {
                    t += Time.unscaledDeltaTime;
                    if (t >= proxAlarme)
                    {
                        proxAlarme += 0.65f;
                        AudioManager.Play(Sfx.Alert, 0.8f);
                    }

                    // Piscar luz vermelha de emergência e tremer a cabine
                    float alphaVermelho = Mathf.Abs(Mathf.Sin(t * 9f)) * 0.42f;
                    alertaOverlay.color = new Color(1f, 0.1f, 0.1f, alphaVermelho);
                    cenaImage.rectTransform.anchoredPosition = new Vector2(
                        Mathf.Sin(t * 45f) * 16f,
                        Mathf.Cos(t * 38f) * 14f
                    );
                    yield return null;
                }
            }

            // ATO 3: Nave caindo no pântano!
            if (!pulouCutscene)
            {
                flashBranco.color = new Color(1f, 0.9f, 0.7f, 0.85f);
                cenaImage.texture = texNaveCaindo;
                alertaOverlay.color = new Color(1f, 0.2f, 0.1f, 0.15f);
                legendaText.text = "A nave despencou em direção ao Pântano Bioluminescente!";
                AudioManager.Play(Sfx.BossSpit, 0.85f);

                t = 0f;
                while (t < 2.8f && !pulouCutscene)
                {
                    t += Time.unscaledDeltaTime;
                    flashBranco.color = new Color(1f, 0.9f, 0.7f, Mathf.Max(0f, 0.85f - t * 2.5f));

                    // Movimento de queda diagonal + vibração de entrada na atmosfera
                    float progresso = t / 2.8f;
                    float escala = 1.02f + progresso * 0.10f;
                    float descidaX = Mathf.Lerp(-25f, 25f, progresso) + Mathf.Sin(t * 50f) * 12f;
                    float descidaY = Mathf.Lerp(25f, -30f, progresso) + Mathf.Cos(t * 42f) * 12f;
                    cenaImage.rectTransform.localScale = new Vector3(escala, escala, 1f);
                    cenaImage.rectTransform.anchoredPosition = new Vector2(descidaX, descidaY);
                    yield return null;
                }
            }

            // Impacto da queda e transição para o Chico caindo na fase ao lado da nave
            AudioManager.Play(Sfx.BossSlam, 0.95f);
            if (canvasObj != null) Destroy(canvasObj);

            if (player != null && !pulouCutscene)
            {
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                bool tinhaRb = rb != null;
                if (tinhaRb) rb.simulated = false;

                // Faz o Chico cair do alto até o chão ao lado da nave acidentada
                Vector3 posAlto = posInicialPlayer + new Vector3(-1.2f, 6.2f, 0f);
                player.transform.position = posAlto;
                if (player.spriteFall != null && player.spriteRenderer != null)
                    player.spriteRenderer.sprite = player.spriteFall;

                CameraFollow.Shake(0.35f, 0.35f);
                VFX.Burst(posInicialPlayer + new Vector3(-2.0f, 0.6f, 0f), new Color(1f, 0.6f, 0.2f), 24, 5f, 0.3f, 0.6f);

                float quedaT = 0f;
                float duracaoQueda = 0.65f;
                while (quedaT < duracaoQueda)
                {
                    quedaT += Time.deltaTime;
                    float p = Mathf.Clamp01(quedaT / duracaoQueda);
                    player.transform.position = Vector3.Lerp(posAlto, posInicialPlayer, p * p);

                    if (Random.value < 0.45f)
                        VFX.Burst(player.transform.position, new Color(1f, 0.75f, 0.3f), 3, 2f, 0.15f, 0.25f);

                    yield return null;
                }

                player.transform.position = posInicialPlayer;
                if (tinhaRb)
                {
                    rb.simulated = true;
                    rb.linearVelocity = Vector2.zero;
                }

                AudioManager.Play(Sfx.Land, 0.8f);
                CameraFollow.Shake(0.25f, 0.22f);
                VFX.Burst(posInicialPlayer, new Color(0.4f, 1f, 0.8f), 14, 3.5f, 0.2f, 0.35f);
                VFX.PopText(posInicialPlayer + Vector3.up * 1.4f, "RECUPERE O PROPULSOR!", new Color(0.4f, 1f, 0.9f), 1.4f);
            }

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Playing);

            Destroy(this);
        }
    }
}
