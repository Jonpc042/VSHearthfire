using Vintagestory.API.Common;
using Vintagestory.GameContent;
using VSHearthfire.BlockEntities;

namespace VSHearthfire.Blocks
{
    public class BlockDryingRack : BlockShelf
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEDryingRack be)
            {
                // Pass the BlockSelection so the BE can determine which selection box was targeted
                return be.OnPlayerInteract(byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}