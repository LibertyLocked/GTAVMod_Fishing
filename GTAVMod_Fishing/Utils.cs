using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Native;

namespace GTAVMod_Fishing
{
    public static class Utils
    {
        public static int PedBoneIndex(this Ped p, int b)
        {
            return Function.Call<int>(Hash.GET_PED_BONE_INDEX, p.Handle, b);
        }
    }

}
