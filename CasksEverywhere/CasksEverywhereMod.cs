﻿using HarmonyLib;
using StardewModdingAPI;
using System.Reflection;

namespace CasksEverywhere
{
    public class CasksEverywhereMod : Mod
    {
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            var instance = new Harmony(helper.ModRegistry.ModID);

            instance.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
