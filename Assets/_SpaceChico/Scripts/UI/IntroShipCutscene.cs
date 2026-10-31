using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    // Abertura animada:
    // 1. Espaço estrelado com a história do Chico subindo em estilo Star Wars
    // 2. A nave do Chico entra voando e CORTA o texto ao meio!
    // 3. O Planeta Bioluminescente surge no espaço e a nave despenca em direção ao planeta
    // 4. Entrada na atmosfera e impacto final no chão da fase
    public class IntroShipCutscene : MonoBehaviour
    {
        private static bool jaMostrouIntro = false;

        private GameObject canvasObj;
        private RectTransform estrelasContainer;
        private List<Image> estrelas = new List<Image>();

        private RectTransform planetaRt;
        private RawImage planetaImg;

        private RectTransform crawlRootRt;
        private RectTransform crawlTopoRt;
        private RectTransform crawlBaseRt;
        private Text crawlTopoText;
        private Text crawlBaseText;

        private RectTransform naveRt;
        private RawImage naveImg;
        private Image rastroCorteImg;
        private Image flashTela;
        private Text legendaText;
        private GameObject barraLegendaObj;

        private Texture2D texNaveSprite;
        private Texture2D texPlanetaSprite;
        private Texture2D texEspacoPlaneta;
        private Texture2D texNaveCaindo;
        private bool pulouCutscene = false;

        public static bool DeveTocarIntro()
        {
            return !jaMostrouIntro;
        }

        public void IniciarCutscene()
        {
            jaMostrouIntro = true;
            texNaveSprite = Resources.Load<Texture2D>("Intro/intro_ship_sprite");
            texPlanetaSprite = Resources.Load<Texture2D>("Intro/intro_planet_sprite");
            texEspacoPlaneta = Resources.Load<Texture2D>("Intro/intro_space_planet");
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

            // 1. Fundo do espaço profundo
            GameObject bgObj = new GameObject("FundoEspaco");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.01f, 0.05f, 1f);
            RectTransform bgRt = bgImg.rectTransform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // 2. Campo de estrelas cintilantes no espaço
            GameObject starsObj = new GameObject("CampoEstrelas");
            starsObj.transform.SetParent(canvasObj.transform, false);
            estrelasContainer = starsObj.AddComponent<RectTransform>();
            estrelasContainer.anchorMin = Vector2.zero;
            estrelasContainer.anchorMax = Vector2.one;
            estrelasContainer.offsetMin = Vector2.zero;
            estrelasContainer.offsetMax = Vector2.zero;

            Random.InitState(42);
            for (int i = 0; i < 75; i++)
            {
                GameObject st = new GameObject("Star_" + i);
                st.transform.SetParent(estrelasContainer, false);
                Image stImg = st.AddComponent<Image>();
                float br = Random.Range(0.55f, 1f);
                stImg.color = (i % 5 == 0)
                    ? new Color(0.5f, 0.95f, 1f, br)
                    : (i % 7 == 0 ? new Color(0.85f, 0.6f, 1f, br) : new Color(1f, 1f, 1f, br));

                RectTransform stRt = stImg.rectTransform;
                float tam = Random.Range(2.5f, 6.5f);
                stRt.sizeDelta = new Vector2(tam, tam);
                stRt.anchoredPosition = new Vector2(Random.Range(-940f, 940f), Random.Range(-520f, 520f));
                estrelas.Add(stImg);
            }

            // 3. O Planeta Bioluminescente no espaço (começa pequeno ao longe e cresce na queda!)
            GameObject planObj = new GameObject("PlanetaBioluminescente");
            planObj.transform.SetParent(canvasObj.transform, false);
            planetaImg = planObj.AddComponent<RawImage>();
            planetaImg.texture = texPlanetaSprite != null ? texPlanetaSprite : texEspacoPlaneta;
            planetaRt = planetaImg.rectTransform;
            planetaRt.anchorMin = new Vector2(0.5f, 0.5f);
            planetaRt.anchorMax = new Vector2(0.5f, 0.5f);
            planetaRt.sizeDelta = new Vector2(260f, 260f);
            planetaRt.anchoredPosition = new Vector2(560f, -240f);
            planetaImg.color = new Color(1f, 1f, 1f, 0.55f);

            Font fonteTitulo = Resources.Load<Font>("Fonts/RussoOne-Regular");
            if (fonteTitulo == null) fonteTitulo = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 4. Texto estilo Star Wars (dividido em metade superior e inferior para ser CORTADO pela nave!)
            GameObject crawlRoot = new GameObject("StarWarsCrawlRoot");
            crawlRoot.transform.SetParent(canvasObj.transform, false);
            crawlRootRt = crawlRoot.AddComponent<RectTransform>();
            crawlRootRt.anchorMin = new Vector2(0.5f, 0.5f);
            crawlRootRt.anchorMax = new Vector2(0.5f, 0.5f);
            crawlRootRt.sizeDelta = new Vector2(1150f, 900f);
            crawlRootRt.anchoredPosition = new Vector2(0f, -520f);
            crawlRootRt.localRotation = Quaternion.Euler(30f, 0f, 0f);

            crawlTopoRt = CriarBlocoCrawl(
                crawlRootRt,
                "CrawlMetadeSuperior",
                fonteTitulo,
                new Vector2(0f, 190f),
                "SPACE CHICO\n" +
                "— Capítulo I: O Pântano Bioluminescente —\n\n" +
                "O destemido felino interestelar CHICO viajava feliz\n" +
                "pelo cosmos a bordo de sua nave exploradora.",
                out crawlTopoText
            );

            crawlBaseRt = CriarBlocoCrawl(
                crawlRootRt,
                "CrawlMetadeInferior",
                fonteTitulo,
                new Vector2(0f, -190f),
                "De repente, uma falha catastrófica no propulsor\n" +
                "fez a espaçonave perder o controle no espaço!\n\n" +
                "Atraído pela gravidade de um planeta selvagem,\n" +
                "Chico precisa sobreviver à queda e recuperar o\n" +
                "PROPULSOR QUÂNTICO para voltar às estrelas!",
                out crawlBaseText
            );

            // 5. Feixe de plasma que corta o texto quando a nave passa
            GameObject rastroObj = new GameObject("RastroCorteTexto");
            rastroObj.transform.SetParent(canvasObj.transform, false);
            rastroCorteImg = rastroObj.AddComponent<Image>();
            rastroCorteImg.color = new Color(0.4f, 0.95f, 1f, 0f);
            RectTransform rastroRt = rastroCorteImg.rectTransform;
            rastroRt.anchorMin = new Vector2(0.5f, 0.5f);
            rastroRt.anchorMax = new Vector2(0.5f, 0.5f);
            rastroRt.sizeDelta = new Vector2(2500f, 16f);
            rastroRt.localRotation = Quaternion.Euler(0f, 0f, 18f);

            // 6. A Nave do Chico (com o gatinho dentro da cúpula e fogo no propulsor)
            GameObject naveObj = new GameObject("NaveChico");
            naveObj.transform.SetParent(canvasObj.transform, false);
            naveImg = naveObj.AddComponent<RawImage>();
            naveImg.texture = texNaveSprite;
            naveRt = naveImg.rectTransform;
            naveRt.anchorMin = new Vector2(0.5f, 0.5f);
            naveRt.anchorMax = new Vector2(0.5f, 0.5f);
            naveRt.sizeDelta = new Vector2(560f, 400f);
            naveRt.anchoredPosition = new Vector2(1450f, 380f);

            // 7. Barra inferior de legenda
            barraLegendaObj = new GameObject("BarraLegenda");
            barraLegendaObj.transform.SetParent(canvasObj.transform, false);
            Image barraImg = barraLegendaObj.AddComponent<Image>();
            barraImg.color = new Color(0.02f, 0.02f, 0.06f, 0.86f);
            RectTransform barraRt = barraImg.rectTransform;
            barraRt.anchorMin = new Vector2(0f, 0f);
            barraRt.anchorMax = new Vector2(1f, 0.13f);
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

            // 8. Aviso para pular a abertura
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

            // 9. Flash de tela
            GameObject flashObj = new GameObject("FlashTela");
            flashObj.transform.SetParent(canvasObj.transform, false);
            flashTela = flashObj.AddComponent<Image>();
            flashTela.color = new Color(1f, 1f, 1f, 0f);
            RectTransform flashRt = flashTela.rectTransform;
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;
            flashRt.offsetMin = Vector2.zero;
            flashRt.offsetMax = Vector2.zero;
        }

        private RectTransform CriarBlocoCrawl(RectTransform parent, string nome, Font fonte, Vector2 pos, string conteudo, out Text txtComp)
        {
            GameObject go = new GameObject(nome);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1100f, 380f);
            rt.anchoredPosition = pos;

            txtComp = go.AddComponent<Text>();
            txtComp.font = fonte;
            txtComp.fontSize = 36;
            txtComp.lineSpacing = 1.32f;
            txtComp.alignment = TextAnchor.MiddleCenter;
            txtComp.color = new Color(1f, 0.86f, 0.22f, 1f);
            txtComp.text = conteudo;

            Shadow sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.95f);
            sh.effectDistance = new Vector2(3f, -3f);
            return rt;
        }

        private void AnimarEstrelas(float tempo, float velocidadeScrollX, float velocidadeScrollY)
        {
            for (int i = 0; i < estrelas.Count; i++)
            {
                Image st = estrelas[i];
                if (st == null) continue;
                RectTransform rt = st.rectTransform;
                Vector2 p = rt.anchoredPosition;
                float camada = (i % 3 + 1) * 0.45f;
                p.x += velocidadeScrollX * camada * Time.unscaledDeltaTime;
                p.y += velocidadeScrollY * camada * Time.unscaledDeltaTime;

                if (p.x > 960f) p.x -= 1920f;
                if (p.x < -960f) p.x += 1920f;
                if (p.y > 540f) p.y -= 1080f;
                if (p.y < -540f) p.y += 1080f;
                rt.anchoredPosition = p;

                Color c = st.color;
                c.a = 0.45f + 0.5f * Mathf.Abs(Mathf.Sin(tempo * 3.5f + i));
                st.color = c;
            }
        }

        private IEnumerator RotinaCutscene()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Cutscene);

            PlayerController player = GameManager.Instance != null ? GameManager.Instance.player : FindFirstObjectByType<PlayerController>();
            Vector3 posInicialPlayer = player != null ? player.transform.position : new Vector3(-1.5f, -0.95f, 0f);

            // =========================================================================
            // ATO 1: O Espaço Sideral e o Texto Estilo Star Wars Subindo
            // =========================================================================
            float duracaoCrawl = 4.8f;
            float t = 0f;
            while (t < duracaoCrawl && !pulouCutscene)
            {
                t += Time.unscaledDeltaTime;
                float p = t / duracaoCrawl;

                AnimarEstrelas(t, -12f, -8f);
                planetaRt.localRotation = Quaternion.Euler(0f, 0f, t * 2.5f);

                // Sobe o texto estilo Star Wars do fundo da tela até o centro do espaço
                float crawlY = Mathf.Lerp(-520f, 45f, p);
                float escalaCrawl = Mathf.Lerp(1.06f, 0.82f, p);
                crawlRootRt.anchoredPosition = new Vector2(0f, crawlY);
                crawlRootRt.localScale = new Vector3(escalaCrawl, escalaCrawl, 1f);

                yield return null;
            }

            // =========================================================================
            // ATO 2: A Nave do Chico Aparece e CORTA O TEXTO AO MEIO!
            // =========================================================================
            if (!pulouCutscene)
            {
                barraLegendaObj.SetActive(true);
                legendaText.text = "A nave desgovernada do Chico cortou o espaço em alta velocidade!";
                AudioManager.Play(Sfx.Alert, 0.9f);
                AudioManager.Play(Sfx.Shoot, 1.0f);

                t = 0f;
                float duracaoCorte = 1.35f;
                bool jaCortouTexto = false;

                // A nave aponta para a esquerda/baixo, então vem da direita/topo para a esquerda/baixo
                Vector2 posNaveIni = new Vector2(1380f, 360f);
                Vector2 posNaveFim = new Vector2(-1380f, -320f);

                while (t < duracaoCorte && !pulouCutscene)
                {
                    t += Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(t / duracaoCorte);

                    AnimarEstrelas(t + 5f, -45f, -20f);

                    // Movimento rápido da nave atravessando a tela
                    naveRt.anchoredPosition = Vector2.Lerp(posNaveIni, posNaveFim, p);
                    naveRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 55f) * 5f);

                    // Feixe luminoso que rasga o texto exatamente onde a nave passa
                    float intensidadeFeixe = Mathf.Sin(p * Mathf.PI);
                    rastroCorteImg.color = new Color(0.35f, 0.95f, 1f, intensidadeFeixe * 0.95f);
                    rastroCorteImg.rectTransform.sizeDelta = new Vector2(2500f, 8f + intensidadeFeixe * 42f);

                    // Quando a nave atinge o meio do texto (p >= 0.36f), o texto é CORTADO ao meio!
                    if (p >= 0.36f)
                    {
                        if (!jaCortouTexto)
                        {
                            jaCortouTexto = true;
                            AudioManager.Play(Sfx.BossSlam, 0.85f);
                            flashTela.color = new Color(0.6f, 0.95f, 1f, 0.7f);
                        }

                        float pCorte = (p - 0.36f) / 0.64f;
                        flashTela.color = new Color(0.6f, 0.95f, 1f, Mathf.Max(0f, 0.7f - pCorte * 1.8f));

                        // Metade de cima do texto voa para cima/esquerda e gira
                        crawlTopoRt.anchoredPosition = new Vector2(-pCorte * 420f, 190f + pCorte * 340f);
                        crawlTopoRt.localRotation = Quaternion.Euler(0f, 0f, pCorte * 22f);

                        // Metade de baixo do texto cai para baixo/direita e gira
                        crawlBaseRt.anchoredPosition = new Vector2(pCorte * 420f, -190f - pCorte * 360f);
                        crawlBaseRt.localRotation = Quaternion.Euler(0f, 0f, -pCorte * 24f);

                        float alphaTxt = Mathf.Clamp01(1f - pCorte * 1.25f);
                        crawlTopoText.color = new Color(1f, 0.86f, 0.22f, alphaTxt);
                        crawlBaseText.color = new Color(1f, 0.86f, 0.22f, alphaTxt);
                    }

                    yield return null;
                }

                crawlRootRt.gameObject.SetActive(false);
                rastroCorteImg.gameObject.SetActive(false);
            }

            // =========================================================================
            // ATO 3: O Planeta Bioluminescente cresce na tela e a nave cai nele!
            // =========================================================================
            if (!pulouCutscene)
            {
                legendaText.text = "Atraída pela gravidade, a nave despencou rumo ao Planeta Bioluminescente!";
                AudioManager.Play(Sfx.BossSpit, 0.9f);

                // Vira a nave para a direita (em direção ao planeta)
                naveRt.localScale = new Vector3(-1f, 1f, 1f);
                planetaImg.color = Color.white;

                t = 0f;
                float duracaoPlaneta = 2.8f;
                float proxAlarme = 0.4f;

                while (t < duracaoPlaneta && !pulouCutscene)
                {
                    t += Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(t / duracaoPlaneta);
                    float curvaZoom = p * p;

                    if (t >= proxAlarme)
                    {
                        proxAlarme += 0.75f;
                        AudioManager.Play(Sfx.Alert, 0.7f);
                    }

                    // As estrelas sobem rápido dando sensação da câmera mergulhando para o planeta
                    AnimarEstrelas(t + 10f, -80f, 220f);

                    // O Planeta se move para o centro e cresce até tomar conta da visão!
                    Vector2 posPlaneta = Vector2.Lerp(new Vector2(560f, -240f), new Vector2(120f, -40f), p);
                    float tamPlaneta = Mathf.Lerp(260f, 1550f, curvaZoom);
                    planetaRt.anchoredPosition = posPlaneta;
                    planetaRt.sizeDelta = new Vector2(tamPlaneta, tamPlaneta);
                    planetaRt.localRotation = Quaternion.Euler(0f, 0f, 15f + t * 8f);

                    // A nave do Chico faz uma curva mergulhando para dentro do planeta e diminuindo em perspectiva
                    Vector2 posNave = Vector2.Lerp(new Vector2(-780f, 280f), new Vector2(110f, -20f), p);
                    posNave.y += Mathf.Sin(p * Mathf.PI) * 160f + Mathf.Sin(t * 45f) * 10f;
                    naveRt.anchoredPosition = posNave;

                    float escalaNave = Mathf.Lerp(0.95f, 0.18f, p);
                    naveRt.localScale = new Vector3(-escalaNave, escalaNave, 1f);
                    naveRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(12f, -38f, p) + Mathf.Sin(t * 50f) * 6f);

                    if (p > 0.78f)
                    {
                        flashTela.color = new Color(0.5f, 1f, 0.85f, (p - 0.78f) / 0.22f);
                    }

                    yield return null;
                }
            }

            // =========================================================================
            // ATO 4: Entrada rasante nos cogumelos do planeta + Queda real na fase!
            // =========================================================================
            if (!pulouCutscene && texNaveCaindo != null)
            {
                planetaRt.gameObject.SetActive(false);
                naveRt.gameObject.SetActive(false);

                // Mostra a nave entrando na atmosfera de cogumelos gigantes do pântano
                GameObject closeQuedaObj = new GameObject("CloseQuedaPantano");
                closeQuedaObj.transform.SetParent(canvasObj.transform, false);
                closeQuedaObj.transform.SetSiblingIndex(2);
                RawImage closeImg = closeQuedaObj.AddComponent<RawImage>();
                closeImg.texture = texNaveCaindo;
                RectTransform closeRt = closeImg.rectTransform;
                closeRt.anchorMin = Vector2.zero;
                closeRt.anchorMax = Vector2.one;
                closeRt.offsetMin = new Vector2(-40f, -40f);
                closeRt.offsetMax = new Vector2(40f, 40f);

                legendaText.text = "Segure firme, Chico! Impacto iminente no pântano!";

                t = 0f;
                float duracaoAtmosfera = 1.6f;
                while (t < duracaoAtmosfera && !pulouCutscene)
                {
                    t += Time.unscaledDeltaTime;
                    float p = t / duracaoAtmosfera;
                    flashTela.color = new Color(0.5f, 1f, 0.85f, Mathf.Max(0f, 1f - t * 3.5f));

                    float esc = 1.02f + p * 0.08f;
                    closeRt.localScale = new Vector3(esc, esc, 1f);
                    closeRt.anchoredPosition = new Vector2(
                        Mathf.Lerp(25f, -25f, p) + Mathf.Sin(t * 50f) * 12f,
                        Mathf.Lerp(20f, -25f, p) + Mathf.Cos(t * 44f) * 12f
                    );
                    yield return null;
                }
            }

            // Fecha a tela de vídeo e mostra a nave e o Chico caindo no chão da fase!
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
