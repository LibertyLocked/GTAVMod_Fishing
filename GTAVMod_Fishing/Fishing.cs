/*
 * Fishing Mod
 * Author: libertylocked
 * Version: 0.2.3
 * License: GPLv2
*/
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GTA;
using GTA.Math;
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

    public class Fishing : Script
    {
        public static bool DebugMode = false; // turn this off on release
        public static int DebugIndex = 0;
        const int _BONE_LEFTHAND = 0x49D9;
        const float _FISHINGBOAT_RANGE = 10f;
        const float _SELLINGSPOT_RANGE = 5f;
        const float _MINIGAME_FRAMETIME = 0.034f;

        Prop fishingRod;
        Vector3[] fishingSpotPos;
        Vector3 sellingSpotPos;
        bool fishAnywhere = false;
        UIText promtText = new UIText("", new Point(50, 50), 0.5f, Color.White);
        Keys fishingKey;
        int fishingButton;
        bool isFishing = false;
        PlayerInventory inventory;
        Fish[] NormalFishes;
        FishItem[] SpecialItems;
        Ped playerPed;

        float minigameTimer = 0;
        int secondsToCatchFish = 0;
        Random rng;

        public Fishing()
        {
            SetupAvailableItems();
            SetupLocations();
            ParseSettings();
            inventory = new PlayerInventory();
            rng = new Random();

            this.Tick += OnTick;
            this.KeyDown += OnKeyDown;
        }

        void OnTick(object sender, EventArgs e)
        {
            Player player = Game.Player;
            if (player != null) playerPed = player.Character;
            if (player != null && player.CanControlCharacter && player.IsAlive && player.Character != null)
            {
                if (!playerPed.IsInVehicle())
                {
                    if (Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 2, fishingButton)) FishingKeyPressed();

                    if ((IsPedInFishingArea(playerPed) || IsPedNearBoat(playerPed))
                        && !isFishing && !fishAnywhere)
                    {
                        promtText.Caption = "Press " + fishingKey.ToString() + " to fish";
                        promtText.Draw();
                    }
                    if (IsPedInSellingArea(playerPed) && !isFishing)
                    {
                        promtText.Caption = "Press " + fishingKey.ToString() + " to sell fish";
                        promtText.Draw();
                    }

                    if (isFishing)
                    {
                        Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
                        UpdateMinigame();
                    }
                }
            }
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == fishingKey) FishingKeyPressed();
        }

        void FishingKeyPressed()
        {
            if (IsPedInFishingArea(Game.Player.Character) || IsPedNearBoat(Game.Player.Character) &&
                (!Game.Player.Character.IsInVehicle() && Function.Call<bool>(Hash.CAN_PLAYER_START_MISSION, Game.Player)))
            {
                if (isFishing)
                {
                    StopFishing();
                }
                else
                {
                    StartFishing();
                }
            }
            else if (IsPedInSellingArea(Game.Player.Character))
            {
                // Sell fish
                int sellAmount = inventory.SellAllFish();
                if (sellAmount > 0)
                {
                    Game.Player.Money += sellAmount;
                    UI.ShowSubtitle("You've sold all your fish for $" + sellAmount);
                }
                else
                {
                    UI.ShowSubtitle("You haven't caught any fish yet!");
                }
            }
        }

        void StopFishing()
        {
            playerPed.Task.ClearAllImmediately();
            if (fishingRod != null)
            {
                fishingRod.Detach();
                fishingRod.Delete();
            }
            isFishing = false;
        }

        void StartFishing()
        {
            playerPed.Task.ClearAllImmediately();
            playerPed.Task.PlayAnimation("amb@world_human_stand_fishing@idle_a", "idle_c", 8f, -1, true, -1); // thanks jedijosh920 for this
            if (fishingRod != null)
            {
                fishingRod.Detach();
                fishingRod.Delete();
            }
            fishingRod = World.CreateProp(new Model("prop_fishing_rod_01"), Vector3.Zero, false, false);
            fishingRod.AttachTo(playerPed, playerPed.PedBoneIndex(_BONE_LEFTHAND), new Vector3(0.13f, 0.1f, 0.01f), new Vector3(180f, 90f, 70f));
            if (DebugMode)
                secondsToCatchFish = 1;
            else
                secondsToCatchFish = 5 + rng.Next(10);
            UI.ShowSubtitle("Wait for it...", 5000);
            isFishing = true;
            minigameTimer = 0; // reset timer
        }

        void UpdateMinigame()
        {
            OutputArgument output = new OutputArgument();
            minigameTimer += Game.LastFrameTime;
            if (minigameTimer > secondsToCatchFish)
            {
                minigameTimer = 0;
                // Give player rewards
                int rNumber = rng.Next(20);
                if (rNumber == 0) // 5% nothing
                {
                    UI.ShowSubtitle("You didn't catch anything.");
                }
                else if (rNumber <= (DebugMode ? 19 : 7))  // 7, 35% getting special items
                {
                    if (DebugMode) DebugIndex = (DebugIndex + 1) % SpecialItems.Length;
                    int caughtIndex = (DebugMode ? DebugIndex: rng.Next(SpecialItems.Length));
                    FishItem caughtItem = SpecialItems[caughtIndex];
                    caughtItem.Spawn();
                    UI.ShowSubtitle("You've caught a " + caughtItem.Name, 5000);
                }
                else
                {
                    // caught regular fish
                    Fish caughtFish = NormalFishes[rng.Next(NormalFishes.Length)];
                    inventory.AddFish(caughtFish);

                    // spawn a fish
                    caughtFish.Spawn();
                    //Vector3 vel = playerPed.ForwardVector * -1;
                    //Vector3 spawnPos = playerPed.Position + playerPed.ForwardVector * 20;
                    //Ped fish = World.CreatePed(new Model(PedHash.Fish), spawnPos);
                    //vel *= 34f;
                    //vel.Z = 7f;
                    //fish.Velocity = vel;

                    UI.ShowSubtitle("You've caught a " + caughtFish.Name + ", worth $" + caughtFish.Price, 5000);
                }
                StopFishing();
            }
        }

        void SetupAvailableItems()
        {
            NormalFishes = new Fish[] 
            { // count: 20
                new Fish("Shrimp", 1, Rarity.Common),
                new Fish("Spotted Bay Bass", 10, Rarity.Common),
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
                new Fish("Tiger Shark", 800, new PedHash[] { PedHash.TigerShark }, Rarity.Common, null),
            };
            SpecialItems = new FishItem[]
            { // count: 31
                // no model
                new FishItem("Condom", 
                    Rarity.Common, null),
                // peds
                new FishItem("Dead Hooker", new PedHash[]{PedHash.Hooker01SFY, PedHash.Hooker02SFY, PedHash.Hooker03SFY}, 
                    Rarity.Common, new ItemAction(x => ((Ped)x).Kill())),
                new FishItem("Dead Johnny", new PedHash[]{PedHash.JohnnyKlebitz},
                    Rarity.Common, new ItemAction(x => ((Ped)x).Kill())),
                new FishItem("Zombie", new PedHash[]{PedHash.Zombie01},
                    Rarity.Common, new ItemAction(x => ((Ped)x).Task.FightAgainst(playerPed))),
                // vehs
                new FishItem("Bike", new VehicleHash[]{VehicleHash.Bmx, VehicleHash.TriBike, VehicleHash.Cruiser, VehicleHash.Scorcher, VehicleHash.Fixter}, 
                    Rarity.Common, null),
                new FishItem("Caddy", new VehicleHash[]{VehicleHash.Caddy, VehicleHash.Caddy2}, 
                    Rarity.Common, null),
                new FishItem("Faggio", new VehicleHash[]{VehicleHash.Faggio2}, 
                    Rarity.Common, null),
                new FishItem("Blazer", new VehicleHash[]{VehicleHash.Blazer, VehicleHash.Blazer2, VehicleHash.Blazer3},
                    Rarity.Common, null),
                new FishItem("Lawn Mower", new VehicleHash[]{VehicleHash.Mower},
                    Rarity.Common, null),
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
                    Rarity.Common, null),
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
                new FishItem("Body Armor", new string[]{"prop_bodyarmour_06"},
                    Rarity.Common, delegate { playerPed.Armor = 100;}),
                new FishItem("Toothbrush", new string[]{"prop_toothbrush_01"},
                    Rarity.Common, null),
                new FishItem("Wheelchair", new string[]{"prop_wheelchair_01"},
                    Rarity.Common, null),
                new FishItem("Weed", new string[]{"prop_weed_01", "prop_weed_02"},
                    Rarity.Common, null),
                new FishItem("Nailgun", new string[]{"prop_tool_nailgun"},
                     Rarity.Common, null),
               new FishItem("Protest Sign", new string[]{"prop_protest_sign_01"},
                    Rarity.Common, null),

            };
        }

        void SetupLocations()
        {
            fishingSpotPos = new Vector3[]
            {
                // Del Perro Pier
                new Vector3(-1821.588f, -1271.219f, 9.517f),
                new Vector3(-1860.388f, -1230.928f, 6.417f),

                // Zancudo River
                new Vector3(-2075.437f, 2599.529f, 4.584f),
                new Vector3(-2084.582f, 2612.879f, 1.584f),

                // legacy sphere collision points:
                // these 3 covers Del Perro Pier
                //new Vector3(-1857.286f, -1242.648f, 8.616f),
                //new Vector3(-1843.145f, -1256.142f, 8.616f),
                //new Vector3(-1831.284f, -1266.262f, 8.616f),
            };
            sellingSpotPos = new Vector3(-1835.398f, -1206.695f, 14.305f); 

            // Set selling blip
            //int blipSelling = Function.Call<int>(Hash.ADD_BLIP_FOR_COORD, sellingSpotPos.X, sellingSpotPos.Y, sellingSpotPos.Z);
            //Function.Call(Hash.SET_BLIP_AS_SHORT_RANGE, blipSelling, true);
            //Function.Call(Hash.SET_BLIP_SPRITE, blipSelling, 52);
            //Function.Call(Hash.SET_BLIP_COLOUR, blipSelling, 3);
        }

        void ParseSettings()
        {
            ScriptSettings settings = ScriptSettings.Load(@".\scripts\Fishing.ini");
            fishingKey = (Keys)Enum.Parse(typeof(Keys), settings.GetValue("Config", "FishingKey", "F5"), true);
            fishingButton = settings.GetValue("Config", "FishingButton", 234);
            fishAnywhere = settings.GetValue("Config", "FishAnywhere", false);
        }

        bool IsPedInFishingArea(Ped playerPed)
        {
            if (fishAnywhere) return true;
            else
            {
                for (int i = 0; i < fishingSpotPos.Length; i += 2)
                {
                    if (Function.Call<bool>(Hash.IS_ENTITY_IN_AREA, playerPed, fishingSpotPos[i].X, fishingSpotPos[i].Y, fishingSpotPos[i].Z,
                        fishingSpotPos[i + 1].X, fishingSpotPos[i + 1].Y, fishingSpotPos[i + 1].Z, true, true, true)) return true;
                    //if (playerPed.IsInRangeOf(v, FISHINGSPOT_RANGE)) return true;
                }
                return false;
            }
        }

        bool IsPedInSellingArea(Ped playerPed)
        {
            return playerPed.IsInRangeOf(sellingSpotPos, _SELLINGSPOT_RANGE);
        }

        bool IsPedNearBoat(Ped playerPed)
        {
            //Vehicle veh = Function.Call<Vehicle>(Hash.GET_CLOSEST_VEHICLE, playerPed.Position.X, playerPed.Position.Y, playerPed.Position.Z, 1000f, (long)0, 70);
            //Vehicle veh = World.GetClosestVehicle(Game.Player.Character.Position, 1000);
            //return veh.Model.IsBoat;
            Vehicle veh = Game.Player.LastVehicle;
            //UI.ShowSubtitle("Veh " + veh.DisplayName + " " + veh.Model.IsBoat + " " + veh.Position.DistanceTo(playerPed.Position));
            return (veh != null && veh.Model.IsBoat && veh.IsInRangeOf(playerPed.Position, _FISHINGBOAT_RANGE));
        }
    }

    public enum EntityType
    {
        Ped,
        Vehicle,
        Prop,
        None,
    }

    public enum Rarity
    {
        Legendary = 1,
        Exceptional = 2,
        Unique = 4,
        Rare = 8,
        Uncommon = 16,
        Common = 32,
    }

    public delegate void ItemAction(Entity ent);

    public class FishItem
    {
        const float _VEL_PED_XY = 40f;
        const float _VEL_VEH_XY = 20f;
        const float _VEL_PROP_XY = 30f;
        const float _VEL_PED_Z = 8f;
        const float _VEL_VEH_Z = 8f;
        const float _VEL_PROP_Z = 8.5f;

        public string Name
        {
            get;
            private set;
        }
        public EntityType EntityType
        {
            get;
            private set;
        }
        public Rarity Rarity
        {
            get;
            private set;
        }
        PedHash[] pedHashes;
        VehicleHash[] vehHashes;
        string[] propStrs;
        ItemAction action;
        Vector3 velocityMultiplier;

        public FishItem(string name, PedHash[] pedHashes, Rarity rarity, ItemAction action)
            : this(name, pedHashes, null, null, rarity, EntityType.Ped, new Vector3(_VEL_PED_XY, _VEL_PED_XY, _VEL_PED_Z), action)
        { }

        public FishItem(string name, PedHash[] pedHashes, Rarity rarity, Vector3 velocityMultiplier, ItemAction action)
            : this(name, pedHashes, null, null, rarity, EntityType.Ped, velocityMultiplier, action)
        { }

        public FishItem(string name, VehicleHash[] vehHashes, Rarity rarity, ItemAction action)
            : this(name, null, vehHashes, null, rarity, EntityType.Vehicle, new Vector3(_VEL_VEH_XY, _VEL_VEH_XY, _VEL_VEH_Z), action)
        { }

        public FishItem(string name, string[] propStrs, Rarity rarity, ItemAction action)
            : this(name, null, null, propStrs, rarity, EntityType.Prop, new Vector3(_VEL_PROP_XY, _VEL_PROP_XY, _VEL_PROP_Z), action)
        { }

        public FishItem(string name, Rarity rarity, ItemAction action)
            : this(name, null, null, null, rarity, EntityType.None, Vector3.Zero, action)
        { }

        private FishItem(string name, PedHash[] pedHashes, VehicleHash[] vehHashes, string[] propStrs, Rarity rarity, EntityType entityType, 
            Vector3 velocityMultiplier, ItemAction action)
        {
            this.Name = name;
            this.pedHashes = pedHashes;
            this.vehHashes = vehHashes;
            this.propStrs = propStrs;
            this.Rarity = rarity;
            this.EntityType = entityType;
            this.action = action;
            this.velocityMultiplier = velocityMultiplier;
        }

        public void Spawn()
        {
            Entity ent = null;
            if (EntityType != EntityType.None)
            {
                Ped playerPed = Game.Player.Character;
                Random rng = new Random();
                Vector3 spawnPos = playerPed.Position + playerPed.ForwardVector * 20;
                Vector3 vel = playerPed.ForwardVector * -1;
                if (Fishing.DebugMode) UI.Notify(Fishing.DebugIndex + " " + Name);

                if (EntityType == EntityType.Ped)
                {
                    PedHash pedHashSelected = pedHashes[rng.Next(pedHashes.Length)];
                    ent = World.CreatePed(new Model(pedHashSelected), spawnPos);
                }
                else if (EntityType == EntityType.Vehicle)
                {
                    VehicleHash vehHashSelected = vehHashes[rng.Next(vehHashes.Length)];
                    ent = World.CreateVehicle(new Model(vehHashSelected), spawnPos);
                }
                else
                {
                    string propStrSelected = propStrs[rng.Next(propStrs.Length)];
                    ent = World.CreateProp(new Model(propStrSelected), spawnPos, true, false);
                    //ent.Position = playerPed.Position + playerPed.ForwardVector * 0.1f; // TEST Entity stuck fix
                }

                vel.X *= velocityMultiplier.X;
                vel.Y *= velocityMultiplier.Y;
                vel.Z = velocityMultiplier.Z;
                ent.Velocity = vel;

                // try to fix entity if it's stuck (props are buggy)
                if (ent.Velocity == Vector3.Zero)
                {
                    ent.Position = playerPed.Position + playerPed.ForwardVector * 0.1f; // tp the entity
                    Game.Player.Character.ApplyForce(playerPed.ForwardVector * -2); // push player a bit
                }

                if (Fishing.DebugMode)
                {
                    UI.Notify("Pos " + ent.Position.ToString());
                    UI.Notify("Vel " + ent.Velocity.ToString());
                }
            }

            if (action != null) action(ent);
        }
    }

    public class Fish : FishItem
    {
        public string Name
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
            : base(name, pedHashes, rarity, new Vector3(34f, 34f, 7f), action)
        {
            Name = name;
            Price = price;
        }
    }

    public class PlayerInventory
    {
        List<Fish> fishes;

        public PlayerInventory()
        {
            fishes = new List<Fish>();
        }

        public void AddFish(Fish fish)
        {
            fishes.Add(new Fish(fish.Name, fish.Price, fish.Rarity));
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
    }

    public static class ItemActions
    {
        public static void HealPlayer(Entity ent)
        {
            Game.Player.Character.Health = Game.Player.Character.MaxHealth;
        }
    }
}
