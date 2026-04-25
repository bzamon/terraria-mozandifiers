using Terraria;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Buffs;

public sealed class AwakenedBuff : ModBuff
{
	public override void SetStaticDefaults()
	{
		Main.buffNoSave[Type] = true;
	}
}
