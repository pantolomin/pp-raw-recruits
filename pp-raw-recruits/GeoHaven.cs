using Harmony;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Geoscape.Entities;

namespace pantolomin.phoenixPoint.mod.ppRawRecruits
{
    [HarmonyPatch(typeof(GeoHaven), "GetRecruitCost")]
    class GetRecruitCost
    {
        [HarmonyPostfix]
        private static void Postfix(ResourcePack ____baseRecruitCost, ref ResourcePack __result)
        {
            __result = ____baseRecruitCost * Mod.Config.costModifier;
        }
    }
}
