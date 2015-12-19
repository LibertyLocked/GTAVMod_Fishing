using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTAVMod_Fishing
{
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

        public virtual Entity Spawn()
        {
            Entity ent = null;
            if (EntityType != EntityType.None)
            {
                Ped playerPed = Game.Player.Character;
                Random rng = new Random();
                Vector3 spawnPos = playerPed.Position + playerPed.ForwardVector * 20;
                Vector3 vel = playerPed.ForwardVector * -1;
                if (Globals.DebugMode) UI.Notify(Globals.DebugIndex + " " + Name);

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

                if (Globals.DebugMode)
                {
                    //UI.Notify("Pos " + ent.Position.ToString());
                    //UI.Notify("Vel " + ent.Velocity.ToString());
                }
            }

            if (action != null) action(ent);
            return ent;
        }
    }

}
