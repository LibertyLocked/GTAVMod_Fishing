using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTAVMod_Fishing
{
    class LocationHelper
    {
        const float _FISHINGBOAT_RANGE = 10f;
        const float _SELLINGSPOT_RANGE = 5f;
        Vector3[] fishingSpotPos, storePos;
        Vector3 sellingSpotPos;

        public LocationHelper()
        {
            // Fishing positions
            fishingSpotPos = new Vector3[]
            {
                // Del Perro Pier
                new Vector3(-1821.588f, -1271.219f, 9.517f),
                new Vector3(-1860.388f, -1230.928f, 6.417f),
                // Zancudo River 1
                new Vector3(-2075.437f, 2599.529f, 4.584f),
                new Vector3(-2084.582f, 2612.879f, 1.584f),
                // Mirror Park
                new Vector3(1121.867f, -666.051f, 58.601f),
                new Vector3(1106.016f, -628.940f, 55.901f),
                // Alamo Sea 1
                new Vector3(721.455f, 4087.194f, 39.352f),
                new Vector3(707.821f, 4099.042f, 33.352f),
                // Alamo Sea 2
                new Vector3(1729.679f, 3987.693f, 32.352f),
                new Vector3(1736.789f, 3980.702f, 30.252f),
                // Alamo Sea 3
                new Vector3(1305.793f, 4208.094f, 37.609f),
                new Vector3(1295.458f, 4223.160f, 32.009f),
                // Alamo Sea 4
                new Vector3(1344.326f, 4218.037f, 35.509f),
                new Vector3(1332.694f, 4230.413f, 31.509f),
                // Lighthouse 1
                new Vector3(3376.033f, 5180.303f, 2.460f),
                new Vector3(3369.488f, 5185.949f, -0.540f),
                // Paleto 1
                new Vector3(-288.438f, 6617.072f, 10.897f),
                new Vector3(-266.353f, 6647.833f, 5.497f),
                // Chumash 1
                new Vector3(-3429.836f, 947.696f, 10.106f),
                new Vector3(-3424.115f, 985.415f, 6.406f),
            };

            // Selling position
            sellingSpotPos = new Vector3(-1835.398f, -1206.695f, 14.305f);

            // Store positions
            storePos = new Vector3[]
            {
                new Vector3(-2967.852f, 391.494f, 15.043f),
                new Vector3(-1487.709f, -378.642f, 40.163f),
                new Vector3(1135.730f, -982.735f, 46.416f),
                new Vector3(1699.184f, 4923.674f, 42.064f),
                new Vector3(1163.344f, -322.426f, 69.205f),
                new Vector3(-1821.456f, 793.741f, 138.116f),
                new Vector3(-1222.349f, -906.804f, 12.326f),
                new Vector3(-707.411f, -913.185f, 19.216f),
                new Vector3(-47.401f, -1756.713f, 29.421f),
                new Vector3(548.025f, 2669.449f, 42.156f),
                new Vector3(25.745f, -1345.546f, 29.497f),
                new Vector3(2555.487f, 382.163f, 108.623f),
                new Vector3(374.221f, 327.793f, 103.566f),
                new Vector3(2677.138f, 3281.371f, 55.241f),
                new Vector3(1729.772f, 6416.200f, 35.037f),
                new Vector3(-3041.044f, 585.193f, 7.909f),
                new Vector3(-3243.957f, 1001.409f, 12.831f),
                new Vector3(1165.471f, 2709.360f, 38.158f),
                new Vector3(1960.363f, 3742.034f, 32.344f),
            };
        }

        public bool IsPlayerNearBoat(Player player)
        {
            Vehicle veh = player.LastVehicle;
            return (veh != null && veh.Model.IsBoat && veh.IsInRangeOf(player.Character.Position, _FISHINGBOAT_RANGE));
        }

        public bool IsEntityInFishingArea(Entity ent)
        {
            if (Globals.FishAnywhere) return true;
            else return isEntityInOneOfTheAreas(ent, fishingSpotPos);
        }

        public bool IsEntityInSellingArea(Entity ent)
        {
            return ent.IsInRangeOf(sellingSpotPos, _SELLINGSPOT_RANGE);
        }

        public bool IsEntityInStoreArea(Entity ent)
        {
            foreach (Vector3 p in storePos)
            {
                if (ent.IsInRangeOf(p, _SELLINGSPOT_RANGE))
                {
                    return true;
                }
            }
            return false;
        }


        private bool isEntityInOneOfTheAreas(Entity ent, Vector3[] vertices)
        {
            for (int i = 0; i < vertices.Length; i += 2)
            {
                if (Function.Call<bool>(Hash.IS_ENTITY_IN_AREA, ent, vertices[i].X, vertices[i].Y, vertices[i].Z,
                    vertices[i + 1].X, vertices[i + 1].Y, vertices[i + 1].Z, true, true, true)) return true;
            }
            return false;
        }


    }
}
