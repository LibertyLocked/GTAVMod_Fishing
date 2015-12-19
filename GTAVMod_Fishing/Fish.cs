using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTAVMod_Fishing
{
    public class Fish : FishItem
    {
        public Entity Entity
        {
            get;
            private set;
        }

        public int Price
        {
            get;
            private set;
        }

        public Fish(string name, int price, Rarity rarity)
            : this(name, price, new PedHash[] { PedHash.Fish }, rarity, null)
        { }

        public Fish(string name, int price, PedHash[] pedHashes, Rarity rarity, ItemAction action)
            : this(name, price, pedHashes, rarity, new Vector3(34f, 34f, 7f), action)
        { }

        public Fish(string name, int price, PedHash[] pedHashes, Rarity rarity, Vector3 velocityMultiplier, ItemAction action)
            : base(name, pedHashes, rarity, velocityMultiplier, action)
        {
            Price = price;
        }

        public override Entity Spawn()
        {
            Entity = base.Spawn();
            return Entity;
        }
    }

}
