using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAVMod_Fishing
{
    public class FishingRod
    {
        int price;

        public int Price
        {
            get { return price; }
        }

        public FishingRod(int price)
        {
            this.price = price;
        }
    }
}
