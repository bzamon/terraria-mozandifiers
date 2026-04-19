using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Pets.Ludovica
{
    public class LudovicaItem : ModItem
    {
        // Names and descriptions of all ExamplePetX classes are defined using .hjson files in the Localization folder
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.DogWhistle); // Copy the Defaults of the Zephyr Fish Item.

            Item.shoot = ModContent.ProjectileType<LudovicaProjectile>(); // "Shoot" your pet projectile.
            Item.buffType = ModContent.BuffType<LudovicaBuff>(); // Apply buff upon usage of the Item.
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                player.AddBuff(Item.buffType, 3600);
            }
            return true;
        }

       
    }
}
