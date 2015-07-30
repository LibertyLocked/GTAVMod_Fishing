using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Native;
using GTA.Math;

namespace GTAVMod_Fishing
{
    class ItemPopulator
    {
        Random rng = new Random();

        public int PopulateFishes(Fish[] fishes)
        {
            fishes = new Fish[] 
            { // count: 23
                new Fish("Shrimp", 1, Rarity.Common),
                new Fish("Spotted Bay Bass", 10, Rarity.Common),
                new Fish("Pigeon", 10, new PedHash[] { PedHash.Pigeon }, Rarity.Common, new Vector3(38f, 38f, 7f), null),
                new Fish("Cat", 10, new PedHash[] { PedHash.Cat }, Rarity.Common, new Vector3(52f, 52f, 7f), null),
                new Fish("Blue Rockfish", 15, Rarity.Common),
                new Fish("Barred Sand Bass", 20, Rarity.Common),
                new Fish("Barred Surf Perch", 20, Rarity.Common),
                new Fish("Kelp Bass", 30, Rarity.Common),
                new Fish("White Sea Bass", 40, Rarity.Common),
                new Fish("Spotfin Croaker", 45, Rarity.Common),
                new Fish("California Halibut", 50, Rarity.Common),
                new Fish("Pacific Barracuda", 60, Rarity.Common),
                new Fish("Swordfish", 70, Rarity.Common),
                new Fish("California Corbina", 70, Rarity.Common),
                new Fish("Red Lionfish", 80, Rarity.Common),
                new Fish("Black Rockfish", 85, Rarity.Common),
                new Fish("American Sole", 90, Rarity.Common),
                new Fish("Yellowtail", 100, Rarity.Common),
                new Fish("Steelhead", 120, Rarity.Common),
                new Fish("Walleye Surfperch", 200, Rarity.Common),
                new Fish("Goldfish", 400, Rarity.Common),
                new Fish("Dolphin", 700, new PedHash[] { PedHash.Dolphin }, Rarity.Exceptional, null),
                new Fish("Tiger Shark", 800, new PedHash[] { PedHash.TigerShark }, Rarity.Legendary, null),
            };
            return fishes.Length;
        }

