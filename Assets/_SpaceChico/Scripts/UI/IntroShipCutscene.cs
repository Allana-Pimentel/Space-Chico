using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    // Cutscene inicial: texto inclinado estilo Star Wars + queda da nave do Chico na fase
    public class IntroShipCutscene : MonoBehaviour
    {
        private static bool jaMostrouIntro = false;

        // Inclina o texto em perspectiva trapezoidal estilo Star Wars
        private class PerspectivaStarWarsEffect : BaseMeshEffect
        {
            public float yOffset;

            public override void ModifyMesh(VertexHelper vh)
            {
                if (!IsActive()) return;

                int count = vh.currentVertCount;
                UIVertex v = new UIVertex();
                for (int i = 0; i < count; i++)
                {
                    vh.PopulateUIVertex(ref v, i);

                    float yPlano = yOffset + v.position.y;
                    float u = Mathf.Clamp((yPlano + 520f) / 1040f, -0.25f, 1.25f);
                    float escalaX = 1.32f / (1f + Mathf.Max(-0.30f, u) * 2.15f);
                    float screenY = -490f + 860f * ((u * 1.85f) / (1f + Mathf.Max(0f, u) * 0.85f));

                    v.position.x *= escalaX;
                    v.position.y = screenY - yOffset;

                    float alphaTop = Mathf.Clamp01(1f - (u - 0.78f) / 0.22f);
                    float alphaBot = Mathf.Clamp01((u + 0.05f) / 0.10f);
                    Color32 c = v.color;
                    c.a = (byte)Mathf.RoundToInt(c.a * alphaTop * alphaBot);
                    v.color = c;

                    vh.SetUIVertex(v, i);
                }
            }
        }

        private GameObject canvasObj;
        private Image fundoImg;
        private RectTransform logoRt;
        private Text logoText;
        private RectTransform crawlRt;
        private Text crawlText;
        private PerspectivaStarWarsEffect crawlPersp;
        private RectTransform naveUiRt;
        private RectTransform planetaUiRt;

        private Texture2D texNave;
        private Texture2D texPlaneta;
        private bool pulou = false;

        public static bool DeveTocarIntro() => !jaMostrouIntro;

        public void IniciarCutscene()
        {
            jaMostrouIntro = true;
            texNave = Resources.Load<Texture2D>("Intro/intro_ship_sprite");
            texPlaneta = Resources.Load<Texture2D>("Intro/intro_planet_sprite");

            CriarUI();
            StartCoroutine(RotinaCutscene());
        }

        private void Update()
        {
            if (canvasObj != null && !pulou)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
                    pulou = true;
            }
        }

        private void CriarUI()
        {
            canvasObj = new GameObject("Canvas_Intro");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            Font fonte = Resources.Load<Font>("Fonts/RussoOne-Regular");
            if (fonte == null) fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Fundo espacial
            var bgObj = new GameObject("Fundo", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            fundoImg = bgObj.GetComponent<Image>();
            fundoImg.color = new Color(0.02f, 0.01f, 0.06f, 1f);
            var bgRt = fundoImg.rectTransform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            // Planeta ao fundo
            var planObj = new GameObject("Planeta", typeof(RectTransform), typeof(RawImage));
            planObj.transform.SetParent(canvasObj.transform, false);
            planObj.GetComponent<RawImage>().texture = texPlaneta;
            planetaUiRt = planObj.GetComponent<RectTransform>();
            planetaUiRt.sizeDelta = new Vector2(260f, 260f);
            planetaUiRt.anchoredPosition = new Vector2(560f, -240f);

            // Logo SPACE CHICO
            var logoObj = new GameObject("Logo", typeof(RectTransform), typeof(Text));
            logoObj.transform.SetParent(canvasObj.transform, false);
            logoRt = logoObj.GetComponent<RectTransform>();
            logoRt.sizeDelta = new Vector2(1400f, 240f);
            logoText = logoObj.GetComponent<Text>();
            logoText.font = fonte;
            logoText.fontSize = 100;
            logoText.alignment = TextAnchor.MiddleCenter;
            logoText.color = new Color(1f, 0.88f, 0.2f, 1f);
            logoText.text = "SPACE CHICO";

            // Texto Star Wars inclinado
            var txtObj = new GameObject("TextoStarWars", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(canvasObj.transform, false);
            crawlRt = txtObj.GetComponent<RectTransform>();
            crawlRt.sizeDelta = new Vector2(1260f, 900f);
            crawlRt.anchoredPosition = new Vector2(0f, -680f);

            crawlText = txtObj.GetComponent<Text>();
            crawlText.font = fonte;
            crawlText.fontSize = 38;
            crawlText.lineSpacing = 1.32f;
            crawlText.alignment = TextAnchor.UpperCenter;
            crawlText.color = new Color(1f, 0.85f, 0.16f, 1f);
            crawlText.text =
                "EPISÓDIO I\nO PÂNTANO BIOLUMINESCENTE\n\n" +
                "O gatinho astronauta CHICO cruzava a galáxia\n" +
                "a bordo de sua nave exploradora.\n\n" +
                "De repente, uma pane no propulsor quântico\n" +
                "deixou a nave desgovernada pelo espaço!\n\n" +
                "Atraída pela gravidade de um planeta selvagem,\n" +
                "a nave agora cai rumo a um pouso forçado...";

            crawlPersp = txtObj.AddComponent<PerspectivaStarWarsEffect>();

            // Nave na UI
            var naveObj = new GameObject("NaveUI", typeof(RectTransform), typeof(RawImage));
            naveObj.transform.SetParent(canvasObj.transform, false);
            naveObj.GetComponent<RawImage>().texture = texNave;
            naveUiRt = naveObj.GetComponent<RectTransform>();
            naveUiRt.sizeDelta = new Vector2(520f, 370f);
            naveUiRt.anchoredPosition = new Vector2(1450f, 400f);

            // Dica para pular
            var skipObj = new GameObject("AvisoPular", typeof(RectTransform), typeof(Text));
            skipObj.transform.SetParent(canvasObj.transform, false);
            var skipTxt = skipObj.GetComponent<Text>();
            skipTxt.font = fonte;
            skipTxt.fontSize = 22;
            skipTxt.alignment = TextAnchor.UpperRight;
            skipTxt.color = new Color(0.85f, 0.95f, 1f, 0.85f);
            skipTxt.text = "[ESPAÇO] Pular Abertura";
            var skipRt = skipTxt.rectTransform;
            skipRt.anchorMin = new Vector2(1f, 1f);
            skipRt.anchorMax = new Vector2(1f, 1f);
            skipRt.pivot = new Vector2(1f, 1f);
            skipRt.anchoredPosition = new Vector2(-36f, -28f);
            skipRt.sizeDelta = new Vector2(450f, 50f);
        }

        private IEnumerator RotinaCutscene()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Cutscene);

            PlayerController player = GameManager.Instance != null ? GameManager.Instance.player : FindAnyObjectByType<PlayerController>();
            Vector3 posInicialPlayer = player != null ? player.transform.position : new Vector3(-1.5f, -0.95f, 0f);
            GameObject propShip = GameObject.Find("Prop_CrashedShip");
            Vector3 posFinalShip = propShip != null ? propShip.transform.position : new Vector3(-5.2f, 2.15f, 0f);

            // 1. Logo diminuindo + Texto Star Wars subindo devagar
            AudioManager.Play(Sfx.PowerUp, 0.85f);
            float t = 0f;
            float duracaoCrawl = 11.2f;
            float yCrawl = -680f;

            while (t < duracaoCrawl && !pulou)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;

                float pLogo = Mathf.Clamp01(t / 3.0f);
                float esc = Mathf.Lerp(2.2f, 0.15f, pLogo);
                logoRt.localScale = new Vector3(esc, esc, 1f);
                logoRt.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 430f, pLogo));
                logoText.color = new Color(1f, 0.88f, 0.2f, Mathf.Clamp01(1f - (pLogo - 0.65f) / 0.35f));

                if (t > 0.8f)
                {
                    yCrawl += dt * 88f;
                    crawlRt.anchoredPosition = new Vector2(0f, yCrawl);
                    crawlPersp.yOffset = yCrawl;
                    crawlText.SetVerticesDirty();
                }

                yield return null;
            }

            // 2. Nave cruza a tela rumo ao planeta
            t = 0f;
            AudioManager.Play(Sfx.Alert, 0.85f);
            while (t < 2.2f && !pulou)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / 2.2f);

                naveUiRt.anchoredPosition = Vector2.Lerp(new Vector2(1300f, 360f), new Vector2(360f, -160f), p);
                float escNave = Mathf.Lerp(1.3f, 0.35f, p);
                naveUiRt.localScale = new Vector3(escNave, escNave, 1f);

                float tamPlaneta = Mathf.Lerp(260f, 1500f, p * p);
                planetaUiRt.sizeDelta = new Vector2(tamPlaneta, tamPlaneta);
                planetaUiRt.anchoredPosition = Vector2.Lerp(new Vector2(560f, -240f), Vector2.zero, p);

                if (p > 0.6f)
                {
                    float fade = 1f - (p - 0.6f) / 0.4f;
                    fundoImg.color = new Color(0.02f, 0.01f, 0.06f, fade);
                }
                yield return null;
            }

            if (canvasObj != null) Destroy(canvasObj);

            // 3. Pouso forçado da nave no cenário e ejeção do Chico
            if (player != null && !pulou)
            {
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.simulated = false;

                AudioManager.Play(Sfx.BossSlam, 1.0f);
                CameraFollow.Shake(0.35f, 0.3f);
                VFX.Burst(posFinalShip + Vector3.up * 0.6f, new Color(1f, 0.55f, 0.15f), 20, 5f, 0.3f, 0.5f);

                Vector3 origemEjecao = posFinalShip + new Vector3(0.4f, 0.65f, 0f);
                float ejecaoT = 0f;
                while (ejecaoT < 0.7f && !pulou)
                {
                    ejecaoT += Time.deltaTime;
                    float p = Mathf.Clamp01(ejecaoT / 0.7f);
                    Vector3 posBase = Vector3.Lerp(origemEjecao, posInicialPlayer, p);
                    player.transform.position = posBase + Vector3.up * (Mathf.Sin(p * Mathf.PI) * 3.2f);
                    yield return null;
                }
            }

            if (propShip != null)
            {
                propShip.transform.localScale = new Vector3(2.35f, 2.35f, 1f);
                propShip.transform.position = posFinalShip;
                propShip.SetActive(true);
            }

            if (player != null)
            {
                player.transform.position = posInicialPlayer;
                player.transform.rotation = Quaternion.identity;
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.simulated = true;
                    rb.linearVelocity = Vector2.zero;
                }
            }

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Playing);

            Destroy(this);
        }
    }
}
