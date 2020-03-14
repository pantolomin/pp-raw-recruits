using Harmony;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.Characters;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPointModLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace pantolomin.phoenixPoint.mod.ppRawRecruits
{
    public class Mod: IPhoenixPointMod
    {
        private const string FILE_NAME = "pp-raw-recruits.properties";
        private const string CanHaveAugmentations = "CanHaveAugmentations";
        private const string HasArmor = "HasArmor";
        private const string HasConsumableItems = "HasConsumableItems";
        private const string HasInventoryItems = "HasInventoryItems";
        private const string HasWeapons = "HasWeapons";
        private static Dictionary<string, string> generationProperties = new Dictionary<string, string>();

        private const string TAG_ARMOUR_ITEM = "ArmourItem_TagDef";

        private static bool isGeneratingHavenRecruit;

        public ModLoadPriority Priority => ModLoadPriority.Low;

        public void Initialize()
        {
            string manifestDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? throw new InvalidOperationException("Could not determine operating directory. Is your folder structure correct? " +
                "Try verifying game files in the Epic Games Launcher, if you're using it.");

            string filePath = manifestDirectory + "/" + FILE_NAME;
            if (File.Exists(filePath)) {
                try
                {
                    foreach (string row in File.ReadAllLines(filePath))
                    {
                        if (row.StartsWith("#")) continue;
                        string[] data = row.Split('=');
                        if (data.Length == 2)
                        {
                            generationProperties.Add(data[0].Trim(), data[1].Trim());
                        }
                    }
                }
                catch (Exception e)
                {
                    FileLog.Log(string.Concat("Failed to read the configuration file (", filePath, "):", e.ToString()));
                }
            }
            HarmonyInstance harmonyInstance = HarmonyInstance.Create(typeof(Mod).Namespace);
            Patch(harmonyInstance, typeof(FactionCharacterGenerator), "GenerateHavenRecruit", null, "Pre_GenerateHavenRecruit", "Post_GenerateHavenRecruit");
            Patch(harmonyInstance, typeof(CharacterEquipmentGenerator), "FillWithFactionEquipment",
                new Type[] { typeof(GeoCharacter), typeof(GeoFaction), typeof(int), typeof(CharacterGenerationParams) },
                "Pre_FillWithFactionEquipment");
            Patch(harmonyInstance, typeof(CharacterEquipmentGenerator), "GetAvailableItemsForCharacter",
                new Type[] { typeof(GeoCharacter), typeof(IEnumerable<TacticalItemDef>), typeof(CharacterGenerationParams) },
                null, "Post_GetAvailableItemsForCharacter");
        }

        // ******************************************************************************************************************
        // ******************************************************************************************************************
        // Patched methods
        // ******************************************************************************************************************
        // ******************************************************************************************************************

        public static void Pre_GenerateHavenRecruit()
        {
            isGeneratingHavenRecruit = true;
        }

        public static void Post_GenerateHavenRecruit()
        {
            isGeneratingHavenRecruit = false;
        }

        public static void Pre_FillWithFactionEquipment(ref CharacterGenerationParams generationParams)
        {
            if (isGeneratingHavenRecruit && generationParams != null)
            {
                CharacterGenerationParams newGenerationParams = new CharacterGenerationParams();
                newGenerationParams.CanHaveAugmentations = getValue(CanHaveAugmentations, bool.Parse, false);
                newGenerationParams.HasArmor = getValue(HasArmor, bool.Parse, false);
                newGenerationParams.HasConsumableItems = getValue(HasConsumableItems, bool.Parse, false);
                newGenerationParams.HasInventoryItems = getValue(HasInventoryItems, bool.Parse, false);
                newGenerationParams.HasWeapons = getValue(HasWeapons, bool.Parse, false);
                newGenerationParams.EnduranceBonus = generationParams.EnduranceBonus;
                newGenerationParams.WillBonus = generationParams.WillBonus;
                newGenerationParams.SpeedBonus = generationParams.SpeedBonus;
                generationParams = newGenerationParams;
            }
        }

        public static void Post_GetAvailableItemsForCharacter(List<TacticalItemDef> __result, GeoCharacter character, CharacterGenerationParams generationParams)
        {
            if (isGeneratingHavenRecruit)
            {
                bool isHuman = character.ClassDef.IsHuman;
                if (isHuman && generationParams != null && !generationParams.HasArmor)
                {
                    __result.RemoveAll((TacticalItemDef i) => i.Tags.Where((GameTagDef tag) => TAG_ARMOUR_ITEM.Equals(tag.name)).Count() > 0);
                }
            }
        }

        private static T getValue<T>(string key, Func<string, T> mapper, T defaultValue)
        {
            string propertyValue;
            if (generationProperties.TryGetValue(key, out propertyValue))
            {
                try
                {
                    return mapper.Invoke(propertyValue);
                } catch (Exception)
                {
                    FileLog.Log(string.Concat("Failed to parse value for key ", key, ": ", propertyValue));
                }
            }
            return defaultValue;
        }

        // ******************************************************************************************************************
        // ******************************************************************************************************************
        // Utility methods
        // ******************************************************************************************************************
        // ******************************************************************************************************************

        private void Patch(HarmonyInstance harmony, Type target, string toPatch, Type[] types, string prefix, string postfix = null)
        {
            MethodInfo original = types != null
                ? target.GetMethod(
                    toPatch,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    types,
                    null)
                : target.GetMethod(toPatch, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (original == null)
            {
                FileLog.Log("Failed to get method " + target.Name + "." + toPatch);
                foreach (MethodInfo method in target.GetRuntimeMethods())
                {
                    if (method.Name.Equals(toPatch))
                    {
                        if (types == null)
                        {
                            original = method;
                            break;
                        }
                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length == types.Length)
                        {
                            bool matches = true;
                            for (int i=0; i< types.Length; i++)
                            {
                                if (!parameters[i].ParameterType.Equals(types[i]))
                                {
                                    FileLog.Log("Parameter difference (" + i + "): " + parameters[i].ParameterType + " != " + types[i]);
                                    matches = false;
                                    break;
                                }
                            }
                            if (matches)
                            {
                                original = method;
                                break;
                            }
                        }
                    }
                }
            }
            harmony.Patch(original, ToHarmonyMethod(prefix), ToHarmonyMethod(postfix), null);
        }

        private HarmonyMethod ToHarmonyMethod(string name)
        {
            if (name == null)
            {
                return null;
            }
            MethodInfo method = typeof(Mod).GetMethod(name);
            if (method == null)
            {
                throw new NullReferenceException(string.Concat("No method for name: ", name));
            }
            return new HarmonyMethod(method);
        }
    }
}
