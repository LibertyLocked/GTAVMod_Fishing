using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTAVMod_Fishing
{
    public class PlayerInventory
    {
        FishingRod fishingRod;

        List<Fish> fishes;
        int size;

        public int MaxSize
        {
            get { return size; }
        }

        public int CurrSize
        {
            get { return fishes.Count; }
        }

        public bool HasFishingRod
        {
#if DEBUG
            get { return true; }
#else
            get { return fishingRod != null; }
#endif
        }

        public PlayerInventory(int size)
        {
            this.size = size;
            fishes = new List<Fish>();
        }

        public bool AddFish(Fish fish)
        {
            if (fishes.Count >= size)
            {
                return false;
            }
            else
            {
                fishes.Add(new Fish(fish.Name, fish.Price, fish.Rarity));
                return true;
            }
        }

        public int SellAllFish()
        {
            int sellMoney = 0;
            foreach (Fish fish in fishes)
            {
                sellMoney += fish.Price;
            }
            fishes.Clear();
            return sellMoney;
        }

        public void AddFishingRod(FishingRod fishingRod)
        {
            this.fishingRod = fishingRod;
        }
    }

}
