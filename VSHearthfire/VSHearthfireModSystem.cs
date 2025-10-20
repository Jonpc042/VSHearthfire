using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using VSHearthfire.Blocks;
using VSHearthfire.BlockEntities;

namespace VSHearthfire
{
    public class VSHearthfireModSystem : ModSystem
    {
        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            Mod.Logger.Notification("Hello from hearthfire mod: " + api.Side);
            base.Start(api);

            api.RegisterBlockClass("BlockDryingRack", typeof(BlockDryingRack));
            api.RegisterBlockEntityClass("BEDryingRack", typeof(BEDryingRack));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from hearthfire mod server side");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from template mod client side");
        }

    }
}
