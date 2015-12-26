/*
 * Fishing Mod
 * Author: libertylocked
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
    public class FishingScript : Script
    {
        const int _BONE_LEFTHAND = 0x49D9;
        const float _MINIGAME_FRAMETIME = 0.034f;

        Keys fishingKey;
        int fishingButton;
        bool entityCleanup = true;
        int backpackSize = 30;
        int chanceNothing = 5, chanceJunk = 35, chanceRodBreak = 4;
        int waitTime = 5;

        UIText promtText = new UIText("", new Point(50, 50), 0.5f, Color.White);
        Prop fishingRod;
        LocationHelper loc;
        bool isFishing = false;
        Fish[] NormalFishes;
        FishItem[] SpecialItems;
        Ped playerPed;
        List<Entity> spawnedEntities;
        float minigameTimer = 0;
        int secondsToCatchFish = 0;
        Random rng;
        bool creditsShown = false;
        byte[] creditsBytes1, creditsBytes2;

        public static PlayerInventory inventory;
        static Action postAction = null; // action to be performed when fishing stopped

        public FishingScript()
        {
            SetupAvailableItems();
            ParseSettings();
            creditsBytes1 = new byte[] { 0x46,0x69,0x73,0x68,0x69,0x6E,0x67,0x20,0x7E,0x72,0x7E,0x76 };
            creditsBytes2 = new byte[] { 0x20,0x7E,0x73,0x7E,0x62,0x79,0x20,0x7E,0x62,0x7E,0x6C,0x69,0x62,0x65,0x72,0x74,0x79,0x6C,0x6F,0x63,0x6B,0x65,0x64 };
            inventory = new PlayerInventory(backpackSize);
            rng = new Random();
            spawnedEntities = new List<Entity>();
            loc = new LocationHelper();

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

                if ((loc.IsEntityInStoreArea(playerPed)))
                {
                    promtText.Caption = "Press " + fishingKey.ToString() + " to buy a fishing rod"
                        + "\nYou have " + inventory.FishingRodsCount + " fishing rods"
                        + "\nCost: $10";
                    promtText.Draw();
                }

                if ((loc.IsEntityInFishingArea(playerPed) || loc.IsPlayerNearBoat(Game.Player))
                    && !isFishing && !Globals.FishAnywhere)
                {
                    if (inventory.HasFishingRod)
                    {
                        promtText.Caption = "Press " + fishingKey.ToString() + " to fish"
                            + "\nFishing rods: " + inventory.FishingRodsCount
                            + "\nBackpack: " + inventory.CurrSize + "/" + inventory.MaxSize;
                    }
                    else
                    {
                        promtText.Caption = "You don't have a fishing rod.\nBuy one at a convenience store!";
                    }
                    promtText.Draw();
                }
                if (loc.IsEntityInSellingArea(playerPed) && !isFishing)
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
            // buy a fishing rod
            if (loc.IsEntityInStoreArea(playerPed) && Game.Player.Money >= 10)
            {
                Game.Player.Money -= 10;
                inventory.AddFishingRod(new FishingRod(10));
                UI.ShowSubtitle("You bought a regular fishing rod");
            }
            else if (inventory.HasFishingRod)
            {
                if (loc.IsEntityInSellingArea(Game.Player.Character) && CanPlayerFish(Game.Player))
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
                else if (loc.IsEntityInFishingArea(Game.Player.Character) || loc.IsPlayerNearBoat(Game.Player) && CanPlayerFish(Game.Player))
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

            // Chance of breaking fishing rod
            if (new Random().Next(100) < chanceRodBreak)
            {
                UI.Notify("~r~Your fishing rod broke!");
                inventory.RemoveFishingRod();
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
            if (Globals.DebugMode) secondsToCatchFish = 1;
            else secondsToCatchFish = waitTime + rng.Next(10);
            UI.ShowSubtitle("Wait for it...", 5000);
            isFishing = true;
            minigameTimer = 0; // reset timer

            // show credits
            if (!creditsShown)
            {
                UI.Notify(Encoding.ASCII.GetString(creditsBytes1) + Globals._SCRIPT_VERSION + Encoding.ASCII.GetString(creditsBytes2));
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
                else if (rNumber < (Globals.DebugMode ? 100 : chanceNothing + chanceJunk))  // getting special items
                {
                    FishItem caughtItem;
                    if (Globals.DebugMode)
                    {
                        Globals.DebugIndex = (Globals.DebugIndex + 1) % SpecialItems.Length;
                        caughtItem = SpecialItems[Globals.DebugIndex];
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
            { // count: 21
                new Fish("Arroyo Chub", 5, Rarity.Common),
                new Fish("Spotted Bay Bass", 10, Rarity.Common),
                new Fish("Blue Rockfish", 15, Rarity.Common),
                new Fish("Barred Sand Bass", 20, Rarity.Common),
                new Fish("Barred Surf Perch", 25, Rarity.Common),
                new Fish("Kelp Bass", 30, Rarity.Common),
                new Fish("White Sea Bass", 40, Rarity.Common),
                new Fish("Spotfin Croaker", 45, Rarity.Common),
                new Fish("California Halibut", 50, Rarity.Common),
                new Fish("Pacific Barracuda", 60, Rarity.Common),
                new Fish("Swordfish", 65, Rarity.Common),
                new Fish("California Corbina", 70, Rarity.Common),
                new Fish("Red Lionfish", 80, Rarity.Common),
                new Fish("Black Rockfish", 85, Rarity.Common),
                new Fish("American Sole", 90, Rarity.Common),
                new Fish("Yellowtail", 100, Rarity.Common),
                new Fish("Steelhead", 120, Rarity.Common),
                new Fish("Walleye Surfperch", 200, Rarity.Common),
                new Fish("Goldfish", 400, Rarity.Common),
                new Fish("Dolphin", 700, new PedHash[] { PedHash.Dolphin }, Rarity.Legendary, null),
                new Fish("Tiger Shark", 800, new PedHash[] { PedHash.TigerShark }, Rarity.Legendary, null),
            };

            SpecialItems = new FishItem[]
            { // count: 54
                // no model
                new FishItem("Condom", 
                    Rarity.Common, null),
                //new FishItem("test", new VehicleHash[]{ VehicleHash.Submersible2 },
                //    Rarity.Common, null),
                // peds
                new FishItem("Dead Hooker", new PedHash[]{PedHash.Hooker01SFY, PedHash.Hooker02SFY, PedHash.Hooker03SFY}, 
                    Rarity.Uncommon, ItemActions.KillPed),
                new FishItem("Dead Johnny", new PedHash[]{PedHash.JohnnyKlebitz},
                    Rarity.Uncommon, ItemActions.KillPed),
                new FishItem("Dead Cop", new PedHash[]{PedHash.Cop01SFY, PedHash.Cop01SMY},
                    Rarity.Uncommon, ItemActions.KillPed),
                new FishItem("Zombie", new PedHash[]{PedHash.Zombie01},
                    Rarity.Rare, new ItemAction((ent) => ((Ped)ent).Task.FightAgainst(Game.Player.Character))),
                new FishItem("Pigeon", new PedHash[]{PedHash.Pigeon}, 
                    Rarity.Uncommon, new Vector3(38f, 38f, 7f), null),
                new FishItem("Cat", new PedHash[]{PedHash.Cat}, 
                    Rarity.Uncommon, new Vector3(52f, 52f, 7f), ItemActions.CatEatsFish),
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
                new FishItem("Submersible", new VehicleHash[]{VehicleHash.Submersible, VehicleHash.Submersible2},
                    Rarity.Legendary, null),
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
                    Rarity.Common, new ItemAction((ent) => World.AddExplosion(ent.Position, ExplosionType.Propane, 1f, 1f))),
                new FishItem("Artifact", new string[]{"prop_artifact_01"},
                    Rarity.Rare, delegate { Game.Player.Money += 2000; }),
                new FishItem("Bowling Ball", new string[]{"prop_bowling_ball"},
                    Rarity.Common, null),
                new FishItem("Cleaver", new string[]{"prop_cleaver"},
                    Rarity.Common, null),
                new FishItem("Yoga Mat", new string[]{"prop_yoga_mat_01", "prop_yoga_mat_02", "prop_yoga_mat_03"},
                    Rarity.Common, null),
                new FishItem("10k Weight", new string[]{"prop_weight_10k"},
                    Rarity.Common, null),
                new FishItem("TV", new string[]{"prop_tv_02", "prop_tv_04", "prop_tv_05", "prop_tv_06", "prop_tv_07"},
                    Rarity.Common, ItemActions.ShootTaserBullet),
                new FishItem("Recycle Bin", new string[]{ "prop_recyclebin_02a" },
                    Rarity.Common, null),
                new FishItem("Road Cone", new string[]{"prop_roadcone01a", "prop_roadcone01b", "prop_roadcone01c", "prop_roadcone02a", "prop_roadcone02b", "prop_roadcone02c"},
                    Rarity.Common, null),
                new FishItem("Spy Cam", new string[]{"prop_spycam"},
                    Rarity.Common, null),
            };
        }

        void ParseSettings()
        {
            ScriptSettings settings = ScriptSettings.Load(@".\scripts\Fishing.ini");
            fishingKey = (Keys)Enum.Parse(typeof(Keys), settings.GetValue("Config", "FishingKey", "F5"), true);
            fishingButton = settings.GetValue("Config", "FishingButton", 234);
            Globals.FishAnywhere= settings.GetValue("Config", "FishAnywhere", false);
            entityCleanup = settings.GetValue("Config", "EntityCleanup", true);
            chanceNothing = settings.GetValue("Config", "ChanceNothing", 5);
            chanceJunk = settings.GetValue("Config", "ChanceJunk", 35);
            chanceRodBreak = settings.GetValue("Config", "ChanceRodBreak", 4);
            backpackSize = settings.GetValue("Config", "BackpackSize", 30);
            waitTime = settings.GetValue("Config", "WaitTime", 5);
        }

        void CleanUpEntities()
        {
            foreach (Entity ent in spawnedEntities)
            {
                if (ent != null && loc.IsEntityInFishingArea(ent)) ent.Delete(); // delete entities in fishing area
            }
            spawnedEntities.Clear(); // clear entities list for performance
        }

        bool CanPlayerFish(Player player)
        {
            return (player != null && player.CanControlCharacter && player.IsAlive && player.Character != null && !player.Character.IsInVehicle());
        }

        public static void PostActionQueue(Action action)
        {
            postAction = action;
        }

        public static void PostAnimation(string animBase, string animName, float speed, int duration, bool lastAnim, float playbackRate)
        {
            PostActionQueue(delegate
            {
                Game.Player.Character.Task.PlayAnimation(animBase, animName, speed, duration, lastAnim, playbackRate);
            });
        }
    }
}
