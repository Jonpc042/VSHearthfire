using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;

namespace VSHearthfire.BlockEntities
{
    public class BEDryingRack : BlockEntityDisplay
    {
        // --- Timer variables ---
        private double lastTickTotalHours;
        private readonly double dryingTimeHours = 12; // 12 in-game hours to dry

        // --- Inventory ---
        public override InventoryBase Inventory => inventory;
        InventoryGeneric inventory = null!;
        private readonly int slotCount = 4;

        // REQUIRED: identify inventory class name for serialization
        public override string InventoryClassName => "dryingrack";

        // --- Recipe dictionary: input -> dried output ---
        private static readonly Dictionary<AssetLocation, AssetLocation> dryingRecipes = new()
        {

            // Golden poppy
            { new AssetLocation("game:flower-goldenpoppy-free"), new AssetLocation("hearthfire:driedgoldenpoppy") },
            { new AssetLocation("game:flower-goldenpoppy"), new AssetLocation("hearthfire:driedgoldenpoppy") },

            // Other vanilla flowers (both normal and -free variants)
            { new AssetLocation("game:flower-catmint-free"), new AssetLocation("hearthfire:driedcatmint") },
            { new AssetLocation("game:flower-catmint"), new AssetLocation("hearthfire:driedcatmint") },

            { new AssetLocation("game:flower-cornflower-free"), new AssetLocation("hearthfire:driedcornflower") },
            { new AssetLocation("game:flower-cornflower"), new AssetLocation("hearthfire:driedcornflower") },

            { new AssetLocation("game:flower-forgetmenot-free"), new AssetLocation("hearthfire:driedforgetmenot") },
            { new AssetLocation("game:flower-forgetmenot"), new AssetLocation("hearthfire:driedforgetmenot") },

            { new AssetLocation("game:flower-lilyofthevalley-free"), new AssetLocation("hearthfire:driedlilyofthevalley") },
            { new AssetLocation("game:flower-lilyofthevalley"), new AssetLocation("hearthfire:driedlilyofthevalley") },

            { new AssetLocation("game:flower-edelweiss-free"), new AssetLocation("hearthfire:driededelweiss") },
            { new AssetLocation("game:flower-edelweiss"), new AssetLocation("hearthfire:driededelweiss") },

            { new AssetLocation("game:flower-heather-free"), new AssetLocation("hearthfire:driedheather") },
            { new AssetLocation("game:flower-heather"), new AssetLocation("hearthfire:driedheather") },

            { new AssetLocation("game:flower-horsetail-free"), new AssetLocation("hearthfire:driedhorsetail") },
            { new AssetLocation("game:flower-horsetail"), new AssetLocation("hearthfire:driedhorsetail") },

            { new AssetLocation("game:flower-orangemallow-free"), new AssetLocation("hearthfire:driedorangemallow") },
            { new AssetLocation("game:flower-orangemallow"), new AssetLocation("hearthfire:driedorangemallow") },

            { new AssetLocation("game:flower-wilddaisy-free"), new AssetLocation("hearthfire:driedwilddaisy") },
            { new AssetLocation("game:flower-wilddaisy"), new AssetLocation("hearthfire:driedwilddaisy") },

            { new AssetLocation("game:flower-westerngorse-free"), new AssetLocation("hearthfire:driedwesterngorse") },
            { new AssetLocation("game:flower-westerngorse"), new AssetLocation("hearthfire:driedwesterngorse") },

            { new AssetLocation("game:flower-cowparsley-free"), new AssetLocation("hearthfire:driedcowparsley") },
            { new AssetLocation("game:flower-cowparsley"), new AssetLocation("hearthfire:driedcowparsley") },

            { new AssetLocation("game:flower-woad-free"), new AssetLocation("hearthfire:driedwoad") },
            { new AssetLocation("game:flower-woad"), new AssetLocation("hearthfire:driedwoad") },

            { new AssetLocation("game:flower-redtopgrass-free"), new AssetLocation("hearthfire:driedredtopgrass") },
            { new AssetLocation("game:flower-redtopgrass"), new AssetLocation("hearthfire:driedredtopgrass") }
        };

        // --- Constructor ---
        public BEDryingRack()
        {
            inventory = new InventoryGeneric(slotCount, null, null);
        }

        // --- Initialization ---
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (api is ICoreServerAPI)
            {
                RegisterGameTickListener(OnTick, 2000); // Tick every 2 seconds on server
            }

            lastTickTotalHours = api.World.Calendar.TotalHours;
        }

