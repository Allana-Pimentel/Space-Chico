using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    // Cutscene inicial: texto inclinado estilo Star Wars + queda da nave do Chico
    public class IntroShipCutscene : MonoBehaviour
    {
        private static bool jaMostrouIntro = false;

        private class PerspectivaStarWarsEffect : BaseMeshEffect
        {
            public float yOffset;
            public override void ModifyMesh(VertexHelper vh)
            {
                if (!IsActive()) return;
                UIVertex v = new UIVertex();
                for (int i = 0; i < vh.currentVertCount; i++)
                {
                    vh.PopulateUIVertex(ref v, i);
                    float yPlano = yOffset + v.position.y;
                    float u = Mathf.Clamp((yPlano + 520f) / 1040f, -0.25f, 1.25f);
                    v.position.x *= 1.32f / (1f + Mathf.Max(-0.30f, u) * 2.15f);
                    v.position.y = -490f + 860f * ((u * 1.85f) / (1f + Mathf.Max(0f, u) * 0.85f)) - yOffset;
                    Color32 c = v.color;
                    c.a = (byte)Mathf.RoundToInt(c.a * Mathf.Clamp01(1f - (u - 0.78f) / 0.22f) * Mathf.Clamp01((u + 0.05f) / 0.10f));
                    v.color = c;
                    vh.SetUIVertex(v, i);
                }
            }
        }

        private GameObject canvasObj;
        private Image fundoImg;
        private RectTransform logoRt, crawlRt, naveUiRt, planetaUiRt;
        private Text logoText, crawlText;
        private PerspectivaStarWarsEffect crawlPersp;
        private bool pulou;

        public static bool DeveTocarIntro() => !jaMostrouIntro;

        public void IniciarCutscene()
        {
            jaMostrouIntro = true;
            CriarUI();
            StartCoroutine(RotinaCutscene());
        }

        void Update()
        {
            if (canvasObj != null && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape)))
                pulou = true;
        }

        void CriarUI()
        {
            canvasObj = new GameObject("Canvas_Intro");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            Font fonte = Resources.Load<Font>("Fonts/RussoOne-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var bgObj = new GameObject("Fundo", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            fundoImg = bgObj.GetComponent<Image>();
            fundoImg.color = new Color(0.02f, 0.01f, 0.06f, 1f);
            fundoImg.rectTransform.anchorMin = Vector2.zero;
            fundoImg.rectTransform.anchorMax = Vector2.one;
            fundoImg.rectTransform.sizeDelta = Vector2.zero;

            var planObj = new GameObject("Planeta", typeof(RectTransform), typeof(RawImage));
            planObj.transform.SetParent(canvasObj.transform, false);
            planObj.GetComponent<RawImage>().texture = Resources.Load<Texture2D>("Intro/intro_planet_sprite");
            planetaUiRt = planObj.GetComponent<RectTransform>();
            planetaUiRt.sizeDelta = new Vector2(260f, 260f);
            planetaUiRt.anchoredPosition = new Vector2(560f, -240f);

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
            crawlText.text = "EPISÓDIO I\nO PÂNTANO BIOLUMINESCENTE\n\nO gatinho astronauta CHICO cruzava a galáxia\na bordo de sua nave exploradora.\n\nDe repente, uma pane no propulsor quântico\ndeixou a nave desgovernada pelo espaço!\n\nAtraída pela gravidade de um planeta selvagem,\na nave agora cai rumo a um pouso forçado...";
            crawlPersp = txtObj.AddComponent<PerspectivaStarWarsEffect>();

            var naveObj = new GameObject("NaveUI", typeof(RectTransform), typeof(RawImage));
            naveObj.transform.SetParent(canvasObj.transform, false);
            naveObj.GetComponent<RawImage>().texture = Resources.Load<Texture2D>("Intro/intro_ship_sprite");
            naveUiRt = naveObj.GetComponent<RectTransform>();
            naveUiRt.sizeDelta = new Vector2(520f, 370f);
            naveUiRt.anchoredPosition = new Vector2(1450f, 400f);
        }

        IEnumerator RotinaCutscene()
        {
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Cutscene);
            PlayerController player = GameManager.Instance != null ? GameManager.Instance.player : FindAnyObjectByType<PlayerController>();
            Vector3 posPlayer = player != null ? player.transform.position : new Vector3(-1.5f, -0.95f, 0f);
            GameObject propShip = GameObject.Find("Prop_CrashedShip");
            Vector3 posShip = propShip != null ? propShip.transform.position : new Vector3(-5.2f, 2.15f, 0f);

            AudioManager.Play(Sfx.PowerUp, 0.85f);
            float t = 0f, yCrawl = -680f;
            while (t < 11.2f && !pulou)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                float pLogo = Mathf.Clamp01(t / 3.0f);
                logoRt.localScale = Vector3.one * Mathf.Lerp(2.2f, 0.15f, pLogo);
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

            t = 0f;
            AudioManager.Play(Sfx.Alert, 0.85f);
            while (t < 2.2f && !pulou)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / 2.2f);
                naveUiRt.anchoredPosition = Vector2.Lerp(new Vector2(1300f, 360f), new Vector2(360f, -160f), p);
                naveUiRt.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.35f, p);
                planetaUiRt.sizeDelta = Vector2.one * Mathf.Lerp(260f, 1500f, p * p);
                planetaUiRt.anchoredPosition = Vector2.Lerp(new Vector2(560f, -240f), Vector2.zero, p);
                if (p > 0.6f) fundoImg.color = new Color(0.02f, 0.01f, 0.06f, 1f - (p - 0.6f) / 0.4f);
                yield return null;
            }

            if (canvasObj != null) Destroy(canvasObj);

            if (player != null && !pulou)
            {
                AudioManager.Play(Sfx.BossSlam, 1.0f);
                CameraFollow.Shake(0.35f, 0.3f);
                Vector3 origem = posShip + new Vector3(0.4f, 0.65f, 0f);
                for (float e = 0f; e < 0.7f && !pulou; e += Time.deltaTime)
                {
                    float p = e / 0.7f;
                    player.transform.position = Vector3.Lerp(origem, posPlayer, p) + Vector3.up * (Mathf.Sin(p * Mathf.PI) * 3.2f);
                    yield return null;
                }
            }

            if (player != null) player.transform.position = posPlayer;
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
            Destroy(this);
        }
    }
}
