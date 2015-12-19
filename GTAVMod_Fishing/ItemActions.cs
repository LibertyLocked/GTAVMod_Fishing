using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTAVMod_Fishing
{
    public static class ItemActions
    {
        public static void HealPlayer(Entity ent)
        {
            Game.Player.Character.Health = Game.Player.Character.MaxHealth;
        }

        public static void ClearPlayerWantedLevel(Entity ent)
        {
            Game.Player.WantedLevel = 0;
        }

        public static void KillPed(Entity ent)
        {
            if ((Ped)ent != null)
            {
                ((Ped)ent).Kill();
            }
        }

        public static void ShootTaserBullet(Entity ent)
        {
            //void SHOOT_SINGLE_BULLET_BETWEEN_COORDS(float x1, float y1, float z1, float x2, float y2, float z2, 
            //int damage, BOOL p7, Hash weaponHash, Ped ownerPed, BOOL p10, BOOL p11, float speed) // 867654CBC7606F2C CB7415AC
            Vector3 shootTo = Game.Player.Character.Position;
            Vector3 shootFrom = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 1f;
            Model stunGunModel = new Model(WeaponHash.StunGun);
            Ped attacker = World.CreatePed(new Model(PedHash.Fish), shootFrom);
            Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS, shootFrom.X, shootFrom.Y, shootFrom.Z, shootTo.X, shootTo.Y, shootTo.Z,
                0, true, stunGunModel.Hash, attacker, true, true, 1f);
            attacker.Delete();
        }

        public static void CatEatsFish(Entity ent)
        {
            Fish eatenFish = FishingScript.inventory.RemoveRandomFish();
            if (eatenFish != null)
            {
                UI.Notify("Cat has eaten your ~r~" + eatenFish.Name + " ~g~$" + eatenFish.Price);
                if (eatenFish.Entity != null)
                {
                    eatenFish.Entity.Delete();
                }
            }
        }
    }

}
