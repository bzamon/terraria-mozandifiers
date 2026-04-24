using Mozandifiers.Content.Pets.Amelia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Mozandifiers.Common.Players
{
    public class StartingItemsPlayer : ModPlayer
    {
        private bool receivedStarterItem;


        public override void OnEnterWorld()
        {
            if (Player.statLifeMax == 100 && Player.inventory[0].type == ItemID.None)
            {
                Player.QuickSpawnItem(Player.GetSource_GiftOrReward(),
                    ModContent.ItemType<AmeliaItem>(), 1);
            }
        }
    }
}
