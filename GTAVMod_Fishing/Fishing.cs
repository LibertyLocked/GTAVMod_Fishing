/*
 * Fishing Mod
 * Author: libertylocked
 * Version: 0.2.6
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
        const string _SCRIPT_VERSION = "0.2.6";
        const int _BONE_LEFTHAND = 0x49D9;
        const float _FISHINGBOAT_RANGE = 10f;
        const float _SELLINGSPOT_RANGE = 5f;
        const float _MINIGAME_FRAMETIME = 0.034f;

        Keys fishingKey;
        int fishingButton;
        bool entityCleanup = true;
        bool fishAnywhere = false;
        int backpackSize = 30;
        int chanceNothing = 5, chanceJunk = 35;
        int waitTime = 5;

        UIText promtText = new UIText("", new Point(50, 50), 0.5f, Color.White);
        Prop fishingRod;
        Vector3[] fishingSpotPos;
        Vector3 sellingSpotPos;
        bool isFishing = false;
        PlayerInventory inventory;
        Fish[] NormalFishes;
        FishItem[] SpecialItems;
        Ped playerPed;
        List<Entity> spawnedEntities;
        float minigameTimer = 0;
        int secondsToCatchFish = 0;
        Random rng;
        bool creditsShown = false;
        byte[] creditsBytes1, creditsBytes2;

        static Action postAction = null; // action to be performed when fishing stopped

        public Fishing()
        {
            SetupAvailableItems();
            SetupLocations();
            ParseSettings();
            creditsBytes1 = new byte[] { 0x46,0x69,0x73,0x68,0x69,0x6E,0x67,0x20,0x7E,0x72,0x7E,0x76 };
            creditsBytes2 = new byte[] { 0x20,0x7E,0x73,0x7E,0x62,0x79,0x20,0x7E,0x62,0x7E,0x6C,0x69,0x62,0x65,0x72,0x74,0x79,0x6C,0x6F,0x63,0x6B,0x65,0x64 };
            inventory = new PlayerInventory(backpackSize);
            rng = new Random();
            spawnedEntities = new List<Entity>();

            this.Tick += OnTick;
            this.KeyDown += OnKeyDown;
        }

        void OnTick(object sender, EventArgs e)
        {
            Player player = Game.Player;
            if (player != null) playerPed = player.Character;
            if (CanPlayerFish(player))
            {
                if (Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 2, fishingButton)) FishingKeyPressed();

                if ((IsEntityInFishingArea(playerPed) || IsPlayerNearBoat(Game.Player))
                    && !isFishing && !fishAnywhere)
                {
                    promtText.Caption = "Press " + fishingKey.ToString() + " to fish"
                        + "\nBackpack: " + inventory.CurrSize + "/" + inventory.MaxSize;
                    promtText.Draw();
                }
                if (IsEntityInSellingArea(playerPed) && !isFishing)
                {
                    promtText.Caption = "Press " + fishingKey.ToString() + " to sell " + inventory.CurrSize + " fish";
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
            if (e.KeyCode == fishingKey) FishingKeyPressed();
        }

        void FishingKeyPressed()
        {
            if (IsEntityInFishingArea(Game.Player.Character) || IsPlayerNearBoat(Game.Player) && CanPlayerFish(Game.Player))
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
            else if (IsEntityInSellingArea(Game.Player.Character) && CanPlayerFish(Game.Player))
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
                CleanUpEntities();
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
            if (postAction != null) // perform post action if there is one
            {
                postAction();
                postAction = null;
            }
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
            if (DebugMode) secondsToCatchFish = 1;
            else secondsToCatchFish = waitTime + rng.Next(10);
            UI.ShowSubtitle("Wait for it...", 5000);
            isFishing = true;
            minigameTimer = 0; // reset timer

            // show credits
            if (!creditsShown)
            {
                UI.Notify(Encoding.ASCII.GetString(creditsBytes1) + _SCRIPT_VERSION + Encoding.ASCII.GetString(creditsBytes2));
                UI.Notify("Work In Progress. Final version may differ.");
                creditsShown = true;
            }
        }

        void UpdateMinigame()
        {
            minigameTimer += Game.LastFrameTime;
            if (minigameTimer > secondsToCatchFish)
            {
                minigameTimer = 0;
                // Give player rewards
                int rNumber = rng.Next(100);
                if (rNumber < chanceNothing) // getting nothing
                {
                    UI.ShowSubtitle("You didn't catch anything.");
                }
                else if (rNumber < (DebugMode ? 100 : chanceNothing + chanceJunk))  // getting special items
                {
                    FishItem caughtItem;
                    if (DebugMode)
                    {
                        DebugIndex = (DebugIndex + 1) % SpecialItems.Length;
                        caughtItem = SpecialItems[DebugIndex];
                    }
                    else
                    {
                        caughtItem = ItemPicker.PickFromItems(SpecialItems);
                    }
                    spawnedEntities.Add(caughtItem.Spawn());
                    UI.ShowSubtitle("You've caught a " + caughtItem.Name, 5000);
                }
                else // caught regular fish
                {
                    Fish caughtFish = ItemPicker.PickFromFishes(NormalFishes);
                    bool added = inventory.AddFish(caughtFish);
                    spawnedEntities.Add(caughtFish.Spawn());
                    UI.ShowSubtitle("You've caught a " + caughtFish.Name + ", worth ~g~$" + caughtFish.Price 
                        + (added ? "" : "\n~r~But your backpack is full!"), 5000);
                }
                StopFishing();
            }
        }

        void SetupAvailableItems()
        {
            NormalFishes = new Fish[] 
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
            SpecialItems = new FishItem[]
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
                    Rarity.Rare, new ItemAction(x => ((Ped)x).Task.FightAgainst(playerPed))),
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
                    Rarity.Common, delegate { playerPed.Armor = 100;}),
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
            entityCleanup = settings.GetValue("Config", "EntityCleanup", true);
            chanceNothing = settings.GetValue("Config", "ChanceNothing", 5);
            chanceJunk = settings.GetValue("Config", "ChanceJunk", 35);
            backpackSize = settings.GetValue("Config", "BackpackSize", 30);
            waitTime = settings.GetValue("Config", "WaitTime", 5);
        }

        void CleanUpEntities()
        {
            foreach (Entity ent in spawnedEntities)
            {
                if (ent != null && IsEntityInFishingArea(ent)) ent.Delete(); // delete entities in fishing area
            }
            spawnedEntities.Clear(); // clear entities list for performance
        }

        bool CanPlayerFish(Player player)
        {
            return (player != null && player.CanControlCharacter && player.IsAlive && player.Character != null && !player.Character.IsInVehicle());
        }

        bool IsPlayerNearBoat(Player player)
        {
            Vehicle veh = player.LastVehicle;
            return (veh != null && veh.Model.IsBoat && veh.IsInRangeOf(player.Character.Position, _FISHINGBOAT_RANGE));
        }

        bool IsEntityInFishingArea(Entity ent)
        {
            if (fishAnywhere) return true;
            else
            {
                for (int i = 0; i < fishingSpotPos.Length; i += 2)
                {
                    if (Function.Call<bool>(Hash.IS_ENTITY_IN_AREA, ent, fishingSpotPos[i].X, fishingSpotPos[i].Y, fishingSpotPos[i].Z,
                        fishingSpotPos[i + 1].X, fishingSpotPos[i + 1].Y, fishingSpotPos[i + 1].Z, true, true, true)) return true;
                }
                return false;
            }
        }

        bool IsEntityInSellingArea(Entity ent)
        {
            return ent.IsInRangeOf(sellingSpotPos, _SELLINGSPOT_RANGE);
        }

        public static void PostActionQueue(Action action)
        {
            postAction = action;
        }

        public static void PostAnimation(string animBase, string animName, float speed, int duration, bool lastAnim, float playbackRate)
        {
            postAction = delegate
            {
                Game.Player.Character.Task.PlayAnimation(animBase, animName, speed, duration, lastAnim, playbackRate);
            };
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
        Unique = 3,
        Rare = 4,
        Uncommon = 5,
        Common = 6,
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

        public Entity Spawn()
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
            return ent;
        }
    }

    public class Fish : FishItem
    {
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
            :base(name, pedHashes, rarity, velocityMultiplier, action)
        {
            Price = price;
        }
    }

    public class PlayerInventory
    {
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
    }

    public static class ItemActions
    {
        public static void HealPlayer(Entity ent)
        {
            Game.Player.Character.Health = Game.Player.Character.MaxHealth;
        }

        public static void ClearPlayerWantedLevel(Entity ent)
        {
            Game.Player.WantedLevel = 0;
        }

        public static void KillPed(Entity ent)
        {
            if ((Ped)ent != null)
            {
                ((Ped)ent).Kill();
            }
        }

        public static void ShootTaserBullet(Entity ent)
        {
            //void SHOOT_SINGLE_BULLET_BETWEEN_COORDS(float x1, float y1, float z1, float x2, float y2, float z2, 
            //int damage, BOOL p7, Hash weaponHash, Ped ownerPed, BOOL p10, BOOL p11, float speed) // 867654CBC7606F2C CB7415AC
            Vector3 shootTo = Game.Player.Character.Position;
            Vector3 shootFrom = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 1f;
            Model stunGunModel = new Model(WeaponHash.StunGun);
            Ped attacker = World.CreatePed(new Model(PedHash.Fish), shootFrom);
            Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS, shootFrom.X, shootFrom.Y, shootFrom.Z, shootTo.X, shootTo.Y, shootTo.Z,
                0, true, stunGunModel.Hash, attacker, true, true, 1f);
            attacker.Delete();
        }
    }

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
