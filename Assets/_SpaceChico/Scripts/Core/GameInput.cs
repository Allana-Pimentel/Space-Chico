using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SpaceChico
{
    /// <summary>Ações de entrada do jogo (GDD seção 4).</summary>
    public enum GameAction
    {
        Left,       // Seta Esquerda
        Right,      // Seta Direita
        Up,         // Seta Cima   (interagir / escalar)
        Down,       // Seta Baixo  (agachar / descer vinha)
        Jump,       // Barra de Espaço (pular / pulo esmagador)
        Shoot,      // Tecla E
        Power,      // Tecla F
        Backpack,   // Tecla B
        Pause,      // Esc
        Confirm     // Enter / Espaço em menus
    }

    /// <summary>
    /// Camada fina sobre o sistema de input da Unity. Funciona tanto com o
    /// Input Manager antigo quanto com o novo Input System, então o resto do
    /// código nunca precisa saber qual está ativo.
    /// </summary>
    public static class GameInput
    {
        public static bool Held(GameAction a)
        {
            switch (a)
            {
                case GameAction.Confirm: return K(KC.Return) || K(KC.KeypadEnter) || K(KC.Space);
                default: return K(Map(a));
            }
        }

        public static bool Pressed(GameAction a)
        {
            switch (a)
            {
                case GameAction.Confirm: return D(KC.Return) || D(KC.KeypadEnter) || D(KC.Space);
                default: return D(Map(a));
            }
        }

        public static bool Released(GameAction a)
        {
            switch (a)
            {
                case GameAction.Confirm: return U(KC.Return) || U(KC.KeypadEnter) || U(KC.Space);
                default: return U(Map(a));
            }
        }

        /// <summary>-1, 0 ou +1 de acordo com as setas esquerda/direita.</summary>
        public static float Horizontal => (Held(GameAction.Right) ? 1f : 0f) - (Held(GameAction.Left) ? 1f : 0f);

        /// <summary>-1, 0 ou +1 de acordo com as setas baixo/cima.</summary>
        public static float Vertical => (Held(GameAction.Up) ? 1f : 0f) - (Held(GameAction.Down) ? 1f : 0f);

        // ------------------------------------------------------------------
        enum KC { Left, Right, Up, Down, Space, E, F, B, Escape, Return, KeypadEnter }

        static KC Map(GameAction a)
        {
            switch (a)
            {
                case GameAction.Left: return KC.Left;
                case GameAction.Right: return KC.Right;
                case GameAction.Up: return KC.Up;
                case GameAction.Down: return KC.Down;
                case GameAction.Jump: return KC.Space;
                case GameAction.Shoot: return KC.E;
                case GameAction.Power: return KC.F;
                case GameAction.Backpack: return KC.B;
                case GameAction.Pause: return KC.Escape;
                default: return KC.Return;
            }
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        static KeyCode ToKeyCode(KC k)
        {
            switch (k)
            {
                case KC.Left: return KeyCode.LeftArrow;
                case KC.Right: return KeyCode.RightArrow;
                case KC.Up: return KeyCode.UpArrow;
                case KC.Down: return KeyCode.DownArrow;
                case KC.Space: return KeyCode.Space;
                case KC.E: return KeyCode.E;
                case KC.F: return KeyCode.F;
                case KC.B: return KeyCode.B;
                case KC.Escape: return KeyCode.Escape;
                case KC.KeypadEnter: return KeyCode.KeypadEnter;
                default: return KeyCode.Return;
            }
        }
        static bool K(KC k) => Input.GetKey(ToKeyCode(k));
        static bool D(KC k) => Input.GetKeyDown(ToKeyCode(k));
        static bool U(KC k) => Input.GetKeyUp(ToKeyCode(k));
#elif ENABLE_INPUT_SYSTEM
        static Key ToKey(KC k)
        {
            switch (k)
            {
                case KC.Left: return Key.LeftArrow;
                case KC.Right: return Key.RightArrow;
                case KC.Up: return Key.UpArrow;
                case KC.Down: return Key.DownArrow;
                case KC.Space: return Key.Space;
                case KC.E: return Key.E;
                case KC.F: return Key.F;
                case KC.B: return Key.B;
                case KC.Escape: return Key.Escape;
                case KC.KeypadEnter: return Key.NumpadEnter;
                default: return Key.Enter;
            }
        }
        static bool K(KC k) { var kb = Keyboard.current; return kb != null && kb[ToKey(k)].isPressed; }
        static bool D(KC k) { var kb = Keyboard.current; return kb != null && kb[ToKey(k)].wasPressedThisFrame; }
        static bool U(KC k) { var kb = Keyboard.current; return kb != null && kb[ToKey(k)].wasReleasedThisFrame; }
#else
        static bool K(KC k) => false;
        static bool D(KC k) => false;
        static bool U(KC k) => false;
#endif
    }
}
