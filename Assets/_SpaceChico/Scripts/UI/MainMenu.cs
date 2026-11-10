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
        private GameObject painelPrincipal;
        private GameObject painelControles;
        private Button botaoJogar;
        private Button botaoVoltar;

        private readonly Color amarelo = new Color(1f, 0.88f, 0.2f);

        void Start()
        {
            Time.timeScale = 1f;

            fonte = Resources.Load<Font>("Fonts/RussoOne-Regular");
            if (fonte == null)
                fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            CriarPainel("Escurecer", new Color(0f, 0f, 0f, 0.45f));

            painelPrincipal = CriarPainel("PainelPrincipal", Color.clear);
            CriarTexto(painelPrincipal.transform, "SPACE CHICO", 120, amarelo, new Vector2(0, 260));
            CriarTexto(painelPrincipal.transform, "O Pântano Bioluminescente", 40, Color.white, new Vector2(0, 160));
            botaoJogar = CriarBotao(painelPrincipal.transform, "JOGAR", new Vector2(0, 0), Jogar);
            CriarBotao(painelPrincipal.transform, "CONTROLES", new Vector2(0, -110), MostrarControles);
            CriarBotao(painelPrincipal.transform, "SAIR", new Vector2(0, -220), Sair);
            CriarTexto(painelPrincipal.transform, "Setas: escolher   |   Enter: confirmar", 26, new Color(1f, 1f, 1f, 0.7f), new Vector2(0, -420));

            painelControles = CriarPainel("PainelControles", Color.clear);
            CriarTexto(painelControles.transform, "CONTROLES", 80, amarelo, new Vector2(0, 300));
            CriarTexto(painelControles.transform,
                "Setas ou A / D: andar\n" +
                "Espaço: pular\n" +
                "W / S perto do cipó: subir e descer\n" +
                "Pule na cabeça dos inimigos para derrotá-los\n" +
                "Esc: pausar o jogo",
                38, Color.white, new Vector2(0, 40));
            botaoVoltar = CriarBotao(painelControles.transform, "VOLTAR", new Vector2(0, -260), MostrarMenu);

            MostrarMenu();
        }

        void Update()
        {
            if (painelControles.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                MostrarMenu();

            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                Button padrao = painelControles.activeSelf ? botaoVoltar : botaoJogar;
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

        void MostrarControles()
        {
            AudioManager.Play(Sfx.UiSelect);
            painelPrincipal.SetActive(false);
            painelControles.SetActive(true);
            EventSystem.current.SetSelectedGameObject(botaoVoltar.gameObject);
        }

        void MostrarMenu()
        {
            painelControles.SetActive(false);
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

