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
        ItemPopulator itemPop = new ItemPopulator();
        Ped playerPed;
        List<Entity> spawnedEntities;
        float minigameTimer = 0;
        int secondsToCatchFish = 0;
        Random rng;
        bool creditsShown = false;
        byte[] creditsBytes1, creditsBytes2;

        static Action postAction = null; // action to be performed when fishing stopped

        public FishingScript()
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
            if (IsEntityInSellingArea(Game.Player.Character) && CanPlayerFish(Game.Player))
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
            else if (IsEntityInFishingArea(Game.Player.Character) || IsPlayerNearBoat(Game.Player) && CanPlayerFish(Game.Player))
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
            itemPop.PopulateFishes(NormalFishes);
            itemPop.PopulateSpecialItems(SpecialItems);
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
}
