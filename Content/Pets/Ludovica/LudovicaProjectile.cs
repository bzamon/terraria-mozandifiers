using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace Mozandifiers.Content.Pets.Ludovica
{
    public class LudovicaProjectile : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 11;
            Main.projPet[Type] = true;

            
        }



        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.Puppy); // Copy the Defaults of the Puppy Projectile.

            Projectile.width = 54;
            Projectile.height = 32;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.netImportant = true;

            AIType = ProjectileID.Puppy; // Mimic as the Puppy during AI.
        }

        public override bool PreAI()
        {
            Player player = Main.player[Projectile.owner];

            player.puppy = false; // Relic from AIType

            return true;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            // Keep the projectile from disappearing as long as the player isn't dead and has the pet buff.
            if (!player.dead && player.HasBuff(ModContent.BuffType<LudovicaBuff>()))
            {
                Projectile.timeLeft = 2;
            }
        }

    }
}
