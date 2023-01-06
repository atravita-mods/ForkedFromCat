﻿using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using System.Linq;
using HarmonyLib;

namespace CustomizableDeathPenalty
{
    public class ModEntry : Mod
    {
        private bool lastDialogueUp;
        private int numberOfSeenDialogues;
        private bool shouldHideInfoDialogueBox;
        private bool shouldHideLostItemsDialogueBox;
        private uint multiple = 30;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.lastDialogueUp = false;
            this.numberOfSeenDialogues = 0;
            var config = helper.ReadConfig<ModConfig>();
            this.shouldHideInfoDialogueBox = config.KeepItems && config.KeepMoney && config.RememberMineLevels;
            this.shouldHideLostItemsDialogueBox = config.KeepItems;

            PlayerStateManager.SetConfig(config);
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;

            DeathPatcher.Initialize(this.Monitor, config);
            var harmony = new Harmony(this.ModManifest.UniqueID);
            DeathPatcher.Apply(harmony);
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (e.IsMultipleOf(this.multiple)) // half second
            {
                //If the state is not saved, and the player has just died, save the player's state.
                if (PlayerStateManager.state == null && Game1.killScreen)
                {
                    this.numberOfSeenDialogues = 0;
                    this.lastDialogueUp = false;

                    PlayerStateManager.SaveState();
                    this.Monitor.Log(
                        $"Saved state! State: {PlayerStateManager.state.money} {PlayerStateManager.state.deepestMineLevel} {PlayerStateManager.state.lowestLevelReached} {PlayerStateManager.state.inventory.Count(item => item != null)}",
                        LogLevel.Trace);
                }
                //If the state has been saved and the player can move, reset the player's old state.
                else if (PlayerStateManager.state != null && Game1.CurrentEvent == null && Game1.player.CanMove)
                {
                    this.Monitor.Log(
                        $"Restoring state! Current State: {Game1.player.Money} {Game1.player.deepestMineLevel} {(Game1.mine == null ? -1 : MineShaft.lowestLevelReached)}  {Game1.player.Items.Count(item => item != null)}",
                        LogLevel.Trace);
                    this.Monitor.Log(
                        $"Saved State: {PlayerStateManager.state.money} {PlayerStateManager.state.deepestMineLevel} {PlayerStateManager.state.lowestLevelReached} {PlayerStateManager.state.inventory.Count(item => item != null)}",
                        LogLevel.Trace);
                    PlayerStateManager.LoadState();
                }

                //Count the number of dialogues that have appeared since the player died. Close the fourth box if all the information in is being reset.
                if (PlayerStateManager.state != null)
                {
                    if (Game1.dialogueUp && Game1.dialogueUp != this.lastDialogueUp)
                    {
                        this.multiple = 5;
                        this.numberOfSeenDialogues++;
                        this.Monitor.Log($"Dialogue changed! {this.numberOfSeenDialogues}", LogLevel.Trace);

                        if (this.shouldHideInfoDialogueBox && this.numberOfSeenDialogues > 2 && Game1.activeClickableMenu is DialogueBox dialogueBox && !dialogueBox.isPortraitBox())
                            dialogueBox.closeDialogue();
                    }

                    if (this.shouldHideLostItemsDialogueBox && this.numberOfSeenDialogues > 2 && Game1.activeClickableMenu is ItemListMenu)
                    {
                        //ItemListMenu.okClicked()
                        Game1.activeClickableMenu = null;
                        if (Game1.CurrentEvent != null)
                            ++Game1.CurrentEvent.CurrentCommand;
                        Game1.player.itemsLostLastDeath.Clear();
                        this.multiple = 30;
                    }
                }

                this.lastDialogueUp = Game1.dialogueUp;
            }
        }
    }
}
