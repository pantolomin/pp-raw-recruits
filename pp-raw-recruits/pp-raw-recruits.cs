using Harmony;
using PhoenixPoint.Common.Utils;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPointModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace pantolomin.phoenixPoint.mod.ppRawRecruits
{
    public class Mod: IPhoenixPointMod
    {
        private const string FILE_NAME = "Mods/pp-raw-recruits.properties";
        private const string CanHaveMutation = "CanHaveMutation";
        private const string HasArmor = "HasArmor";
        private const string HasConsumableItems = "HasConsumableItems";
        private const string HasInventoryItems = "HasInventoryItems";
        private const string HasWeapons = "HasWeapons";

        private static Dictionary<string, string> generationProperties = new Dictionary<string, string>();
        private static bool mustPreventHardcodedPriestHeadMutation = false;

        public ModLoadPriority Priority => ModLoadPriority.Low;

        public void Initialize()
        {
            try
            {
                foreach (string row in File.ReadAllLines(FILE_NAME))
                {
                    if (row.StartsWith("#")) continue;
                    string[] data = row.Split('=');
                    if (data.Length == 2)
                    {
                        generationProperties.Add(data[0].Trim(), data[1].Trim());
                    }
                }
            }
            catch (FileNotFoundException) { 
                // simply ignore
            }
            catch (Exception e)
            {
                FileLog.Log(string.Concat("Failed to read the configuration file (", FILE_NAME, "):", e.ToString()));
            }
            HarmonyInstance harmonyInstance = HarmonyInstance.Create(typeof(Mod).Namespace);
            Patch(harmonyInstance, typeof(FactionCharacterGenerator), "FillWithFactionEquipment", null, "Pre_FillWithFactionEquipment", "Post_FillWithFactionEquipment");
            Patch(harmonyInstance, typeof(FactionCharacterGenerator), "PickRandomMutation", null, "Pre_PickRandomMutation");
        }

        // ******************************************************************************************************************
        // ******************************************************************************************************************
        // Patched methods
        // ******************************************************************************************************************
        // ******************************************************************************************************************

        public static void Pre_FillWithFactionEquipment(ref CharacterGenerationParams generationParams)
        {
            if (generationParams != null)
            {
                mustPreventHardcodedPriestHeadMutation = true;
                CharacterGenerationParams newGenerationParams = new CharacterGenerationParams();
                newGenerationParams.CanHaveMutation = getValue(CanHaveMutation, bool.Parse, false);
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

        public static void Post_FillWithFactionEquipment()
        {
            mustPreventHardcodedPriestHeadMutation = false;
        }

        public static bool Pre_PickRandomMutation(ref TacticalItemDef __result)
        {
            if (mustPreventHardcodedPriestHeadMutation)
            {
                mustPreventHardcodedPriestHeadMutation = false;
                __result = null;
                return false;
            }
            return true;
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
