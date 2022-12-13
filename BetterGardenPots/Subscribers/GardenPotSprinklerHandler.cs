﻿using BetterGardenPots.APIs;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace BetterGardenPots.Subscribers
{
    internal class GardenPotSprinklerHandler : IEventSubscriber
    {
        private ISimpleSprinklersAPI simpleSprinklersAPI;
        private IBetterSprinklersAPI betterSprinklersAPI;
        private IPrismaticToolsAPI prismaticToolsAPI;

        private readonly IModHelper helper;

        public GardenPotSprinklerHandler(IModHelper helper)
        {
            this.helper = helper;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }

        private bool isSubscribed;

        public void Subscribe()
        {
            if (this.isSubscribed)
                return;

            this.helper.Events.GameLoop.DayStarted += this.OnDayStarted;

            this.isSubscribed = false;
        }

        public void Unsubscribe()
        {
            if (!this.isSubscribed)
                return;

            this.helper.Events.GameLoop.DayStarted -= this.OnDayStarted;

            this.isSubscribed = true;
        }

        /// <summary>Raised after the game is launched, right before the first update tick.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, EventArgs e)
        {
            if (this.helper.ModRegistry.IsLoaded("tZed.SimpleSprinkler"))
            {
                this.simpleSprinklersAPI = this.helper.ModRegistry.GetApi<ISimpleSprinklersAPI>("tZed.SimpleSprinkler");
            }

            if (this.helper.ModRegistry.IsLoaded("Speeder.BetterSprinklers"))
            {
                this.betterSprinklersAPI = this.helper.ModRegistry.GetApi<IBetterSprinklersAPI>("Speeder.BetterSprinklers");
            }

            if (this.helper.ModRegistry.IsLoaded("stokastic.PrismaticTools"))
            {
                this.prismaticToolsAPI = this.helper.ModRegistry.GetApi<IPrismaticToolsAPI>("stokastic.PrismaticTools");
            }
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, EventArgs e)
        {
            foreach (GameLocation location in GetMainAndInnerLocations(Game1.locations))
                foreach (Vector2 wateredTile in this.GetWateredTiles(location))
                {
                    if (location.Objects.TryGetValue(wateredTile, out SObject item) && item is IndoorPot pot && pot.hoeDirt.Value != null)
                    {
                        pot.hoeDirt.Value.state.Value = 1;
                        pot.showNextIndex.Value = true;
                    }
                }
        }

        private static IEnumerable<GameLocation> GetMainAndInnerLocations(IEnumerable<GameLocation> locations)
        {
            foreach (GameLocation loc in locations)
            {
                yield return loc;
                if (loc is BuildableGameLocation buildableLoc)
                    foreach (GameLocation l in buildableLoc.buildings.Select(item => item.indoors.Value))
                        yield return l;
            }
        }

        private IEnumerable<Vector2> GetWateredTiles(GameLocation location)
        {
            if (location == null)
                yield break;

            foreach (KeyValuePair<Vector2, SObject> item in location.Objects.Pairs)
                if (item.Value.IsSprinkler())
                    foreach (Vector2 pos in this.GetRange(item.Value, item.Key))
                        yield return pos;
        }

        public IEnumerable<Vector2> GetRange(SObject obj, Vector2 position)
        {
            if (this.prismaticToolsAPI != null && obj.ParentSheetIndex == this.prismaticToolsAPI.SprinklerIndex)
                foreach (Vector2 pos in this.prismaticToolsAPI.GetSprinklerCoverage(position))
                    yield return pos;
            else
            {
                if (this.betterSprinklersAPI == null)
                {
                    foreach (Vector2 pos in obj.GetSprinklerTiles())
                        yield return pos;
                }
                else if (this.betterSprinklersAPI.GetSprinklerCoverage().TryGetValue(obj.ParentSheetIndex, out Vector2[] bExtra))
                    foreach (Vector2 extraPos in bExtra)
                        yield return extraPos + position;

                if (this.simpleSprinklersAPI != null && this.simpleSprinklersAPI.GetNewSprinklerCoverage().TryGetValue(obj.ParentSheetIndex, out Vector2[] sExtra))
                    foreach (Vector2 extraPos in sExtra)
                        yield return extraPos + position;
            }
        }
    }
}
