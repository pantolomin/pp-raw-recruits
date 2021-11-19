using Harmony;
using PhoenixPoint.Geoscape.Core;

namespace pantolomin.phoenixPoint.mod.ppRawRecruits
{
    [HarmonyPatch(typeof(FactionCharacterGenerator), "GenerateHavenRecruit")]
    class GenerateHavenRecruit
	{
        public static bool isGeneratingHavenRecruit;

        [HarmonyPrefix]
        private static void Prefix()
        {
            isGeneratingHavenRecruit = true;
		}

        [HarmonyPostfix]
        private static void Postfix()
        {
            isGeneratingHavenRecruit = false;
        }
    }

    [HarmonyPatch(typeof(FactionCharacterGenerator), "ApplyGenerationParameters")]
    class ApplyGenerationParameters
    {
        [HarmonyPrefix]
        private static void Prefix(ref CharacterGenerationParams generationParams)
        {
            if (GenerateHavenRecruit.isGeneratingHavenRecruit && generationParams != null)
            {
                CharacterGenerationParams newGenerationParams = new CharacterGenerationParams();
                newGenerationParams.CanHaveAugmentations = Mod.Config.canHaveAugmentations;
                newGenerationParams.HasArmor = Mod.Config.hasArmor;
                newGenerationParams.HasConsumableItems = Mod.Config.hasConsumableItems;
                newGenerationParams.HasInventoryItems = Mod.Config.hasInventoryItems;
                newGenerationParams.HasWeapons = Mod.Config.hasWeapons;
                newGenerationParams.EnduranceBonus = generationParams.EnduranceBonus;
                newGenerationParams.WillBonus = generationParams.WillBonus;
                newGenerationParams.SpeedBonus = generationParams.SpeedBonus;
                generationParams = newGenerationParams;
            }
        }
    }
}
