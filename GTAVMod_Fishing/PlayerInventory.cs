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
        List<FishingRod> fishingRods;
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
            get { return fishingRods.Count > 0; }
#endif
        }

        public int FishingRodsCount
        {
            get { return fishingRods.Count; }
        }

        public PlayerInventory(int size)
        {
            this.size = size;
            fishes = new List<Fish>();
            fishingRods = new List<FishingRod>();
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

        public Fish RemoveRandomFish()
        {
            if (fishes.Count > 0)
            {
                Fish removedFish = fishes[new Random().Next(CurrSize)];
                fishes.Remove(removedFish);
                return removedFish;
            }
            else
            {
                return null;
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
            this.fishingRods.Add(fishingRod);
        }

        public void RemoveFishingRod()
        {
            if (fishingRods.Count > 0)
            {
                this.fishingRods.RemoveAt(0);
            }
        }
    }

}