        public int PopulateSpecialItems(FishItem[] fishItems)
        {
            fishItems = new FishItem[]
            { // count: 42
                // no model
                new FishItem("Condom", 
                    Rarity.Common, null),
                //new FishItem("test", new PedHash[]{PedHash.Crow},
                //    Rarity.Common, new ItemAction(x => World.AddExplosion(x.Position, ExplosionType.Fire, 1f, 0))),
                // peds
                new FishItem("Dead Hooker", new PedHash[]{PedHash.Hooker01SFY, PedHash.Hooker02SFY, PedHash.Hooker03SFY}, 
                    Rarity.Uncommon, ItemActions.KillPed),
                new FishItem("Dead Johnny", new PedHash[]{PedHash.JohnnyKlebitz},
                    Rarity.Uncommon, ItemActions.KillPed),
                new FishItem("Dead Cop", new PedHash[]{PedHash.Cop01SFY, PedHash.Cop01SMY},
                    Rarity.Uncommon, ItemActions.KillPed),
                new FishItem("Zombie", new PedHash[]{PedHash.Zombie01},
                    Rarity.Rare, new ItemAction(x => ((Ped)x).Task.FightAgainst(Game.Player.Character))),
                // vehs
                new FishItem("Bike", new VehicleHash[]{VehicleHash.Bmx, VehicleHash.TriBike, VehicleHash.Cruiser, VehicleHash.Scorcher, VehicleHash.Fixter}, 
                    Rarity.Uncommon, null),
                new FishItem("Caddy", new VehicleHash[]{VehicleHash.Caddy, VehicleHash.Caddy2}, 
                    Rarity.Uncommon, null),
                new FishItem("Faggio", new VehicleHash[]{VehicleHash.Faggio2}, 
                    Rarity.Uncommon, null),
                new FishItem("Blazer", new VehicleHash[]{VehicleHash.Blazer, VehicleHash.Blazer2, VehicleHash.Blazer3},
                    Rarity.Uncommon, null),
                new FishItem("Lawn Mower", new VehicleHash[]{VehicleHash.Mower},
                    Rarity.Uncommon, null),
                // props
                new FishItem("Wallet", new string[]{"prop_ld_wallet_01", "prop_ld_wallet_02"},
                    Rarity.Common, delegate { Game.Player.Money += 100 + rng.Next(900);}),
                new FishItem("Pizza", new string[]{"prop_pizza_box_01", "prop_pizza_box_02"}, 
                    Rarity.Common, ItemActions.HealPlayer),
                new FishItem("Sandwich", new string[]{"prop_sandwich_01"},
                    Rarity.Common, ItemActions.HealPlayer),
                new FishItem("Donut", new string[]{"prop_donut_01", "prop_donut_02"},
                    Rarity.Common, ItemActions.HealPlayer),
                new FishItem("Hotdog", new string[]{"prop_cs_hotdog_01", "prop_cs_hotdog_02"},
                    Rarity.Common, ItemActions.HealPlayer),
                new FishItem("Taco", new string[]{"prop_taco_01", "prop_taco_02"},
                    Rarity.Common, ItemActions.HealPlayer),
                new FishItem("Screwdriver", new string[]{"prop_tool_screwdvr01", "prop_tool_screwdvr02", "prop_tool_screwdvr03"}, 
                    Rarity.Common, null),
                new FishItem("Alien Egg", new string[]{"prop_alien_egg_01"},
                    Rarity.Uncommon, null),
                new FishItem("Car Door", new string[]{"prop_car_door_01", "prop_car_door_02", "prop_car_door_03", "prop_car_door_04"},
                    Rarity.Common, null),
                new FishItem("Car Seat", new string[]{"prop_car_seat"},
                    Rarity.Common, null),
                new FishItem("Dildo", new string[]{"prop_cs_dildo_01"},
                    Rarity.Common, null),
                new FishItem("Shit", new string[]{"prop_big_shit_01", "prop_big_shit_02"},
                    Rarity.Common, null),
                new FishItem("Coffin", new string[]{"prop_coffin_02"},
                    Rarity.Common, null),
                new FishItem("DVD Player", new string[]{"prop_cs_dvd_player"},
                    Rarity.Common, null),
                new FishItem("Guitar", new string[]{"prop_acc_guitar_01"},
                    Rarity.Common, null),
                new FishItem("Bong", new string[]{"prop_bong_01"},
                    Rarity.Common, null),
                new FishItem("Body Armor", new string[]{"prop_armour_pickup"},
                    Rarity.Common, delegate { Game.Player.Character.Armor = 100;}),
                new FishItem("Toothbrush", new string[]{"prop_toothbrush_01"},
                    Rarity.Common, null),
                new FishItem("Wheelchair", new string[]{"prop_wheelchair_01"},
                    Rarity.Common, null),
                new FishItem("Weed", new string[]{"prop_weed_02"},
                    Rarity.Common, null),
                new FishItem("Nailgun", new string[]{"prop_tool_nailgun"},
                     Rarity.Common, null),
                new FishItem("Protest Sign", new string[]{"prop_protest_sign_01"},
                    Rarity.Common, null),
                new FishItem("Fax Machine", new string[]{"prop_fax_01"},
                    Rarity.Common, null),
                new FishItem("FIB Badge", new string[]{"prop_fib_badge"},
                    Rarity.Common, ItemActions.ClearPlayerWantedLevel),
                new FishItem("Book", new string[]{"prop_cs_book_01"},
                    Rarity.Common, null),
                new FishItem("Beer", new string[]{"prop_cs_beer_bot_01", "prop_cs_beer_bot_02"},
                    Rarity.Common, null),
                new FishItem("Tape", new string[]{"prop_beta_tape"},
                    Rarity.Common, null),
                new FishItem("Tape Player", new string[]{"prop_tapeplayer_01"},
                    Rarity.Common, null),
                new FishItem("Ballistic Shield", new string[]{"prop_ballistic_shield"},
                    Rarity.Common, null),
                new FishItem("Bongo", new string[]{"prop_bongos_01"},
                    Rarity.Common, null),
                new FishItem("Battery", new string[]{"prop_battery_01"},
                    Rarity.Common, ItemActions.ShootTaserBullet),
                new FishItem("Explosive Crow", new PedHash[]{PedHash.Crow},
                    Rarity.Common, new ItemAction(x => World.AddExplosion(x.Position, ExplosionType.ExplosionWithFire1, 1f, 1f))),
            };
            return fishItems.Length;
        }
    }
}
