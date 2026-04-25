using Mozandifiers.Content.Pets.Amelia;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Mozandifiers.Common.Players;

public class StartingItemsPlayer : ModPlayer
{
	public override void OnEnterWorld()
	{
		// Keep the starter item restricted to fresh characters without an item in slot 0.
		if (Player.statLifeMax == 100 && Player.inventory[0].type == ItemID.None) {
			Player.QuickSpawnItem(
				Player.GetSource_GiftOrReward(),
				ModContent.ItemType<AmeliaItem>(),
				1);
		}
	}
}
