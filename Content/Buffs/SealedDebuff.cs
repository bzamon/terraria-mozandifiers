using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Buffs;

public sealed class SealedDebuff : ModBuff
{
	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.buffNoSave[Type] = true;
	}
}
