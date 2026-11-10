using UnityEngine;

namespace SpaceChico
{
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
        public const string TagEnemy = "Inimigo";
        public const string TagGround = "Ground";

        public static readonly string[] All = { Ground, Player, Enemy, EnemyTrigger, Projectile, Pickup, Hazard };

        public static int GroundMask => LayerMask.GetMask(Ground);

        public static readonly string[,] IgnoredPairs =
        {
            { Enemy, Enemy },
            { Enemy, Pickup },
            { Enemy, Hazard },
            { Projectile, Player },
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
            for (int i = 0; i < All.Length; i++)
            {
                int layerA = LayerMask.NameToLayer(All[i]);
                if (layerA < 0) continue;

                Physics2D.IgnoreLayerCollision(0, layerA, false);
                for (int j = i; j < All.Length; j++)
                {
                    int layerB = LayerMask.NameToLayer(All[j]);
                    if (layerB < 0) continue;
                    Physics2D.IgnoreLayerCollision(layerA, layerB, false);
                }
            }

            for (int i = 0; i < IgnoredPairs.GetLength(0); i++)
            {
                int a = LayerMask.NameToLayer(IgnoredPairs[i, 0]);
                int b = LayerMask.NameToLayer(IgnoredPairs[i, 1]);
                if (a >= 0 && b >= 0) Physics2D.IgnoreLayerCollision(a, b, true);
            }
        }
    }

    public static class ProgressData
    {
        public static int BonusMaxLife;
        public static int TotalCrystals;
        public static void NewGame() { BonusMaxLife = 0; TotalCrystals = 0; }
    }
}
