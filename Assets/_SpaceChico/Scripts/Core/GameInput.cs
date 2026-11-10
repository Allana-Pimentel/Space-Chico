using UnityEngine;

namespace SpaceChico
{
    public enum GameAction { Left, Right, Up, Down, Jump, Pause, Confirm }

    // Leitura simples dos controles do teclado (Setas / WASD + Espaço + Esc)
    public static class GameInput
    {
        public static float Horizontal => (Held(GameAction.Right) ? 1f : 0f) - (Held(GameAction.Left) ? 1f : 0f);
        public static float Vertical => (Held(GameAction.Up) ? 1f : 0f) - (Held(GameAction.Down) ? 1f : 0f);

        public static bool Held(GameAction a)
        {
            switch (a)
            {
                case GameAction.Left:  return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
                case GameAction.Right: return Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
                case GameAction.Up:    return Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
                case GameAction.Down:  return Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
                case GameAction.Jump:  return Input.GetKey(KeyCode.Space);
                default: return false;
            }
        }

        public static bool Pressed(GameAction a)
        {
            switch (a)
            {
                case GameAction.Jump:    return Input.GetKeyDown(KeyCode.Space);
                case GameAction.Pause:   return Input.GetKeyDown(KeyCode.Escape);
                case GameAction.Confirm: return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
                default: return false;
            }
        }

        public static bool Released(GameAction a) => a == GameAction.Jump && Input.GetKeyUp(KeyCode.Space);
    }
}
