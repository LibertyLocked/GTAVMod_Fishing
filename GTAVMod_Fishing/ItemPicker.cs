using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GTAVMod_Fishing
{
    public static class ItemPicker
    {
        static Random rng = new Random();
        static int TotalFishChance = 0, TotalItemChance = 0;

        public static Fish PickFromFishes(Fish[] fishes)
        {
            if (TotalFishChance == 0) TotalFishChance = GetTotalChance(fishes);
            return (Fish)Pick(fishes, TotalFishChance);
        }

        public static FishItem PickFromItems(FishItem[] fishItems)
        {
            if (TotalItemChance == 0) TotalItemChance = GetTotalChance(fishItems);
            return Pick(fishItems, TotalItemChance);
        }

        private static FishItem Pick(FishItem[] fishItems, int totalChance)
        {
            int numPicked = rng.Next(totalChance);
            int cumulativeChance = 0;
            int i = 0;
            for (i = 0; i < fishItems.Length; i++)
            {
                //if (numPicked >= cumulativeChance) break;
                cumulativeChance += (int)fishItems[i].Rarity;
                if (numPicked < cumulativeChance) break;
            }
            return fishItems[i];
        }

        private static int GetTotalChance(FishItem[] fishItems)
        {
            int total = 0;
            foreach (FishItem item in fishItems)
            {
                total += (int)item.Rarity;
            }
            return total;
        }
    }

}