        // --- Drying Timer Logic ---
        public void OnTick(float dt)
        {
            double totalHours = Api.World.Calendar.TotalHours;
            double hoursPassed = totalHours - lastTickTotalHours;
            lastTickTotalHours = totalHours;

            for (int i = 0; i < slotCount; i++)
            {
                ItemSlot slot = inventory[i];
                if (slot.Empty) continue;

                AssetLocation inputCode = slot.Itemstack.Collectible.Code;
                AssetLocation outputCode = null;

                // Use recipe dictionary
                if (!dryingRecipes.TryGetValue(inputCode, out outputCode))
                {
                    continue;
                }

                float progress = slot.Itemstack.Attributes.GetFloat("transitionstate", 0f);
                progress += (float)(hoursPassed / dryingTimeHours);

                if (progress >= 1f)
                {
                    // Timer finished: Swap the item
                    Item driedItem = Api.World.GetItem(outputCode);
                    if (driedItem != null)
                    {
                        slot.Itemstack = new ItemStack(driedItem);
                        slot.MarkDirty();
                    }
                }
                else
                {
                    // Timer not finished: Save progress
                    slot.Itemstack.Attributes.SetFloat("transitionstate", progress);
                    slot.MarkDirty();
                }
            }
        }

        // --- Helper method to check for valid inputs ---
        private bool IsValidDryingItem(ItemSlot slot)
        {
            if (slot.Empty) return false;
            AssetLocation code = slot.Itemstack.Collectible.Code;

            if (dryingRecipes.ContainsKey(code)) return true;

            // Debug: notify what code was rejected (server log / chat)
            Api.World.Logger.Notification($"DryingRack: rejected item {code}");
            return false;
        }

        // --- Player Interaction ---
        public bool OnPlayerInteract(IPlayer byPlayer)
        {
            // Client should not perform authoritative inventory changes - short-circuit to avoid UI desync.
            if (Api.Side == EnumAppSide.Client) return true;

            ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (hotbarSlot.Empty)
            {
                // Hand is empty: Try to take an item
                if (TryTake(byPlayer))
                {
                    MarkDirty(true);
                    return true;
                }
            }
            else
            {
                // Hand has item: Try to place an item
                if (TryPut(byPlayer, hotbarSlot))
                {
                    MarkDirty(true);
                    return true;
                }
            }
            return false;
        }

        private bool TryPut(IPlayer byPlayer, ItemSlot hotbarSlot)
        {
            // Ensure server-side authoritative changes
            if (Api.Side != EnumAppSide.Server) return false;

            if (!IsValidDryingItem(hotbarSlot))
            {
                return false;
            }

            int quantity = 1;
            for (int i = 0; i < slotCount; i++)
            {
                if (inventory[i].Empty)
                {
                    int moved = hotbarSlot.TryPutInto(Api.World, inventory[i], quantity);
                    if (moved > 0)
                    {
                        // ensure the newly placed stack starts from 0 progress
                        inventory[i].Itemstack.Attributes.SetFloat("transitionstate", 0f);

                        Api.World.PlaySoundAt(new AssetLocation("game:sounds/block/cloth"), byPlayer.Entity, byPlayer, true, 16);
                        return true;
                    }
                    else
                    {
                        // debug if TryPutInto failed despite IsValidDryingItem
                        Api.World.Logger.Notification($"DryingRack: TryPutInto moved 0 of {hotbarSlot.Itemstack?.Collectible?.Code}");
                    }
                }
            }
            return false;
        }

        private bool TryTake(IPlayer byPlayer)
        {
            // Ensure server-side authoritative changes
            if (Api.Side != EnumAppSide.Server) return false;

            for (int i = slotCount - 1; i >= 0; i--)
            {
                if (!inventory[i].Empty)
                {
                    ItemStack stack = inventory[i].TakeOut(1);
                    if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
                    {
                        Api.World.SpawnItemEntity(stack, byPlayer.Entity.Pos.XYZ.Add(0, 0.5, 0));
                    }
                    Api.World.PlaySoundAt(new AssetLocation("game:sounds/block/cloth"), byPlayer.Entity, byPlayer, true, 16);
                    return true;
                }
            }
            return false;
        }

        // --- Display & Rendering ---
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine("");
            for (int i = 0; i < slotCount; i++)
            {
                dsc.AppendLine(inventory[i].Empty ? "Empty" : inventory[i].GetStackName());
            }
        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] R = new float[slotCount][];
            for (int i = 0; i < slotCount; i++)
            {
                R[i] = new float[] {
                    (i % 2 == 0) ? 0.2f : 0.7f,
                    (i < 2) ? 0.125f : 0.625f,
                    0.125f
                };
            }
            return R;
        }

        // --- Save/Load Logic ---
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            inventory.ToTreeAttributes(tree);
            tree.SetDouble("lastTickTotalHours", lastTickTotalHours);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);
            // InventoryGeneric.FromTreeAttributes expects only an ITreeAttribute
            inventory.FromTreeAttributes(tree);
            lastTickTotalHours = tree.GetDouble("lastTickTotalHours", worldForResolve.Calendar.TotalHours);
        }
    }
}