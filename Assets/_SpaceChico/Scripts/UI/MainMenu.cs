using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceChico
{
    public class MainMenu : MonoBehaviour
    {
        public string cenaDoJogo = "01_Pantano";

        private Font fonte;
        private GameObject painelPrincipal, painelCreditos;
        private Button botaoJogar, botaoVoltarCreditos;
        private Text tituloTexto;

        private readonly Color amarelo = new Color(1f, 0.88f, 0.2f);

        void Start()
        {
            Time.timeScale = 1f;

            fonte = Resources.Load<Font>("Fonts/RussoOne-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            CriarPainel("Escurecer", new Color(0f, 0f, 0f, 0.45f));

            painelPrincipal = CriarPainel("PainelPrincipal", Color.clear);
            tituloTexto = CriarTexto(painelPrincipal.transform, "SPACE CHICO", 120, amarelo, new Vector2(0, 260));
            CriarTexto(painelPrincipal.transform, "O Pantano Bioluminescente", 40, Color.white, new Vector2(0, 160));
            botaoJogar = CriarBotao(painelPrincipal.transform, "JOGAR", new Vector2(0, 0), Jogar);
            CriarBotao(painelPrincipal.transform, "CREDITOS", new Vector2(0, -110), MostrarCreditos);
            CriarTexto(painelPrincipal.transform, "Setas: escolher   |   Enter: confirmar", 24, new Color(1f, 1f, 1f, 0.7f), new Vector2(0, -380));

            painelCreditos = CriarPainel("PainelCreditos", Color.clear);
            CriarTexto(painelCreditos.transform, "CREDITOS", 80, amarelo, new Vector2(0, 300));
            CriarTexto(painelCreditos.transform,
                "SPACE CHICO: O Pantano Bioluminescente\n\n" +
                "Projeto Academico de Computacao / Jogos Digitais\n" +
                "Motor: Unity 6 (2D Retro Platformer)\n" +
                "Mecanicas: Pulo Duplo, Escalada, Stomp & Boss Fight\n\n" +
                "Controles: Setas / WASD (Mover e Subir Vinhas)\n" +
                "Espaco: Pular e Pisar na cabeca dos inimigos\n" +
                "Esc: Pausar o jogo",
                32, Color.white, new Vector2(0, 50));
            botaoVoltarCreditos = CriarBotao(painelCreditos.transform, "VOLTAR", new Vector2(0, -260), MostrarMenu);

            MostrarMenu();
        }

        void Update()
        {
            if (tituloTexto != null)
            {
                float s = 1f + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.04f;
                tituloTexto.transform.localScale = new Vector3(s, s, 1f);
            }

            if (painelCreditos.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                MostrarMenu();

            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                Button padrao = painelCreditos.activeSelf ? botaoVoltarCreditos : botaoJogar;
                EventSystem.current.SetSelectedGameObject(padrao.gameObject);
            }
        }

        void Jogar()
        {
            AudioManager.Play(Sfx.UiSelect);
            ProgressData.NewGame();
            IntroShipCutscene.ResetarIntro();
            SceneManager.LoadScene(cenaDoJogo);
        }

        void MostrarCreditos()
        {
            AudioManager.Play(Sfx.UiSelect);
            painelPrincipal.SetActive(false);
            painelCreditos.SetActive(true);
            EventSystem.current.SetSelectedGameObject(botaoVoltarCreditos.gameObject);
        }

        void MostrarMenu()
        {
            painelCreditos.SetActive(false);
            painelPrincipal.SetActive(true);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(botaoJogar.gameObject);
        }

        void Sair()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }


        GameObject CriarPainel(string nome, Color cor)
        {
            GameObject painel = new GameObject(nome, typeof(RectTransform), typeof(Image));
            painel.transform.SetParent(transform, false);

            RectTransform rt = painel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image img = painel.GetComponent<Image>();
            img.color = cor;
            img.raycastTarget = false;
            return painel;
        }

        Text CriarTexto(Transform pai, string conteudo, int tamanho, Color cor, Vector2 posicao)
        {
            GameObject obj = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(pai, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1600, 400);
            rt.anchoredPosition = posicao;

            Text txt = obj.GetComponent<Text>();
            txt.font = fonte;
            txt.text = conteudo;
            txt.fontSize = tamanho;
            txt.color = cor;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
            return txt;
        }

        Button CriarBotao(Transform pai, string rotulo, Vector2 posicao, UnityAction acao)
        {
            GameObject obj = new GameObject("Botao_" + rotulo, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(pai, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(420, 86);
            rt.anchoredPosition = posicao;

            Button botao = obj.GetComponent<Button>();
            ColorBlock cores = botao.colors;
            cores.normalColor = new Color(0.08f, 0.12f, 0.18f, 0.85f);
            cores.highlightedColor = new Color(0.95f, 0.65f, 0.15f, 1f);
            cores.selectedColor = new Color(0.95f, 0.65f, 0.15f, 1f);
            cores.pressedColor = new Color(0.7f, 0.45f, 0.1f, 1f);
            botao.colors = cores;
            botao.onClick.AddListener(acao);

            Text txt = CriarTexto(obj.transform, rotulo, 44, Color.white, Vector2.zero);
            txt.rectTransform.sizeDelta = rt.sizeDelta;
            return botao;
        }
    }
}

