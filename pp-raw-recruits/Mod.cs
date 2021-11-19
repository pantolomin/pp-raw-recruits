using Harmony;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace pantolomin.phoenixPoint.mod.ppRawRecruits
{
    public class Mod
    {
        internal static ModConfig Config;

        public static void Init()
        {
            new Mod().MainMod();
        }

        public void MainMod(Func<string, object, object> api = null)
        {
            Config = api("config", null) as ModConfig ?? new ModConfig();
            HarmonyInstance.Create("phoenixpoint.RawRecruits").PatchAll(Assembly.GetExecutingAssembly());
            api("log verbose", "Mod Initialised.");
        }
    }
}
