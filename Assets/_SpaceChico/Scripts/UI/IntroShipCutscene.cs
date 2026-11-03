using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceChico
{
    // Abertura cinematográfica completa com as 3 melhorias:
    // 1. Logo "SPACE CHICO" gigante encolhendo + parágrafos independentes atropelados um a um pela nave com fumaça
    // 2. Transição contínua de câmera: Espaço -> Planeta Bioluminescente -> Nuvens da atmosfera abrindo para o céu da fase
    // 3. Queda da nave no cenário da fase, batida no chão e ejeção do Chico em parábola!
    public class IntroShipCutscene : MonoBehaviour
    {
        private static bool jaMostrouIntro = false;

        private class ParagrafoCrawl
        {
            public RectTransform rt;
            public Text txt;
            public PerspectivaStarWarsEffect efeitoPerspectiva;
            public Vector2 posBase;
            public bool foiAtropelado;
            public Vector2 velocidadeVoando;
            public float velocidadeGiro;
            public float anguloAtual;
        }

        // Deforma os vértices do texto em um trapézio 3D real inclinado para o fundo do espaço (estilo Star Wars clássico!)
        private class PerspectivaStarWarsEffect : BaseMeshEffect
        {
            public float yCentroParagrafo;
            public bool ativo = true;

            public static float CalcularEscalaPerspectiva(float yPlano)
            {
                float u = Mathf.Clamp((yPlano + 520f) / 1040f, -0.25f, 1.25f);
                return 1.32f / (1f + Mathf.Max(-0.30f, u) * 2.15f);
            }

            public static float CalcularScreenY(float yPlano)
            {
                float u = Mathf.Clamp((yPlano + 520f) / 1040f, -0.25f, 1.25f);
                return -490f + 860f * ((u * 1.85f) / (1f + Mathf.Max(0f, u) * 0.85f));
            }

            public override void ModifyMesh(VertexHelper vh)
            {
                if (!IsActive() || !ativo) return;

                int count = vh.currentVertCount;
                UIVertex v = new UIVertex();
                for (int i = 0; i < count; i++)
                {
                    vh.PopulateUIVertex(ref v, i);

                    float yPlano = yCentroParagrafo + v.position.y;
                    float u = (yPlano + 520f) / 1040f;
                    float persp = CalcularEscalaPerspectiva(yPlano);
                    float screenY = CalcularScreenY(yPlano);

                    v.position.x *= persp;
                    v.position.y = screenY - yCentroParagrafo;

                    float alphaTop = Mathf.Clamp01(1f - (u - 0.78f) / 0.22f);
                    float alphaBot = Mathf.Clamp01((u + 0.05f) / 0.10f);
                    Color32 c = v.color;
                    c.a = (byte)Mathf.RoundToInt(c.a * alphaTop * alphaBot);
                    v.color = c;

                    vh.SetUIVertex(v, i);
                }
            }
        }

        private class ParticulaFumacaUI
        {
            public RectTransform rt;
            public Image img;
            public Vector2 velocidade;
            public float tempoVida;
            public float duracao;
            public float escalaInicial;
        }

        private GameObject canvasObj;
        private Image fundoEspacoImg;
        private RectTransform estrelasContainer;
        private List<Image> estrelas = new List<Image>();

        private RectTransform logoRt;
        private Text logoText;
        private RectTransform crawlRootRt;
        private List<ParagrafoCrawl> paragrafos = new List<ParagrafoCrawl>();
        private List<ParticulaFumacaUI> fumacasUI = new List<ParticulaFumacaUI>();

        private RectTransform planetaRt;
        private RawImage planetaImg;

        private RectTransform naveRt;
        private RawImage naveImg;
        private Image rastroCorteImg;

        private RectTransform nuvemEsqRt;
        private RectTransform nuvemDirRt;
        private Image nuvemEsqImg;
        private Image nuvemDirImg;

        private Image flashTela;
        private Text legendaText;
        private GameObject barraLegendaObj;

        private Texture2D texNaveSprite;
        private Texture2D texPlanetaSprite;
        private Sprite spriteCirculo;
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
            spriteCirculo = CriarSpriteCirculoSuave();

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

        private Sprite CriarSpriteCirculoSuave()
        {
            int tam = 64;
            Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
            float centro = (tam - 1) * 0.5f;
            for (int y = 0; y < tam; y++)
            {
                for (int x = 0; x < tam; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centro, centro)) / centro;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f));
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

            // 1. Fundo do espaço profundo (fica transparente no final para revelar o céu da fase sem corte!)
            GameObject bgObj = new GameObject("FundoEspaco");
            bgObj.transform.SetParent(canvasObj.transform, false);
            fundoEspacoImg = bgObj.AddComponent<Image>();
            fundoEspacoImg.color = new Color(0.02f, 0.01f, 0.05f, 1f);
            RectTransform bgRt = fundoEspacoImg.rectTransform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // 2. Campo de estrelas cintilantes
            GameObject starsObj = new GameObject("CampoEstrelas");
            starsObj.transform.SetParent(canvasObj.transform, false);
            estrelasContainer = starsObj.AddComponent<RectTransform>();
            estrelasContainer.anchorMin = Vector2.zero;
            estrelasContainer.anchorMax = Vector2.one;
            estrelasContainer.offsetMin = Vector2.zero;
            estrelasContainer.offsetMax = Vector2.zero;

            Random.InitState(42);
            for (int i = 0; i < 80; i++)
            {
                GameObject st = new GameObject("Star_" + i);
                st.transform.SetParent(estrelasContainer, false);
                Image stImg = st.AddComponent<Image>();
                float br = Random.Range(0.55f, 1f);
                stImg.color = (i % 4 == 0)
                    ? new Color(0.45f, 0.95f, 1f, br)
                    : (i % 6 == 0 ? new Color(0.85f, 0.55f, 1f, br) : new Color(1f, 1f, 1f, br));

                RectTransform stRt = stImg.rectTransform;
                float tam = Random.Range(2.5f, 6.5f);
                stRt.sizeDelta = new Vector2(tam, tam);
                stRt.anchoredPosition = new Vector2(Random.Range(-950f, 950f), Random.Range(-530f, 530f));
                estrelas.Add(stImg);
            }

            // 3. Planeta Bioluminescente no espaço
            GameObject planObj = new GameObject("PlanetaBioluminescente");
            planObj.transform.SetParent(canvasObj.transform, false);
            planetaImg = planObj.AddComponent<RawImage>();
            planetaImg.texture = texPlanetaSprite;
            planetaRt = planetaImg.rectTransform;
            planetaRt.anchorMin = new Vector2(0.5f, 0.5f);
            planetaRt.anchorMax = new Vector2(0.5f, 0.5f);
            planetaRt.sizeDelta = new Vector2(240f, 240f);
            planetaRt.anchoredPosition = new Vector2(580f, -260f);
            planetaImg.color = new Color(1f, 1f, 1f, 0.5f);

            Font fonteTitulo = Resources.Load<Font>("Fonts/RussoOne-Regular");
            if (fonteTitulo == null) fonteTitulo = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 4. Logo gigante "SPACE CHICO" estilo abertura clássica de Star Wars
            GameObject logoObj = new GameObject("LogoSpaceChico");
            logoObj.transform.SetParent(canvasObj.transform, false);
            logoRt = logoObj.AddComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0.5f, 0.5f);
            logoRt.anchorMax = new Vector2(0.5f, 0.5f);
            logoRt.sizeDelta = new Vector2(1400f, 260f);
            logoRt.anchoredPosition = new Vector2(0f, 40f);

            logoText = logoObj.AddComponent<Text>();
            logoText.font = fonteTitulo;
            logoText.fontSize = 105;
            logoText.alignment = TextAnchor.MiddleCenter;
            logoText.color = new Color(1f, 0.88f, 0.2f, 1f);
            logoText.text = "SPACE CHICO";

            Shadow logoShadow = logoObj.AddComponent<Shadow>();
            logoShadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            logoShadow.effectDistance = new Vector2(5f, -5f);

            // 5. Parágrafos independentes do texto Star Wars (com inclinação trapezoidal 3D real!)
            GameObject crawlRoot = new GameObject("StarWarsCrawlRoot");
            crawlRoot.transform.SetParent(canvasObj.transform, false);
            crawlRootRt = crawlRoot.AddComponent<RectTransform>();
            crawlRootRt.anchorMin = new Vector2(0.5f, 0.5f);
            crawlRootRt.anchorMax = new Vector2(0.5f, 0.5f);
            crawlRootRt.sizeDelta = new Vector2(1400f, 1080f);
            crawlRootRt.anchoredPosition = Vector2.zero;
            crawlRootRt.localRotation = Quaternion.identity;

            string[] textosHistoria = new string[]
            {
                "EPISÓDIO I",
                "O PÂNTANO BIOLUMINESCENTE",
                "É uma era de grandes viagens espaciais. O destemido\ngatinho astronauta CHICO cruzava a galáxia feliz\na bordo de sua moderna nave exploradora.",
                "De repente, uma pane misteriosa no propulsor quântico\nfez os motores falharem, deixando a espaçonave\ntotalmente desgovernada pelo espaço profundo!",
                "Atraída pela gravidade de um planeta selvagem e brilhante,\na nave do Chico agora corta as estrelas em chamas\nrumo a um pouso forçado na selva..."
            };

            float[] alturasY = new float[] { -380f, -475f, -640f, -855f, -1070f };
            int[] tamanhosFonte = new int[] { 38, 48, 36, 36, 36 };
            for (int i = 0; i < textosHistoria.Length; i++)
            {
                AdicionarParagrafoCrawl(fonteTitulo, textosHistoria[i], new Vector2(0f, alturasY[i]), tamanhosFonte[i]);
            }

            // 6. Feixe luminoso que corta a tela junto com a nave
            GameObject rastroObj = new GameObject("RastroCorteTexto");
            rastroObj.transform.SetParent(canvasObj.transform, false);
            rastroCorteImg = rastroObj.AddComponent<Image>();
            rastroCorteImg.color = new Color(0.4f, 0.95f, 1f, 0f);
            RectTransform rastroRt = rastroCorteImg.rectTransform;
            rastroRt.anchorMin = new Vector2(0.5f, 0.5f);
            rastroRt.anchorMax = new Vector2(0.5f, 0.5f);
            rastroRt.sizeDelta = new Vector2(2600f, 16f);
            rastroRt.localRotation = Quaternion.Euler(0f, 0f, 18f);

            // 7. A Nave do Chico (com o gatinho dentro da cúpula)
            GameObject naveObj = new GameObject("NaveChico");
            naveObj.transform.SetParent(canvasObj.transform, false);
            naveImg = naveObj.AddComponent<RawImage>();
            naveImg.texture = texNaveSprite;
            naveRt = naveImg.rectTransform;
            naveRt.anchorMin = new Vector2(0.5f, 0.5f);
            naveRt.anchorMax = new Vector2(0.5f, 0.5f);
            naveRt.sizeDelta = new Vector2(540f, 385f);
            naveRt.anchoredPosition = new Vector2(1500f, 420f);

            // 8. Camada de nuvens da atmosfera do planeta (para transição sem corte seco!)
            nuvemEsqRt = CriarNuvemAtmosfera("NuvemAtmosferaEsq", new Vector2(-1200f, -900f), new Color(0.18f, 0.08f, 0.32f, 0f), out nuvemEsqImg);
            nuvemDirRt = CriarNuvemAtmosfera("NuvemAtmosferaDir", new Vector2(1200f, -900f), new Color(0.12f, 0.35f, 0.38f, 0f), out nuvemDirImg);

            // 9. Barra inferior de legenda
            barraLegendaObj = new GameObject("BarraLegenda");
            barraLegendaObj.transform.SetParent(canvasObj.transform, false);
            Image barraImg = barraLegendaObj.AddComponent<Image>();
            barraImg.color = new Color(0.02f, 0.02f, 0.06f, 0.86f);
            RectTransform barraRt = barraImg.rectTransform;
            barraRt.anchorMin = new Vector2(0f, 0f);
            barraRt.anchorMax = new Vector2(1f, 0.12f);
            barraRt.offsetMin = Vector2.zero;
            barraRt.offsetMax = Vector2.zero;

            GameObject legObj = new GameObject("TextoLegenda");
            legObj.transform.SetParent(barraLegendaObj.transform, false);
            legendaText = legObj.AddComponent<Text>();
            legendaText.font = fonteTitulo;
            legendaText.fontSize = 30;
            legendaText.alignment = TextAnchor.MiddleCenter;
            legendaText.color = new Color(0.95f, 0.98f, 1f, 1f);
            RectTransform legRt = legendaText.rectTransform;
            legRt.anchorMin = Vector2.zero;
            legRt.anchorMax = Vector2.one;
            legRt.offsetMin = new Vector2(50f, 6f);
            legRt.offsetMax = new Vector2(-50f, -6f);
            barraLegendaObj.SetActive(false);

            // 10. Aviso para pular abertura
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

            // 11. Flash de tela
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

        private void AdicionarParagrafoCrawl(Font fonte, string texto, Vector2 pos, int tamanhoFonte)
        {
            GameObject go = new GameObject("ParagrafoCrawl_" + paragrafos.Count);
            go.transform.SetParent(crawlRootRt, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1280f, 180f);
            rt.anchoredPosition = pos;

            Text txt = go.AddComponent<Text>();
            txt.font = fonte;
            txt.fontSize = tamanhoFonte;
            txt.lineSpacing = 1.28f;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1f, 0.85f, 0.16f, 1f);
            txt.text = texto;

            Shadow sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.95f);
            sh.effectDistance = new Vector2(3f, -3f);

            var persp = go.AddComponent<PerspectivaStarWarsEffect>();
            persp.yCentroParagrafo = pos.y;
            persp.ativo = true;

            paragrafos.Add(new ParagrafoCrawl
            {
                rt = rt,
                txt = txt,
                efeitoPerspectiva = persp,
                posBase = pos,
                foiAtropelado = false
            });
        }

        private RectTransform CriarNuvemAtmosfera(string nome, Vector2 pos, Color cor, out Image img)
        {
            GameObject go = new GameObject(nome);
            go.transform.SetParent(canvasObj.transform, false);
            img = go.AddComponent<Image>();
            img.sprite = spriteCirculo;
            img.color = cor;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(2200f, 1500f);
            rt.anchoredPosition = pos;
            return rt;
        }

        private void EmitirFumacaNaveUI(Vector2 posNave, Color corFumaca, float escala = 1f)
        {
            if (canvasObj == null) return;
            GameObject fObj = new GameObject("FumacaNave");
            fObj.transform.SetParent(canvasObj.transform, false);
            fObj.transform.SetSiblingIndex(naveRt.GetSiblingIndex());

            Image img = fObj.AddComponent<Image>();
            img.sprite = spriteCirculo;
            img.color = corFumaca;

            RectTransform rt = img.rectTransform;
            float tam = Random.Range(38f, 76f) * escala;
            rt.sizeDelta = new Vector2(tam, tam);
            rt.anchoredPosition = posNave + new Vector2(Random.Range(-25f, 25f), Random.Range(-25f, 25f));

            fumacasUI.Add(new ParticulaFumacaUI
            {
                rt = rt,
                img = img,
                velocidade = new Vector2(Random.Range(-90f, 90f), Random.Range(40f, 140f)),
                tempoVida = 0f,
                duracao = Random.Range(0.45f, 0.75f),
                escalaInicial = 1f
            });
        }

        private void AtualizarFumacasUI(float dt)
        {
            for (int i = fumacasUI.Count - 1; i >= 0; i--)
            {
                var f = fumacasUI[i];
                if (f.rt == null)
                {
                    fumacasUI.RemoveAt(i);
                    continue;
                }

                f.tempoVida += dt;
                if (f.tempoVida >= f.duracao)
                {
                    Destroy(f.rt.gameObject);
                    fumacasUI.RemoveAt(i);
                    continue;
                }

                float p = f.tempoVida / f.duracao;
                f.rt.anchoredPosition += f.velocidade * dt;
                float esc = Mathf.Lerp(f.escalaInicial, f.escalaInicial * 2.1f, p);
                f.rt.localScale = new Vector3(esc, esc, 1f);

                Color c = f.img.color;
                c.a = Mathf.Lerp(0.75f, 0f, p);
                f.img.color = c;
            }
        }

        private void AnimarEstrelas(float tempo, float velX, float velY, float alphaMult = 1f)
        {
            for (int i = 0; i < estrelas.Count; i++)
            {
                Image st = estrelas[i];
                if (st == null) continue;
                RectTransform rt = st.rectTransform;
                Vector2 p = rt.anchoredPosition;
                float camada = (i % 3 + 1) * 0.45f;
                p.x += velX * camada * Time.unscaledDeltaTime;
                p.y += velY * camada * Time.unscaledDeltaTime;

                if (p.x > 960f) p.x -= 1920f;
                if (p.x < -960f) p.x += 1920f;
                if (p.y > 540f) p.y -= 1080f;
                if (p.y < -540f) p.y += 1080f;
                rt.anchoredPosition = p;

                Color c = st.color;
                c.a = (0.45f + 0.5f * Mathf.Abs(Mathf.Sin(tempo * 3.5f + i))) * alphaMult;
                st.color = c;
            }
        }

        private IEnumerator RotinaCutscene()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Cutscene);

            PlayerController player = GameManager.Instance != null ? GameManager.Instance.player : FindFirstObjectByType<PlayerController>();
            Vector3 posInicialPlayer = player != null ? player.transform.position : new Vector3(-1.5f, -0.95f, 0f);

            GameObject propShip = GameObject.Find("Prop_CrashedShip");
            if (propShip != null)
            {
                propShip.transform.localScale = new Vector3(2.35f, 2.35f, 1f);
                propShip.transform.position = new Vector3(-5.2f, 2.15f, 0f);
            }
            Vector3 posFinalShip = propShip != null ? propShip.transform.position : new Vector3(-5.2f, 2.15f, 0f);

            // =========================================================================
            // ATO 1: Logo "SPACE CHICO" Gigante Encolhendo + Texto Star Wars Subindo Lento e Inclinado
            // =========================================================================
            AudioManager.Play(Sfx.PowerUp, 0.85f);

            float duracaoCrawl = 11.2f;
            float velCrawl = 88f; // Velocidade lenta e majestosa estilo Star Wars
            float scrollOffset = 0f;
            float t = 0f;
            while (t < duracaoCrawl && !pulouCutscene)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;

                AnimarEstrelas(t, -8f, -6f);
                planetaRt.localRotation = Quaternion.Euler(0f, 0f, t * 2.2f);

                // Logo gigante encolhe em direção ao fundo do espaço nos primeiros 3.2 segundos
                float pLogo = Mathf.Clamp01(t / 3.2f);
                float escLogo = Mathf.Lerp(2.35f, 0.14f, pLogo);
                logoRt.localScale = new Vector3(escLogo, escLogo, 1f);
                logoRt.anchoredPosition = new Vector2(0f, Mathf.Lerp(-10f, 445f, pLogo));
                logoText.color = new Color(1f, 0.88f, 0.2f, Mathf.Clamp01(1f - (pLogo - 0.65f) / 0.35f));

                // Parágrafos sobem lentamente ao longo do plano inclinado 3D (começando logo após o logo afastar)
                if (t > 0.8f)
                {
                    scrollOffset += dt * velCrawl;
                }

                for (int i = 0; i < paragrafos.Count; i++)
                {
                    var par = paragrafos[i];
                    float yPlano = par.posBase.y + scrollOffset;
                    par.rt.anchoredPosition = new Vector2(0f, yPlano);
                    if (par.efeitoPerspectiva != null)
                    {
                        par.efeitoPerspectiva.yCentroParagrafo = yPlano;
                        par.txt.SetVerticesDirty();
                    }
                }

                yield return null;
            }

            logoRt.gameObject.SetActive(false);

            // =========================================================================
            // ATO 2: A Nave do Chico Vem Zunindo, Solta Fumaça e Atropela cada Parágrafo!
            // =========================================================================
            if (!pulouCutscene)
            {
                barraLegendaObj.SetActive(true);
                legendaText.text = "De repente, a nave desgovernada do Chico atropelou a história pelo espaço!";
                AudioManager.Play(Sfx.Alert, 0.9f);
                AudioManager.Play(Sfx.Shoot, 1.0f);

                t = 0f;
                float duracaoCorte = 1.75f;
                float timerFumaca = 0f;

                // A nave vem grande de trás da câmera (direita/topo) para o meio/esquerda do espaço!
                Vector2 posNaveIni = new Vector2(1380f, 420f);
                Vector2 posNaveFim = new Vector2(-1380f, -340f);

                while (t < duracaoCorte && !pulouCutscene)
                {
                    float dt = Time.unscaledDeltaTime;
                    t += dt;
                    float p = Mathf.Clamp01(t / duracaoCorte);
                    scrollOffset += dt * (velCrawl * 0.5f);

                    AnimarEstrelas(t + 5f, -55f, -25f);

                    Vector2 posNaveAtual = Vector2.Lerp(posNaveIni, posNaveFim, p);
                    naveRt.anchoredPosition = posNaveAtual;

                    // Efeito 3D: a nave entra grande perto da câmera (1.55x) e vai para o espaço (0.85x)
                    float escNave = Mathf.Lerp(1.55f, 0.85f, p);
                    naveRt.localScale = new Vector3(escNave, escNave, 1f);
                    naveRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 55f) * 6f);

                    // Solta bolinhas de fumaça e faíscas do propulsor da nave
                    timerFumaca += dt;
                    if (timerFumaca >= 0.035f)
                    {
                        timerFumaca = 0f;
                        EmitirFumacaNaveUI(posNaveAtual + new Vector2(160f * escNave, 65f * escNave), new Color(1f, 0.65f, 0.2f, 0.85f), escNave);
                        EmitirFumacaNaveUI(posNaveAtual + new Vector2(190f * escNave, 80f * escNave), new Color(0.65f, 0.55f, 0.8f, 0.7f), escNave * 1.2f);
                    }
                    AtualizarFumacasUI(dt);

                    // Feixe de plasma cortando o espaço
                    float intFeixe = Mathf.Sin(p * Mathf.PI);
                    rastroCorteImg.color = new Color(0.35f, 0.95f, 1f, intFeixe * 0.9f);
                    rastroCorteImg.rectTransform.sizeDelta = new Vector2(2600f, 8f + intFeixe * 36f);

                    // Verifica colisão da nave com cada parágrafo do texto para espalhá-los um por um!
                    for (int i = 0; i < paragrafos.Count; i++)
                    {
                        var par = paragrafos[i];
                        float gatilhoP = 0.18f + i * 0.11f;
                        if (!par.foiAtropelado && p >= gatilhoP)
                        {
                            par.foiAtropelado = true;
                            float yPlano = par.posBase.y + scrollOffset;
                            float escAtual = PerspectivaStarWarsEffect.CalcularEscalaPerspectiva(yPlano);
                            float screenYAtual = PerspectivaStarWarsEffect.CalcularScreenY(yPlano);

                            if (par.efeitoPerspectiva != null)
                            {
                                par.efeitoPerspectiva.ativo = false;
                                par.txt.SetVerticesDirty();
                            }
                            par.rt.anchoredPosition = new Vector2(0f, screenYAtual);
                            par.rt.localScale = new Vector3(escAtual, escAtual * 0.75f, 1f);

                            float dirY = (i % 2 == 0) ? 1f : -1f;
                            float dirX = (i < 2) ? -1f : 1f;
                            par.velocidadeVoando = new Vector2(dirX * Random.Range(420f, 680f), dirY * Random.Range(320f, 540f));
                            par.velocidadeGiro = dirX * Random.Range(35f, 65f);

                            AudioManager.Play(Sfx.Land, 0.55f);
                            if (i == 2)
                            {
                                AudioManager.Play(Sfx.BossSlam, 0.85f);
                                flashTela.color = new Color(0.6f, 0.95f, 1f, 0.55f);
                            }
                        }

                        if (!par.foiAtropelado)
                        {
                            float yPlano = par.posBase.y + scrollOffset;
                            par.rt.anchoredPosition = new Vector2(0f, yPlano);
                            if (par.efeitoPerspectiva != null)
                            {
                                par.efeitoPerspectiva.yCentroParagrafo = yPlano;
                                par.txt.SetVerticesDirty();
                            }
                        }
                        else
                        {
                            par.rt.anchoredPosition += par.velocidadeVoando * dt;
                            par.anguloAtual += par.velocidadeGiro * dt;
                            par.rt.localRotation = Quaternion.Euler(0f, 0f, par.anguloAtual);
                            Color c = par.txt.color;
                            c.a = Mathf.Max(0f, c.a - dt * 1.35f);
                            par.txt.color = c;
                        }
                    }

                    flashTela.color = new Color(0.6f, 0.95f, 1f, Mathf.Max(0f, flashTela.color.a - dt * 2.5f));
                    yield return null;
                }

                crawlRootRt.gameObject.SetActive(false);
                rastroCorteImg.gameObject.SetActive(false);
            }

            // =========================================================================
            // ATO 3: Pan Contínuo de Câmera — Espaço -> Planeta -> Nuvens da Atmosfera!
            // =========================================================================
            if (!pulouCutscene)
            {
                legendaText.text = "Sem controle, a nave mergulhou na atmosfera do Planeta Bioluminescente!";
                AudioManager.Play(Sfx.BossSpit, 0.9f);

                // Posiciona o jogador (invisível) lá no alto do céu da fase para a câmera real do jogo já apontar para o céu!
                if (player != null)
                {
                    var rb = player.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.simulated = false;
                    if (player.spriteRenderer != null) player.spriteRenderer.enabled = false;
                    player.transform.position = posFinalShip + new Vector3(-5.0f, 7.2f, 0f);

                    var camFollow = FindFirstObjectByType<CameraFollow>();
                    if (camFollow != null) camFollow.SnapToTarget();
                }
                if (propShip != null) propShip.SetActive(false);

                planetaImg.color = Color.white;

                t = 0f;
                float duracaoPlaneta = 2.7f;
                float timerFumaca = 0f;
                float proxAlarme = 0.35f;

                while (t < duracaoPlaneta && !pulouCutscene)
                {
                    float dt = Time.unscaledDeltaTime;
                    t += dt;
                    float p = Mathf.Clamp01(t / duracaoPlaneta);
                    float curvaZoom = p * p;

                    if (t >= proxAlarme)
                    {
                        proxAlarme += 0.75f;
                        AudioManager.Play(Sfx.Alert, 0.65f);
                    }

                    // As estrelas sobem rápido enquanto a câmera desce do espaço para o planeta
                    float alphaEspaco = Mathf.Clamp01(1f - Mathf.Max(0f, (p - 0.68f) / 0.32f));
                    AnimarEstrelas(t + 10f, -60f, 340f, alphaEspaco);

                    // O Planeta sobe e cresce até preencher a tela
                    Vector2 posPlaneta = Vector2.Lerp(new Vector2(580f, -260f), new Vector2(80f, -20f), p);
                    float tamPlaneta = Mathf.Lerp(240f, 1850f, curvaZoom);
                    planetaRt.anchoredPosition = posPlaneta;
                    planetaRt.sizeDelta = new Vector2(tamPlaneta, tamPlaneta);
                    planetaRt.localRotation = Quaternion.Euler(0f, 0f, 12f + t * 9f);
                    planetaImg.color = new Color(1f, 1f, 1f, alphaEspaco);

                    // A nave faz uma curva mergulhando para dentro do planeta
                    Vector2 posNave = Vector2.Lerp(new Vector2(-780f, 300f), new Vector2(80f, -20f), p);
                    posNave.y += Mathf.Sin(p * Mathf.PI) * 170f + Mathf.Sin(t * 48f) * 10f;
                    naveRt.anchoredPosition = posNave;

                    float escNave = Mathf.Lerp(0.95f, 0.16f, p);
                    naveRt.localScale = new Vector3(-escNave, escNave, 1f);
                    naveRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(14f, -38f, p) + Mathf.Sin(t * 50f) * 6f);

                    timerFumaca += dt;
                    if (timerFumaca >= 0.045f && p < 0.85f)
                    {
                        timerFumaca = 0f;
                        EmitirFumacaNaveUI(posNave, new Color(1f, 0.6f, 0.2f, 0.75f * alphaEspaco), escNave * 1.1f);
                    }
                    AtualizarFumacasUI(dt);

                    // Nos últimos 40%, as nuvens da atmosfera sobem e o fundo espacial abre para o céu real da fase!
                    if (p > 0.58f)
                    {
                        float pNuvem = (p - 0.58f) / 0.42f;
                        float arcoNuvem = Mathf.Sin(pNuvem * Mathf.PI);
                        nuvemEsqImg.color = new Color(0.22f, 0.10f, 0.38f, arcoNuvem * 0.92f);
                        nuvemDirImg.color = new Color(0.15f, 0.48f, 0.45f, arcoNuvem * 0.92f);

                        nuvemEsqRt.anchoredPosition = Vector2.Lerp(new Vector2(-750f, -700f), new Vector2(-950f, 650f), pNuvem);
                        nuvemDirRt.anchoredPosition = Vector2.Lerp(new Vector2(750f, -700f), new Vector2(950f, 650f), pNuvem);

                        fundoEspacoImg.color = new Color(0.02f, 0.01f, 0.05f, 1f - pNuvem);
                    }

                    yield return null;
                }
            }

            // Remove o overlay espacial (o céu da fase já está visível atrás!)
            if (canvasObj != null) Destroy(canvasObj);

            // =========================================================================
            // ATO 4: A Nave Cai em Chamas na Fase, Bate no Chão e o Chico é Ejetado!
            // =========================================================================
            if (player != null && !pulouCutscene)
            {
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.simulated = false;
                if (player.spriteRenderer != null) player.spriteRenderer.enabled = false;
                if (propShip != null) propShip.SetActive(false);

                // Cria a nave em voo no cenário 2D da fase para cair do céu até o ponto da batida
                GameObject naveMundoObj = new GameObject("NaveCaindoMundo");
                SpriteRenderer srNaveMundo = naveMundoObj.AddComponent<SpriteRenderer>();
                if (texNaveSprite != null)
                {
                    srNaveMundo.sprite = Sprite.Create(
                        texNaveSprite,
                        new Rect(0, 0, texNaveSprite.width, texNaveSprite.height),
                        new Vector2(0.5f, 0.5f),
                        115f
                    );
                }
                srNaveMundo.sortingOrder = 28;
                naveMundoObj.transform.localScale = new Vector3(-1f, 1f, 1f);

                Vector3 posAltoShip = posFinalShip + new Vector3(-6.5f, 8.2f, 0f);
                naveMundoObj.transform.position = posAltoShip;
                player.transform.position = posAltoShip;

                // 4A. Descida da nave em chamas pelo céu da fase acompanhada pela câmera
                AudioManager.Play(Sfx.BossSpit, 0.95f);
                float quedaT = 0f;
                float duracaoQuedaNave = 0.95f;
                while (quedaT < duracaoQuedaNave && !pulouCutscene)
                {
                    quedaT += Time.deltaTime;
                    float p = Mathf.Clamp01(quedaT / duracaoQuedaNave);
                    float curva = p * p;

                    Vector3 posAtualNave = Vector3.Lerp(posAltoShip, posFinalShip, curva);
                    naveMundoObj.transform.position = posAtualNave;
                    naveMundoObj.transform.rotation = Quaternion.Euler(0f, 0f, -24f + Mathf.Sin(quedaT * 45f) * 5f);

                    // O player invisível acompanha a nave para a câmera descer suavemente do céu até o chão!
                    player.transform.position = Vector3.Lerp(posAltoShip, posInicialPlayer, p);

                    if (Random.value < 0.75f)
                    {
                        VFX.Burst(posAtualNave + new Vector3(-1.8f, 1.1f, 0f), new Color(1f, 0.6f, 0.15f), 5, 3.8f, 0.30f, 0.45f);
                    }

                    yield return null;
                }

                Destroy(naveMundoObj);
                if (propShip != null)
                {
                    propShip.transform.localScale = new Vector3(2.35f, 2.35f, 1f);
                    propShip.transform.position = posFinalShip;
                    propShip.SetActive(true);
                }

                // 4B. IMPACTO DA NAVE NO CHÃO + EJEÇÃO DO CHICO EM PARÁBOLA!
                if (!pulouCutscene)
                {
                    AudioManager.Play(Sfx.BossSlam, 1.0f);
                    AudioManager.Play(Sfx.Jump, 0.9f);
                    CameraFollow.Shake(0.45f, 0.4f);

                    // Explosão de faíscas e fumaça na nave quebrada
                    VFX.Burst(posFinalShip + Vector3.up * 0.6f, new Color(1f, 0.55f, 0.15f), 32, 6.0f, 0.35f, 0.7f);
                    VFX.Burst(posFinalShip + Vector3.up * 0.9f, new Color(0.6f, 0.95f, 1f), 18, 4.5f, 0.25f, 0.5f);

                    // O Chico aparece sendo ejetado de dentro da cúpula da nave fazendo um arco parabólico!
                    if (player.spriteRenderer != null)
                    {
                        player.spriteRenderer.enabled = true;
                        if (player.spriteFall != null) player.spriteRenderer.sprite = player.spriteFall;
                    }

                    Vector3 origemEjecao = posFinalShip + new Vector3(0.4f, 0.65f, 0f);
                    VFX.PopText(origemEjecao + Vector3.up * 0.8f, "MIAU!", new Color(1f, 0.9f, 0.3f), 0.9f);

                    float ejecaoT = 0f;
                    float duracaoEjecao = 0.72f;
                    while (ejecaoT < duracaoEjecao && !pulouCutscene)
                    {
                        ejecaoT += Time.deltaTime;
                        float p = Mathf.Clamp01(ejecaoT / duracaoEjecao);

                        // Movimento em arco parabólico da nave até o ponto inicial do jogador
                        Vector3 posBase = Vector3.Lerp(origemEjecao, posInicialPlayer, p);
                        float alturaArco = Mathf.Sin(p * Mathf.PI) * 3.4f;
                        player.transform.position = posBase + Vector3.up * alturaArco;

                        // Faz uma pirueta de 360 graus no ar enquanto é ejetado
                        player.transform.rotation = Quaternion.Euler(0f, 0f, -p * 360f);

                        yield return null;
                    }

                    // Pousa no chão com um pequeno quique
                    player.transform.rotation = Quaternion.identity;
                    player.transform.position = posInicialPlayer;
                    if (player.spriteIdle != null && player.spriteRenderer != null)
                        player.spriteRenderer.sprite = player.spriteIdle;

                    AudioManager.Play(Sfx.Land, 0.85f);
                    CameraFollow.Shake(0.2f, 0.18f);
                    VFX.Burst(posInicialPlayer, new Color(0.4f, 1f, 0.8f), 16, 3.6f, 0.2f, 0.38f);
                    VFX.PopText(posInicialPlayer + Vector3.up * 1.5f, "RECUPERE O PROPULSOR!", new Color(0.4f, 1f, 0.9f), 1.4f);
                }
            }

            // Garante que tudo esteja na posição final correta (inclusive se o jogador pulou a abertura)
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
                if (player.spriteRenderer != null)
                {
                    player.spriteRenderer.enabled = true;
                    if (player.spriteIdle != null) player.spriteRenderer.sprite = player.spriteIdle;
                }
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
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
