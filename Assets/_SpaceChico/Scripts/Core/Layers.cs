using UnityEngine;

namespace SpaceChico
{
    /// <summary>
    /// Nomes das layers e tags usadas no projeto. O builder cria as layers no
    /// TagManager; aqui também configuramos a matriz de colisão em tempo de
    /// execução para garantir que ela esteja sempre correta.
    /// </summary>
    public static class Layers
    {
        public const string Ground = "Ground";
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string EnemyTrigger = "EnemyTrigger";
        public const string Projectile = "Projectile";
        public const string Pickup = "Pickup";
        public const string Hazard = "Hazard";

        public const string TagPlayer = "Player";
        public const string TagEnemy = "Inimigo";   // conforme o GDD (Sprint 4.3)
        public const string TagGround = "Ground";

        public static readonly string[] All = { Ground, Player, Enemy, EnemyTrigger, Projectile, Pickup, Hazard };

        public static int GroundMask => LayerMask.GetMask(Ground);

        /// <summary>Pares de layers que NÃO devem colidir.</summary>
        public static readonly string[,] IgnoredPairs =
        {
            { Player, Enemy },              // corpo do inimigo não empurra o jogador (o dano vem dos triggers)
            { Enemy, Enemy },
            { Enemy, Pickup },
            { Enemy, Hazard },
            { Projectile, Player },
            { Projectile, Enemy },          // o tiro acerta os triggers (EnemyTrigger), não o corpo
            { Projectile, Pickup },
            { Projectile, Projectile },
            { Projectile, Hazard },
            { EnemyTrigger, Ground },
            { EnemyTrigger, Enemy },
            { EnemyTrigger, EnemyTrigger },
            { EnemyTrigger, Pickup },
            { EnemyTrigger, Hazard },
            { Pickup, Ground },
            { Pickup, Pickup },
            { Pickup, Hazard },
            { Hazard, Ground },
            { Hazard, Hazard },
        };

        public static void ApplyCollisionMatrix()
        {
            for (int i = 0; i < IgnoredPairs.GetLength(0); i++)
            {
                int a = LayerMask.NameToLayer(IgnoredPairs[i, 0]);
                int b = LayerMask.NameToLayer(IgnoredPairs[i, 1]);
                if (a >= 0 && b >= 0) Physics2D.IgnoreLayerCollision(a, b, true);
            }
        }
    }

    /// <summary>Progresso que sobrevive à troca de cena (ex.: +1 vida máxima por peça).</summary>
    public static class ProgressData
    {
        public static int BonusMaxLife;
        public static int TotalCrystals;
        public static void NewGame() { BonusMaxLife = 0; TotalCrystals = 0; }
    }
}
