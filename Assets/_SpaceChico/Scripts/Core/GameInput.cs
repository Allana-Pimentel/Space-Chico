using UnityEngine;

namespace SpaceChico
{
    // Ações do jogador no teclado
    public enum GameAction
    {
        Left, Right, Up, Down, Jump, Shoot, Power, Backpack, Pause, Confirm
    }

    // Script simples para centralizar a leitura das teclas
    public static class GameInput
    {
        // Retorna -1 (esquerda), 0 (parado) ou 1 (direita)
        public static float Horizontal
        {
            get
            {
                float h = 0f;
                if (Held(GameAction.Right)) h += 1f;
                if (Held(GameAction.Left)) h -= 1f;
                return h;
            }
        }

        // Retorna -1 (baixo), 0 (parado) ou 1 (cima)
        public static float Vertical
        {
            get
            {
                float v = 0f;
                if (Held(GameAction.Up)) v += 1f;
                if (Held(GameAction.Down)) v -= 1f;
                return v;
            }
        }

        // Verifica se a tecla está sendo segurada
        public static bool Held(GameAction acao)
        {
            switch (acao)
            {
                case GameAction.Left:     return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
                case GameAction.Right:    return Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
                case GameAction.Up:       return Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
                case GameAction.Down:     return Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
                case GameAction.Jump:     return Input.GetKey(KeyCode.Space);
                case GameAction.Shoot:    return Input.GetKey(KeyCode.E);
                case GameAction.Power:    return Input.GetKey(KeyCode.F);
                case GameAction.Backpack: return Input.GetKey(KeyCode.B) || Input.GetKey(KeyCode.I) || Input.GetKey(KeyCode.Tab);
                case GameAction.Pause:    return Input.GetKey(KeyCode.Escape);
                case GameAction.Confirm:  return Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.Space);
                default: return false;
            }
        }

        // Verifica se a tecla acabou de ser apertada neste frame
        public static bool Pressed(GameAction acao)
        {
            switch (acao)
            {
                case GameAction.Left:     return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
                case GameAction.Right:    return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
                case GameAction.Up:       return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
                case GameAction.Down:     return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
                case GameAction.Jump:     return Input.GetKeyDown(KeyCode.Space);
                case GameAction.Shoot:    return Input.GetKeyDown(KeyCode.E);
                case GameAction.Power:    return Input.GetKeyDown(KeyCode.F);
                case GameAction.Backpack: return Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab);
                case GameAction.Pause:    return Input.GetKeyDown(KeyCode.Escape);
                case GameAction.Confirm:  return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
                default: return false;
            }
        }

        // Verifica se a tecla foi solta neste frame
        public static bool Released(GameAction acao)
        {
            switch (acao)
            {
                case GameAction.Jump: return Input.GetKeyUp(KeyCode.Space);
                default: return false;
            }
        }
    }
}
