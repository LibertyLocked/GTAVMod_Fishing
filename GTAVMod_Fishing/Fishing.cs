using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
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
        Fish[] NormalFishes, SpecialFishes;

        const float MINIGAME_FRAMETIME = 0.034f;
        float minigameTimer = 0;
        int secondsToCatchFish = 0;
        Random rnd;
        //UIRectangle recBack, recPlayer;

        public Fishing()
        {
            NormalFishes = new Fish[] 
            {
                // normal fishes
                new Fish("Spotted Bay Bass", 10),
                new Fish("Barred Sand Bass", 20),
                new Fish("Kelp Bass", 30),
                new Fish("White Sea Bass", 40),
                new Fish("California Halibut", 50),
                new Fish("Pacific Barracuda", 60),
                new Fish("Yellowtail", 100),
                new Fish("Barred Surf Perch", 20),
                new Fish("Swordfish", 1),
                new Fish("Goldfish", 500),
            };
            SpecialFishes = new Fish[]
            {
                // special cases
                new Fish("Condom", 0),
                new Fish("Dead Hooker", 0),
                new Fish("Wallet", 0),
                new Fish("Bike", 0),
                new Fish("Pizza", 0),
                new Fish("Toilet", 0),
            };

            inventory = new PlayerInventory();
            rnd = new Random();

            //recBack = new UIRectangle(new Point(100, 600), new Size(1080, 50), Color.FromArgb(100, 255, 255, 255));
            //recPlayer = new UIRectangle(new Point(640 - 400 / 2, 600), new Size(400, 50), Color.FromArgb(100, 0, 255, 0));

            ScriptSettings settings = ScriptSettings.Load(@".\scripts\Fishing.ini");
            fishingKey = (Keys)Enum.Parse(typeof(Keys), settings.GetValue("Config", "FishingKey"), true);

            this.Tick += OnTick;
            this.KeyDown += OnKeyDown;
        }

        void OnTick(object sender, EventArgs e)
        {
            Player player = Game.Player;
            if (player != null && player.CanControlCharacter && player.IsAlive && player.Character != null)
            {
                if (IsPlayerInFishingArea(player.Character.Position) && !isFishing)
                {
                    promtText.Caption = "Press " + fishingKey.ToString() + " to fish";
                    promtText.Draw();
                }
                if (IsPlayerInSellingArea(player.Character.Position) && !isFishing)
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
                if (IsPlayerInFishingArea(Game.Player.Character.Position))
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
                else if (IsPlayerInSellingArea(Game.Player.Character.Position))
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
            Ped myPed = Game.Player.Character;
            myPed.Task.ClearAllImmediately();
            if (fishingRod != null)
            {
                fishingRod.Detach();
                fishingRod.Delete();
            }
            isFishing = false;
        }

        void StartFishing()
        {
            Ped myPed = Game.Player.Character;
            myPed.Task.ClearAllImmediately();
            myPed.Task.PlayAnimation("amb@world_human_stand_fishing@idle_a", "idle_c", 8f, -1, true, -1);
            if (fishingRod != null)
            {
                fishingRod.Detach();
                fishingRod.Delete();
            }
            fishingRod = World.CreateProp(new Model("prop_fishing_rod_01"), Vector3.Zero, false, true);
            fishingRod.AttachTo(myPed, myPed.PedBoneIndex(BONE_LEFTHAND), new Vector3(0.13f, 0.1f, 0.01f), new Vector3(-100f, 30f, 0f));
            secondsToCatchFish = 5 + rnd.Next(10);
            UI.ShowSubtitle("Wait for it...", 10000);
            isFishing = true;
        }

        void UpdateMinigame()
        {
            minigameTimer += Game.LastFrameTime;
            if (minigameTimer > secondsToCatchFish)
            {
                minigameTimer = 0;
                // Give player rewards
                int rNumber = rnd.Next(20);
                if (rNumber == 0) // 5% nothing
                {
                    UI.ShowSubtitle("You didn't catch anything.");
                }
                else if (rNumber <= 7)  // 35% getting special items
                {
                    int caughtIndex = rnd.Next(SpecialFishes.Length);
                    Fish caughtFish = SpecialFishes[caughtIndex];
                    if (caughtFish.Name == "Dead Hooker")
                    {
                        int rNum = rnd.Next(3);
                        PedHash hookerHash;
                        if (rNum == 0) hookerHash = PedHash.Hooker01SFY;
                        else if (rNum == 1) hookerHash = PedHash.Hooker02SFY;
                        else hookerHash = PedHash.Hooker03SFY;
                        Ped hooker = World.CreatePed(new Model(hookerHash), Game.Player.Character.Position);
                        hooker.Kill();
                    }
                    else if (caughtFish.Name == "Wallet")
                    {
                        int money = 100 + rnd.Next(900);
                        Game.Player.Money += money;
                    }
                    else if (caughtFish.Name == "Bike")
                    {
                        World.CreateVehicle(new Model(VehicleHash.Bmx), Game.Player.Character.Position);
                    }
                    else if (caughtFish.Name == "Condom")
                    {
                        World.CreateProp(new Model("prop_vend_condom_01"), Game.Player.Character.Position, true, false);
                    }
                    else if (caughtFish.Name == "Pizza")
                    {
                        World.CreateProp(new Model("prop_pizza_box_01"), Game.Player.Character.Position, true, false);
                        Game.Player.Character.Health = Game.Player.Character.MaxHealth;
                    }
                    else if (caughtFish.Name == "Toilet")
                    {
                        World.CreateProp(new Model("prop_toilet_01"), Game.Player.Character.Position, true, false);
                    }
                    UI.ShowSubtitle("You've caught a " + caughtFish.Name, 5000);
                }
                else
                {
                    Fish caughtFish = NormalFishes[rnd.Next(NormalFishes.Length)];
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

        bool IsPlayerInFishingArea(Vector3 playerPos)
        {
            return playerPos.DistanceTo(fishingSpotPos) <= FISHINGSPOT_RANGE;
        }

        bool IsPlayerInSellingArea(Vector3 playerPos)
        {
            return playerPos.DistanceTo(sellingSpotPos) <= SELLINGSPOT_RANGE;
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
