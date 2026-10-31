using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    // Abertura completa:
    // 1. História do Chico subindo no espaço estilo Star Wars + Chico feliz na nave
    // 2. Pane no motor e a nave do Chico rasgando a tela em alta velocidade
    // 3. Queda da nave e do Chico no Pântano Bioluminescente
    public class IntroShipCutscene : MonoBehaviour
    {
        private static bool jaMostrouIntro = false;

        private GameObject canvasObj;
        private RawImage fundoImage;
        private RectTransform crawlContainer;
        private Text crawlText;
        private RectTransform naveRasgandoRt;
        private Image rastroCorteImg;
        private Image alertaOverlay;
        private Image flashBranco;
        private Text legendaText;
        private GameObject barraLegendaObj;

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
            canvasObj = new GameObject("Canvas_IntroStarWars");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Fundo espacial escuro
            GameObject bgObj = new GameObject("FundoEspaco");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.01f, 0.06f, 1f);
            RectTransform bgRt = bgImg.rectTransform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Imagem ilustrada de fundo (Chico feliz na nave / Nave caindo)
            GameObject imgObj = new GameObject("CenaIlustrada");
            imgObj.transform.SetParent(canvasObj.transform, false);
            fundoImage = imgObj.AddComponent<RawImage>();
            fundoImage.texture = texNaveFeliz;
            fundoImage.color = new Color(1f, 1f, 1f, 0.38f);
            RectTransform imgRt = fundoImage.rectTransform;
            imgRt.anchorMin = Vector2.zero;
            imgRt.anchorMax = Vector2.one;
            imgRt.offsetMin = new Vector2(-40f, -40f);
            imgRt.offsetMax = new Vector2(40f, 40f);

            Font fonteTitulo = Resources.Load<Font>("Fonts/RussoOne-Regular");
            if (fonteTitulo == null) fonteTitulo = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Texto estilo Star Wars em perspectiva subindo pelo espaço
            GameObject crawlObj = new GameObject("StarWarsCrawl");
            crawlObj.transform.SetParent(canvasObj.transform, false);
            crawlContainer = crawlObj.AddComponent<RectTransform>();
            crawlContainer.anchorMin = new Vector2(0.5f, 0.5f);
            crawlContainer.anchorMax = new Vector2(0.5f, 0.5f);
            crawlContainer.sizeDelta = new Vector2(1100f, 1300f);
            crawlContainer.anchoredPosition = new Vector2(0f, -680f);
            crawlContainer.localRotation = Quaternion.Euler(28f, 0f, 0f);

            crawlText = crawlObj.AddComponent<Text>();
            crawlText.font = fonteTitulo;
            crawlText.fontSize = 36;
            crawlText.lineSpacing = 1.35f;
            crawlText.alignment = TextAnchor.UpperCenter;
            crawlText.color = new Color(1f, 0.88f, 0.25f, 1f);
            crawlText.text =
                "SPACE CHICO\n" +
                "— Capítulo I: O Pântano Bioluminescente —\n\n" +
                "O destemido felino interestelar CHICO viajava feliz\n" +
                "pelo cosmos a bordo de sua nave exploradora.\n\n" +
                "De repente, uma falha catastrófica no motor de dobra\n" +
                "fez a espaçonave perder o controle!\n\n" +
                "Atraída pela gravidade de um planeta selvagem,\n" +
                "sua nave rasgou o espaço e despencou entre vapores\n" +
                "tóxicos. Agora Chico precisa recuperar o\n" +
                "PROPULSOR QUÂNTICO para voltar às estrelas!";

            Shadow crawlShadow = crawlObj.AddComponent<Shadow>();
            crawlShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            crawlShadow.effectDistance = new Vector2(3f, -3f);

            // Feixe luminoso que "rasga a tela" quando a nave passa
            GameObject rastroObj = new GameObject("RastroCorteTela");
            rastroObj.transform.SetParent(canvasObj.transform, false);
            rastroCorteImg = rastroObj.AddComponent<Image>();
            rastroCorteImg.color = new Color(0.5f, 0.95f, 1f, 0f);
            RectTransform rastroRt = rastroCorteImg.rectTransform;
            rastroRt.anchorMin = new Vector2(0.5f, 0.5f);
            rastroRt.anchorMax = new Vector2(0.5f, 0.5f);
            rastroRt.sizeDelta = new Vector2(2600f, 18f);
            rastroRt.localRotation = Quaternion.Euler(0f, 0f, -22f);

            // Nave do Chico que cruza/rasga a tela em alta velocidade
            GameObject navePassandoObj = new GameObject("NaveRasgandoTela");
            navePassandoObj.transform.SetParent(canvasObj.transform, false);
            naveRasgandoRt = navePassandoObj.AddComponent<RectTransform>();
            naveRasgandoRt.anchorMin = new Vector2(0.5f, 0.5f);
            naveRasgandoRt.anchorMax = new Vector2(0.5f, 0.5f);
            naveRasgandoRt.sizeDelta = new Vector2(520f, 320f);
            naveRasgandoRt.anchoredPosition = new Vector2(-1400f, 520f);

            // Tenta usar o próprio sprite da nave/Chico na passagem
            GameObject propShip = GameObject.Find("Prop_CrashedShip");
            Sprite sprNave = propShip != null && propShip.GetComponent<SpriteRenderer>() != null
                ? propShip.GetComponent<SpriteRenderer>().sprite
                : null;

            if (sprNave != null)
            {
                Image imgNave = navePassandoObj.AddComponent<Image>();
                imgNave.sprite = sprNave;
                imgNave.preserveAspect = true;
                naveRasgandoRt.localScale = new Vector3(-1f, 1f, 1f);
            }
            else
            {
                RawImage rawNave = navePassandoObj.AddComponent<RawImage>();
                rawNave.texture = texNaveCaindo;
            }

            // Overlay de alerta vermelho
            GameObject alertObj = new GameObject("AlertaVermelho");
            alertObj.transform.SetParent(canvasObj.transform, false);
            alertaOverlay = alertObj.AddComponent<Image>();
            alertaOverlay.color = new Color(1f, 0.1f, 0.1f, 0f);
            RectTransform alertRt = alertaOverlay.rectTransform;
            alertRt.anchorMin = Vector2.zero;
            alertRt.anchorMax = Vector2.one;
            alertRt.offsetMin = Vector2.zero;
            alertRt.offsetMax = Vector2.zero;

            // Barra inferior de legenda
            barraLegendaObj = new GameObject("BarraLegenda");
            barraLegendaObj.transform.SetParent(canvasObj.transform, false);
            Image barraImg = barraLegendaObj.AddComponent<Image>();
            barraImg.color = new Color(0.02f, 0.02f, 0.06f, 0.88f);
            RectTransform barraRt = barraImg.rectTransform;
            barraRt.anchorMin = new Vector2(0f, 0f);
            barraRt.anchorMax = new Vector2(1f, 0.14f);
            barraRt.offsetMin = Vector2.zero;
            barraRt.offsetMax = Vector2.zero;

            GameObject legObj = new GameObject("TextoLegenda");
            legObj.transform.SetParent(barraLegendaObj.transform, false);
            legendaText = legObj.AddComponent<Text>();
            legendaText.font = fonteTitulo;
            legendaText.fontSize = 32;
            legendaText.alignment = TextAnchor.MiddleCenter;
            legendaText.color = new Color(0.95f, 0.98f, 1f, 1f);
            RectTransform legRt = legendaText.rectTransform;
            legRt.anchorMin = Vector2.zero;
            legRt.anchorMax = Vector2.one;
            legRt.offsetMin = new Vector2(50f, 8f);
            legRt.offsetMax = new Vector2(-50f, -8f);
            barraLegendaObj.SetActive(false);

            // Aviso para pular a abertura
            GameObject skipObj = new GameObject("AvisoPular");
            skipObj.transform.SetParent(canvasObj.transform, false);
            Text pularText = skipObj.AddComponent<Text>();
            pularText.font = fonteTitulo;
            pularText.fontSize = 22;
            pularText.alignment = TextAnchor.UpperRight;
            pularText.color = new Color(0.85f, 0.95f, 1f, 0.85f);
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

            // =========================================================================
            // PARTE 1: Texto estilo Star Wars subindo no espaço + Chico feliz na nave
            // =========================================================================
            fundoImage.texture = texNaveFeliz;
            fundoImage.color = new Color(1f, 1f, 1f, 0.35f);
            AudioManager.Play(Sfx.PowerUp, 0.65f);

            float duracaoCrawl = 6.2f;
            float t = 0f;
            while (t < duracaoCrawl && !pulouCutscene)
            {
                t += Time.unscaledDeltaTime;
                float p = t / duracaoCrawl;

                // Sobe o texto estilo Star Wars e diminui levemente a escala conforme vai para o fundo do espaço
                float posY = Mathf.Lerp(-560f, 680f, p);
                float escalaCrawl = Mathf.Lerp(1.05f, 0.58f, p);
                crawlContainer.anchoredPosition = new Vector2(0f, posY);
                crawlContainer.localScale = new Vector3(escalaCrawl, escalaCrawl, 1f);

                // Revela o Chico feliz na cabine da nave gradualmente enquanto o texto sobe
                float alphaFundo = Mathf.Lerp(0.32f, 0.95f, Mathf.Clamp01((p - 0.35f) / 0.5f));
                fundoImage.color = new Color(1f, 1f, 1f, alphaFundo);
                fundoImage.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Sin(t * 2.5f) * 12f);

                // Desvanece o texto no final da subida
                if (p > 0.78f)
                {
                    float alphaTexto = Mathf.Clamp01(1f - (p - 0.78f) / 0.22f);
                    crawlText.color = new Color(1f, 0.88f, 0.25f, alphaTexto);
                }

                yield return null;
            }

            crawlContainer.gameObject.SetActive(false);
            barraLegendaObj.SetActive(true);

            // =========================================================================
            // PARTE 2: Chico feliz na nave -> Alerta de pane -> NAVE RASGA A TELA!
            // =========================================================================
            if (!pulouCutscene)
            {
                fundoImage.color = Color.white;
                legendaText.text = "Chico viajava feliz pelo espaço... até o motor sofrer uma pane!";
                AudioManager.Play(Sfx.Alert, 0.95f);

                t = 0f;
                while (t < 1.5f && !pulouCutscene)
                {
                    t += Time.unscaledDeltaTime;
                    float alphaVermelho = Mathf.Abs(Mathf.Sin(t * 11f)) * 0.45f;
                    alertaOverlay.color = new Color(1f, 0.1f, 0.1f, alphaVermelho);
                    fundoImage.rectTransform.anchoredPosition = new Vector2(
                        Mathf.Sin(t * 48f) * 15f,
                        Mathf.Cos(t * 40f) * 13f
                    );
                    yield return null;
                }

                // A NAVE DO CHICO RASGA A TELA!
                AudioManager.Play(Sfx.Shoot, 1.0f);
                AudioManager.Play(Sfx.BossSpit, 0.95f);
                legendaText.text = "A nave desgovernada rasgou o espaço rumo ao planeta!";

                t = 0f;
                float duracaoRasgo = 0.75f;
                Vector2 iniNave = new Vector2(-1350f, 480f);
                Vector2 fimNave = new Vector2(1350f, -480f);

                while (t < duracaoRasgo && !pulouCutscene)
                {
                    t += Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(t / duracaoRasgo);

                    naveRasgandoRt.anchoredPosition = Vector2.Lerp(iniNave, fimNave, p);
                    naveRasgandoRt.localRotation = Quaternion.Euler(0f, 0f, -22f + Mathf.Sin(t * 60f) * 6f);

                    // Feixe brilhante que rasga a tela no caminho da nave
                    float larguraCorte = Mathf.Sin(p * Mathf.PI) * 54f;
                    rastroCorteImg.rectTransform.sizeDelta = new Vector2(2600f, Mathf.Max(6f, larguraCorte));
                    rastroCorteImg.color = new Color(0.6f, 0.98f, 1f, Mathf.Sin(p * Mathf.PI) * 0.95f);

                    if (p > 0.65f)
                    {
                        flashBranco.color = new Color(0.85f, 0.98f, 1f, (p - 0.65f) / 0.35f);
                    }

                    yield return null;
                }

                naveRasgandoRt.gameObject.SetActive(false);
                rastroCorteImg.gameObject.SetActive(false);
            }

            // =========================================================================
            // PARTE 3: Vemos o Chico cair nesse planeta (Cena ilustrada + Queda real!)
            // =========================================================================
            if (!pulouCutscene)
            {
                fundoImage.texture = texNaveCaindo;
                alertaOverlay.color = new Color(1f, 0.2f, 0.1f, 0.12f);
                legendaText.text = "Cuidado, Chico! Queda livre no Pântano Bioluminescente!";
                AudioManager.Play(Sfx.Alert, 0.75f);

                t = 0f;
                while (t < 2.3f && !pulouCutscene)
                {
                    t += Time.unscaledDeltaTime;
                    flashBranco.color = new Color(1f, 1f, 1f, Mathf.Max(0f, 1f - t * 3f));

                    float p = t / 2.3f;
                    float escala = 1.02f + p * 0.10f;
                    float descidaX = Mathf.Lerp(25f, -25f, p) + Mathf.Sin(t * 50f) * 12f;
                    float descidaY = Mathf.Lerp(25f, -25f, p) + Mathf.Cos(t * 42f) * 12f;
                    fundoImage.rectTransform.localScale = new Vector3(escala, escala, 1f);
                    fundoImage.rectTransform.anchoredPosition = new Vector2(descidaX, descidaY);
                    yield return null;
                }
            }

            // Remove o overlay e mostra a nave e o próprio Chico caindo no cenário da fase!
            if (canvasObj != null) Destroy(canvasObj);

            if (player != null && !pulouCutscene)
            {
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                bool tinhaRb = rb != null;
                if (tinhaRb) rb.simulated = false;

                GameObject propShip = GameObject.Find("Prop_CrashedShip");
                Vector3 posFinalShip = propShip != null ? propShip.transform.position : (posInicialPlayer + new Vector3(-2.5f, 0f, 0f));
                Vector3 posAltoShip = posFinalShip + new Vector3(-4.5f, 7.5f, 0f);

                Vector3 posAltoPlayer = posInicialPlayer + new Vector3(-2.5f, 7.2f, 0f);
                player.transform.position = posAltoPlayer;
                if (player.spriteFall != null && player.spriteRenderer != null)
                    player.spriteRenderer.sprite = player.spriteFall;

                // Nave e Chico despencam do céu do planeta até o chão do pântano
                float quedaT = 0f;
                float duracaoQueda = 0.85f;
                while (quedaT < duracaoQueda)
                {
                    quedaT += Time.deltaTime;
                    float p = Mathf.Clamp01(quedaT / duracaoQueda);
                    float curva = p * p;

                    if (propShip != null)
                        propShip.transform.position = Vector3.Lerp(posAltoShip, posFinalShip, curva);

                    player.transform.position = Vector3.Lerp(posAltoPlayer, posInicialPlayer, curva);

                    if (Random.value < 0.6f)
                    {
                        Vector3 posFumaca = propShip != null ? propShip.transform.position : player.transform.position;
                        VFX.Burst(posFumaca + Vector3.up * 0.8f, new Color(1f, 0.65f, 0.2f), 4, 3f, 0.22f, 0.35f);
                    }

                    yield return null;
                }

                if (propShip != null) propShip.transform.position = posFinalShip;
                player.transform.position = posInicialPlayer;

                if (tinhaRb)
                {
                    rb.simulated = true;
                    rb.linearVelocity = Vector2.zero;
                }

                // Impacto da queda no planeta!
                AudioManager.Play(Sfx.BossSlam, 1.0f);
                AudioManager.Play(Sfx.Land, 0.85f);
                CameraFollow.Shake(0.4f, 0.35f);
                VFX.Burst(posFinalShip + Vector3.up * 0.5f, new Color(1f, 0.55f, 0.15f), 28, 5.5f, 0.32f, 0.65f);
                VFX.Burst(posInicialPlayer, new Color(0.4f, 1f, 0.8f), 16, 3.8f, 0.22f, 0.4f);
                VFX.PopText(posInicialPlayer + Vector3.up * 1.5f, "RECUPERE O PROPULSOR!", new Color(0.4f, 1f, 0.9f), 1.4f);
            }

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Playing);

            Destroy(this);
        }
    }
}
