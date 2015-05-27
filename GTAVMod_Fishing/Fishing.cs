/*
 * Fishing Mod
 * Author: libertylocked
 * Version: 0.2.2
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
        public static bool DebugMode = false;
        public static int DebugIndex = 0;

        const int BONE_LEFTHAND = 0x49D9;
        const float FISHINGSPOT_RANGE = 10f;
        const float SELLINGSPOT_RANGE = 5f;

        Prop fishingRod;
        Vector3 fishingSpotPos = new Vector3(-1849.973f, -1249.577f, 8.616f);
        Vector3 sellingSpotPos = new Vector3(-1835.398f,-1206.695f, 14.305f);
        UIText promtText = new UIText("", new Point(50, 50), 0.5f, Color.White);
        Keys fishingKey = Keys.F5;
        bool isFishing = false;
        PlayerInventory inventory;
        Fish[] NormalFishes;
        FishItem[] SpecialItems;
        Ped playerPed;

        const float MINIGAME_FRAMETIME = 0.034f;
        float minigameTimer = 0;
        int secondsToCatchFish = 0;
        Random rng;
        //UIRectangle recBack, recPlayer;

        public Fishing()
        {
            SetupAvailableItems();

            inventory = new PlayerInventory();
            rng = new Random();

            //recBack = new UIRectangle(new Point(100, 600), new Size(1080, 50), Color.FromArgb(100, 255, 255, 255));
            //recPlayer = new UIRectangle(new Point(640 - 400 / 2, 600), new Size(400, 50), Color.FromArgb(100, 0, 255, 0));

            ScriptSettings settings = ScriptSettings.Load(@".\scripts\Fishing.ini");
            fishingKey = (Keys)Enum.Parse(typeof(Keys), settings.GetValue("Config", "FishingKey", "F5"), true);

            this.Tick += OnTick;
            this.KeyDown += OnKeyDown;
        }

        void OnTick(object sender, EventArgs e)
        {
            Player player = Game.Player;
            if (player != null && player.CanControlCharacter && player.IsAlive && player.Character != null)
            {
                if (IsPlayerInFishingArea(player.Character) && !isFishing)
                {
                    promtText.Caption = "Press " + fishingKey.ToString() + " to fish";
                    promtText.Draw();
                }
                if (IsPlayerInSellingArea(player.Character) && !isFishing)
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

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == fishingKey)
            {
                if (IsPlayerInFishingArea(Game.Player.Character))
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
                else if (IsPlayerInSellingArea(Game.Player.Character))
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

        }

        void StopFishing()
        {
            playerPed = Game.Player.Character;
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
            playerPed = Game.Player.Character;
            playerPed.Task.ClearAllImmediately();
            playerPed.Task.PlayAnimation("amb@world_human_stand_fishing@idle_a", "idle_c", 8f, -1, true, -1);
            if (fishingRod != null)
            {
                fishingRod.Detach();
                fishingRod.Delete();
            }
            fishingRod = World.CreateProp(new Model("prop_fishing_rod_01"), Vector3.Zero, false, false);
            fishingRod.AttachTo(playerPed, playerPed.PedBoneIndex(BONE_LEFTHAND), new Vector3(0.13f, 0.1f, 0.01f), new Vector3(180f, 90f, 70f));
            if (DebugMode)
                secondsToCatchFish = 1;
            else
                secondsToCatchFish = 5 + rng.Next(10);
            UI.ShowSubtitle("Wait for it...", 10000);
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
                    // spawn a fish
                    Vector3 vel = playerPed.ForwardVector * -1;
                    Vector3 spawnPos = playerPed.Position + playerPed.ForwardVector * 20;
                    Ped fish = World.CreatePed(new Model(PedHash.Fish), spawnPos);
                    vel *= 34f;
                    vel.Z = 7f;
                    fish.Velocity = vel;

                    Fish caughtFish = NormalFishes[rng.Next(NormalFishes.Length)];
                    inventory.AddFish(caughtFish);
                    UI.ShowSubtitle("You've caught a " + caughtFish.Name + ", worth $" + caughtFish.Price, 5000);
                }
                StopFishing();
            }
            //if (minigameTimer >= MINIGAME_FRAMETIME)
            //{
            //    minigameTimer -= Game.LastFrameTime;
            //}
        }

        bool IsPlayerInFishingArea(Ped playerPed)
        {
            return playerPed.IsInRangeOf(fishingSpotPos, FISHINGSPOT_RANGE);
            //return playerPos.DistanceTo(fishingSpotPos) <= FISHINGSPOT_RANGE;
        }

        bool IsPlayerInSellingArea(Ped playerPed)
        {
            return playerPed.IsInRangeOf(sellingSpotPos, SELLINGSPOT_RANGE);
            //return playerPos.DistanceTo(sellingSpotPos) <= SELLINGSPOT_RANGE;
        }

        void SetupAvailableItems()
        {
            NormalFishes = new Fish[] 
            { // count: 17
                new Fish("Shrimp", 1),
                new Fish("Spotted Bay Bass", 10),
                new Fish("Barred Sand Bass", 20),
                new Fish("Barred Surf Perch", 20),
                new Fish("Kelp Bass", 30),
                new Fish("White Sea Bass", 40),
                new Fish("Spotfin Croaker", 45),
                new Fish("California Halibut", 50),
                new Fish("Pacific Barracuda", 60),
                new Fish("Swordfish", 70),
                new Fish("California Corbina", 75),
                new Fish("Red Lionfish", 80),
                new Fish("American Sole", 90),
                new Fish("Yellowtail", 100),
                new Fish("Steelhead", 150),
                new Fish("Walleye Surfperch", 200),
                new Fish("Goldfish", 500),
            };
            SpecialItems = new FishItem[]
            { // count: 25
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
                // props
                new FishItem("Wallet", new string[]{"prop_ld_wallet_01", "prop_ld_wallet_02"},
                    Rarity.Common, delegate { Game.Player.Money += 100 + rng.Next(900);}),
                new FishItem("Pizza", new string[]{"prop_pizza_box_01", "prop_pizza_box_02"}, 
                    Rarity.Common, new ItemAction(x => Game.Player.Character.Health = Game.Player.Character.MaxHealth)),
                new FishItem("Sandwich", new string[]{"prop_sandwich_01"},
                    Rarity.Common, new ItemAction(x => Game.Player.Character.Health = Game.Player.Character.MaxHealth)),
                new FishItem("Donut", new string[]{"prop_donut_01", "prop_donut_02"},
                    Rarity.Common, new ItemAction(x => Game.Player.Character.Health = Game.Player.Character.MaxHealth)),
                new FishItem("Hotdog", new string[]{"prop_cs_hotdog_01", "prop_cs_hotdog_02"},
                    Rarity.Common, new ItemAction(x => Game.Player.Character.Health = Game.Player.Character.MaxHealth)),
                new FishItem("Taco", new string[]{"prop_taco_01", "prop_taco_02"},
                    Rarity.Common, new ItemAction(x => Game.Player.Character.Health = Game.Player.Character.MaxHealth)),
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
                    Rarity.Common, delegate { Game.Player.Character.Armor = 100;})
            };
        }
    }

    //enum FishingState
    //{
    //    Stopped,
    //    Casting,
    //    Caught,
    //}

    public class Fish
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

        public Fish(string name, int price)
        {
            Name = name;
            Price = price;
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
        const float VEL_PED_XY = 40f;
        const float VEL_VEH_XY = 20f;
        const float VEL_PROP_XY = 30f;
        const float VEL_PED_Z = 8f;
        const float VEL_VEH_Z = 8f;
        const float VEL_PROP_Z = 8.5f;

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

        public FishItem(string name, PedHash[] pedHashes, Rarity rarity, ItemAction customAction)
        {
            Name = name;
            this.pedHashes = pedHashes;
            this.action = customAction;
            EntityType = EntityType.Ped;
            Rarity = rarity;
        }

        public FishItem(string name, VehicleHash[] vehHashes, Rarity rarity, ItemAction customAction)
        {
            Name = name;
            this.vehHashes = vehHashes;
            this.action = customAction;
            EntityType = EntityType.Vehicle;
            Rarity = rarity;
        }

        public FishItem(string name, string[] propStrs, Rarity rarity, ItemAction customAction)
        {
            Name = name;
            this.propStrs = propStrs;
            this.action = customAction;
            EntityType = EntityType.Prop;
            Rarity = rarity;
        }

        public FishItem(string name, Rarity rarity, ItemAction customAction)
        {
            Name = name;
            this.action = customAction;
            EntityType = EntityType.None;
            Rarity = rarity;
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
                    vel *= VEL_PED_XY;
                    vel.Z = VEL_PED_Z;
                }
                else if (EntityType == EntityType.Vehicle)
                {
                    VehicleHash vehHashSelected = vehHashes[rng.Next(vehHashes.Length)];
                    ent = World.CreateVehicle(new Model(vehHashSelected), spawnPos);
                    vel *= VEL_VEH_XY;
                    vel.Z = VEL_VEH_Z;
                }
                else
                {
                    string propStrSelected = propStrs[rng.Next(propStrs.Length)];
                    ent = World.CreateProp(new Model(propStrSelected), spawnPos, true, false);
                    vel = Game.Player.Character.Position - spawnPos;
                    //ent.FreezePosition = false;
                    vel.Normalize();
                    vel *= VEL_PROP_XY;
                    vel.Z = VEL_PROP_Z;
                }
                ent.Velocity = vel;

                // try to fix entity if it's stuck
                if (ent.Velocity == Vector3.Zero)
                {
                    ent.Position = playerPed.Position; // dirty fix
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

    public class PlayerInventory
    {
        List<Fish> fishes;

        public PlayerInventory()
        {
            fishes = new List<Fish>();
        }

        public void AddFish(Fish fish)
        {
            fishes.Add(new Fish(fish.Name, fish.Price));
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
}
