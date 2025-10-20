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
                return be.OnPlayerInteract(byPlayer);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}