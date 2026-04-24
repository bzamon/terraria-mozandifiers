using System.Collections.Generic;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class AttunedPrefix : WeaponPrefix
{
	public const int MaxResonanceStacks = 4;
	public const int ResonanceDecayTicks = 150;
	public const int ResonanceHitGainCooldownTicks = 12;
	public const float EmpoweredCastShootSpeedMultiplier = 1.15f;

	protected override float PrefixRollChance => 1.05f;
	protected override float PrefixValueMultiplier => 1.35f;

	public override PrefixCategory Category => PrefixCategory.Magic;

	public override bool CanRoll(Item item)
	{
		return IsMagicWeapon(item);
	}

	public override void SetStats(
		ref float damageMult,
		ref float knockbackMult,
		ref float useTimeMult,
		ref float scaleMult,
		ref float shootSpeedMult,
		ref float manaMult,
		ref int critBonus)
	{
		damageMult *= 0.84f;
		knockbackMult *= 0.9f;
		useTimeMult *= 0.88f;
		shootSpeedMult *= 1.1f;
		manaMult *= 0.75f;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}Attuned",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.AttunedTooltip",
				(int)((1f - 0.75f) * 100f)));

		yield return new TooltipLine(
			Mod,
			$"{Name}Resonance",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.ResonanceTooltip",
				MaxResonanceStacks,
				ResonanceDecayTicks / 60f));

		yield return new TooltipLine(
			Mod,
			$"{Name}EmpoweredCast",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.EmpoweredTooltip",
				(int)((EmpoweredCastShootSpeedMultiplier - 1f) * 100f)));
	}
}
