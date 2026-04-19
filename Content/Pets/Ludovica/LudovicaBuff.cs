using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Pets.Ludovica
{
    public class LudovicaBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        { // This method gets called every frame your buff is active on your player.
            bool unused = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<LudovicaProjectile>());
        }
    }
}
