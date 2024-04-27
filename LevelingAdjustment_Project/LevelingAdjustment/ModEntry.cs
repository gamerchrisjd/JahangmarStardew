// Copyright (c) 2019 Jahangmar
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

using System.Collections.Generic;

namespace LevelingAdjustment
{
    public class ModEntry : Mod
    {
        public const int SKILL_COUNT = 5; //without luck
        public const int FARMING_SKILL = 0;
        public const int MINING_SKILL = 3;
        public const int FISHING_SKILL = 1;
        public const int FORAGING_SKILL = 2;
        public const int LUCK_SKILL = 5;
        public const int COMBAT_SKILL = 4;
        public const int MASTERY_SKILL = 6;

        private int[] oldExperiencePoints;
        private int[] oldLevels;
        private LevelingConfig conf;

        private List<ExpAnimation> expAnimations = new List<ExpAnimation>();

        public override void Entry(IModHelper helper)
        {
            conf = Helper.ReadConfig<LevelingConfig>();

            if (conf.combatExperienceFactor < 0 || conf.farmingExperienceFactor < 0 || conf.fishingExperienceFactor < 0 || conf.foragingExperienceFactor < 0 || conf.miningExperienceFactor < 0 || conf.generalExperienceFactor < 0 || conf.combatMasteryExperienceFactor < 0 || conf.farmingMasteryExperienceFactor < 0 || conf.fishingMasteryExperienceFactor < 0 || conf.foragingMasteryExperienceFactor < 0 || conf.miningMasteryExperienceFactor < 0 || conf.generalMasteryExperienceFactor < 0)
            {
                Monitor.Log("ExperienceFactors in config.json must be at least 0", LogLevel.Error);
                Monitor.Log("Deactivating mod", LogLevel.Error);
                return;
            }

            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            Helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;

            Helper.Events.Display.RenderedWorld += Display_RenderedWorld;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            /*
            Helper.ConsoleCommands.Add("setexp", "", HandleSetExp);
            Helper.ConsoleCommands.Add("resetlevels", "", (arg, args) =>
            {
                for (int skill = 0; skill < SKILL_COUNT; skill++)
                {
                    int exp = Game1.player.experiencePoints[skill];
                    SetLevel(skill, exp);
                }
            });
            */
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.conf = new LevelingConfig(),
                save: () => this.Helper.WriteConfig(this.conf)
            );

            configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => "Experience Notification",
            tooltip: () => "Check this box to see Experience point gains displayed above your character's head whenever gaining experience. Switches to displaying Mastery point gains once all skills are at level 10",
            getValue: () => this.conf.expNotification,
            setValue: value => this.conf.expNotification = value
            );

            configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => "Round Partial Experience Points",
            tooltip: () => "By default (when this setting is disabled/unchecked), if the Experience/Mastery Factor value would result in partial XP/Mastery points for an action, that action will give a % chance to gain a whole XP/Mastery point to accurately reflect the chosen Experience/Mastery Factor on average in all situations. If enabled, partial XP/Mastery points are always rounded to the nearest whole number, with a minimum of 1 XP if it would otherwise round to 0 XP. E.G. An action giving 1.6 XP points becomes 1 XP point with a 60% chance for an additional 1 XP point with this setting disabled, or if enabled is always rounded to 2 XP points.",
            getValue: () => this.conf.roundPartialExp,
            setValue: value => this.conf.roundPartialExp = value
            );

            configMenu.AddSectionTitle(
            mod: this.ModManifest,
            text: () => "Skill Experience",
            tooltip: () => "The below factors affect normal skill experience gains"
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "General Experience Factor",
            tooltip: () => "All experience gains for any skill are multiplied by this value. Stacks multiplicatively with the individual Skill Experience Factors below",
            getValue: () => (float)this.conf.generalExperienceFactor,
            setValue: value => this.conf.generalExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Farming Experience Factor",
            tooltip: () => "All Farming skill experience gains are multiplied by this value. Stacks multiplicatively with the General Experience Factor above",
            getValue: () => (float)this.conf.farmingExperienceFactor,
            setValue: value => this.conf.farmingExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Foraging Experience Factor",
            tooltip: () => "All Foraging skill experience gains are multiplied by this value. Stacks multiplicatively with the General Experience Factor above",
            getValue: () => (float)this.conf.foragingExperienceFactor,
            setValue: value => this.conf.foragingExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Fishing Experience Factor",
            tooltip: () => "All Fishing skill experience gains are multiplied by this value. Stacks multiplicatively with the General Experience Factor above",
            getValue: () => (float)this.conf.fishingExperienceFactor,
            setValue: value => this.conf.fishingExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Mining Experience Factor",
            tooltip: () => "All Mining skill experience gains are multiplied by this value. Stacks multiplicatively with the General Experience Factor above",
            getValue: () => (float)this.conf.miningExperienceFactor,
            setValue: value => this.conf.miningExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Combat Experience Factor",
            tooltip: () => "All Combat skill experience gains are multiplied by this value. Stacks multiplicatively with the General Experience Factor above",
            getValue: () => (float)this.conf.combatExperienceFactor,
            setValue: value => this.conf.combatExperienceFactor = value
            );

            configMenu.AddSectionTitle(
            mod: this.ModManifest,
            text: () => "Mastery Points",
            tooltip: () => "In Vanilla once all skills are at level 10, every 1 Experience Point earned in any skill is converted into 1 Mastery Point (except for Farming). The below factors affect Mastery point gains, replacing the vanilla Mastery Point Factors (in Vanilla all Mastery Point Factors are 1, except Farming which is 0.5). Because Mastery Point gains come from Experience Point gains, the final total Mastery Points you earn from an action depend on both the Mastery Factors and Experience Factors you have set."
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "General Mastery Factor",
            tooltip: () => "All Mastery point gains from any skill are multiplied by this value. Stacks multiplicatively with the individual Skill Mastery Factors below",
            getValue: () => (float)this.conf.generalMasteryExperienceFactor,
            setValue: value => this.conf.generalMasteryExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Farming Mastery Factor",
            tooltip: () => "All Mastery point gains from Farming Experience are multiplied by this value. Stacks multiplicatively with the General Mastery Factor above. (Farming Mastery point gains are multiplied by 0.5 in Vanilla)",
            getValue: () => (float)this.conf.farmingMasteryExperienceFactor,
            setValue: value => this.conf.farmingMasteryExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Foraging Mastery Factor",
            tooltip: () => "All Mastery point gains from Foraging Experience are multiplied by this value. Stacks multiplicatively with the General Mastery Factor above",
            getValue: () => (float)this.conf.foragingMasteryExperienceFactor,
            setValue: value => this.conf.foragingMasteryExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Fishing Mastery Factor",
            tooltip: () => "All Mastery point gains from Fishing Experience are multiplied by this value. Stacks multiplicatively with the General Mastery Factor above",
            getValue: () => (float)this.conf.fishingMasteryExperienceFactor,
            setValue: value => this.conf.fishingMasteryExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Mining Mastery Factor",
            tooltip: () => "All Mastery point gains from Mining Experience are multiplied by this value. Stacks multiplicatively with the General Mastery Factor above",
            getValue: () => (float)this.conf.miningMasteryExperienceFactor,
            setValue: value => this.conf.miningMasteryExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Combat Mastery Factor",
            tooltip: () => "All Mastery point gains from Combat Experience are multiplied by this value. Stacks multiplicatively with the General Mastery Factor above",
            getValue: () => (float)this.conf.combatMasteryExperienceFactor,
            setValue: value => this.conf.combatMasteryExperienceFactor = value
            );

        }

        void HandleSetExp(string arg1, string[] arg2)
        {
            if (arg2.Length == 2)
            {
                int skill = System.Convert.ToInt32(arg2[0]);
                int exp = System.Convert.ToInt32(arg2[1]);
                Helper.Events.GameLoop.UpdateTicked -= GameLoop_UpdateTicked;
                Game1.player.experiencePoints[skill] = exp;
                SetOldExpArray();
                Helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            }
        }


        void Display_RenderedWorld(object sender, StardewModdingAPI.Events.RenderedWorldEventArgs e)
        {
            if (Game1.paused)
                return;

            expAnimations.RemoveAll(anim => anim.Expired());
            expAnimations.ForEach(anim =>
            {
                anim.update(Game1.currentGameTime);
                anim.Draw(e.SpriteBatch);
            });
        }


        void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            conf = Helper.ReadConfig<LevelingConfig>();
            SetOldExpArray();
        }

        private void SetOldExpArray()
        {
            oldExperiencePoints = new int[SKILL_COUNT+1];
            var exp = Game1.player.experiencePoints.Fields.ToArray();
            var masteryExp = Game1.stats.Get("MasteryExp");
            for (int i = 0; i < SKILL_COUNT; i++)
                oldExperiencePoints[i] = exp[i];
            oldExperiencePoints[5] = (int)masteryExp;

            oldLevels = new int[SKILL_COUNT+1];
            oldLevels[FARMING_SKILL] = Game1.player.farmingLevel;
            oldLevels[FISHING_SKILL] = Game1.player.fishingLevel;
            oldLevels[FORAGING_SKILL] = Game1.player.foragingLevel;
            oldLevels[MINING_SKILL] = Game1.player.miningLevel;
            oldLevels[COMBAT_SKILL] = Game1.player.combatLevel;
            oldLevels[5] = StardewValley.Menus.MasteryTrackerMenu.getCurrentMasteryLevel();
        }

        void GameLoop_UpdateTicked(object sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            bool expchanged = false;
            double randomNumber = Utility.getRandomDouble(0, 1);

            var gameexp = Game1.player.experiencePoints.Fields.ToArray();
            for (int skill = 0; skill < SKILL_COUNT; skill++)
            {
                int gameexpi = gameexp[skill];
                int diff = gameexpi - oldExperiencePoints[skill];
                int moddiff = 0;
                double partialXP = 0;
                double moddiffInitial = 0;
                if (diff > 0)
                {
                    expchanged = true;

                    Monitor.Log(SkillName(skill) + " exp is " + oldExperiencePoints[skill], LogLevel.Trace);
                    Monitor.Log(SkillName(skill) + " exp increased by " + diff + " (game)", LogLevel.Trace);

                    moddiffInitial = diff * conf.generalExperienceFactor * ExperienceFactor(skill);
                    partialXP = moddiffInitial - System.Math.Floor(moddiffInitial);
                    if (partialXP != 0 && !conf.roundPartialExp)
                    {
                        Monitor.Log($"Modded exp gain is {System.Math.Floor(moddiffInitial)} with a {partialXP * 100}% chance for +1 exp", LogLevel.Trace);
                        if (partialXP > randomNumber)
                        {
                            moddiff = (int)System.Math.Floor(moddiffInitial) + 1;
                        }
                        else
                        {
                            moddiff = (int)System.Math.Floor(moddiffInitial);
                        }
                    }
                    else if (partialXP != 0 && conf.roundPartialExp)
                    {
                        moddiff = (int)System.Math.Round(moddiffInitial, MidpointRounding.AwayFromZero);
                        Monitor.Log($"\"Round Partial Experience Points\" is enabled, so exp was rounded from {moddiffInitial} to {moddiff}", LogLevel.Trace);
                        if (moddiff == 0)
                        {
                            moddiff = 1;
                            Monitor.Log("exp was rounded down to 0, so it was set instead to 1.", LogLevel.Trace);
                        }
                    }
                    else
                    {
                        moddiff = (int)System.Math.Round(moddiffInitial, MidpointRounding.AwayFromZero);
                    }
                    
                    int modexpi = oldExperiencePoints[skill] + moddiff;
                    int newgain = moddiff - diff;
                    Monitor.Log(SkillName(skill) + " exp increased by " + moddiff + " (with factor " + conf.generalExperienceFactor + "*" + ExperienceFactor(skill) + ")", LogLevel.Trace);

                    if (newgain > 0) //mod increases exp gain
                    {
                        //handles level ups
                        Game1.player.gainExperience(skill, newgain);
                    }
                    else if (newgain < 0) //mod reduces exp gain
                    {
                        //check if level was increased but modexpi would decrease the level again
                        //in this case do nothing since otherwise the player might get notified multiple times about the new level
                        if (GetLevelFromExp(modexpi) == GetSkillLevelValue(skill))
                        {
                            Game1.player.experiencePoints[skill] = modexpi;
                        }
                    }

                    Monitor.Log(SkillName(skill) + " exp is now " + Game1.player.experiencePoints[skill], LogLevel.Trace);

                    if (Game1.player.Level >= 25)
                    {
                        int modMasteryDiff = 0;
                        int masteryDiff = (int)Game1.stats.Get("MasteryExp") - oldExperiencePoints[5];
                        Monitor.Log($"Mastery exp is {oldExperiencePoints[5]}", LogLevel.Trace);
                        Monitor.Log($"Mastery exp increased by {masteryDiff} (game)", LogLevel.Trace);
                        // Basing new Mastery exp gains off the skill gains above instead of the game's mastery exp gains, to circumvent vanilla's mastery exp formula (in 1.6.4 vanilla multiplies all mastery exp gains by 0.5)
                        //int modMasteryExpi = (int)System.Math.Ceiling(oldExperiencePoints[5] + masteryDiff * conf.generalMasteryExperienceFactor * MasteryExperienceFactor(skill));
                        double modMasteryDiffInitial = moddiff * conf.generalMasteryExperienceFactor * MasteryExperienceFactor(skill);
                        double partialMasteryXP = modMasteryDiffInitial - System.Math.Floor(modMasteryDiffInitial);
                        if (partialMasteryXP != 0 && !conf.roundPartialExp)
                        {
                            Monitor.Log($"Modded Mastery exp gain is {System.Math.Floor(modMasteryDiffInitial)} with a {partialMasteryXP * 100}% chance for +1 Mastery exp", LogLevel.Trace);
                            if (partialMasteryXP > randomNumber)
                            {
                                modMasteryDiff = (int)System.Math.Floor(modMasteryDiffInitial) + 1;
                            }
                            else
                            {
                                modMasteryDiff = (int)System.Math.Floor(modMasteryDiffInitial);
                            }
                        }
                        else if (partialMasteryXP != 0 && conf.roundPartialExp)
                        {
                            modMasteryDiff = (int)System.Math.Round(modMasteryDiffInitial, MidpointRounding.AwayFromZero);
                            Monitor.Log($"\"Round Partial Experience Points\" is enabled, so Mastery exp was rounded from {modMasteryDiffInitial} to {modMasteryDiff}", LogLevel.Trace);
                            if (modMasteryDiff == 0)
                            {
                                modMasteryDiff = 1;
                                Monitor.Log("Mastery exp was rounded down to 0, so it was set instead to 1.", LogLevel.Trace);
                            }
                        }
                        else
                        {
                            modMasteryDiff = (int)System.Math.Round(modMasteryDiffInitial, MidpointRounding.AwayFromZero);
                        }

                        int modMasteryExpi = oldExperiencePoints[5] + modMasteryDiff;
                        int newMasterygain = modMasteryDiff - masteryDiff;
                        Monitor.Log($"Mastery exp increased by {modMasteryDiff} (with factor {conf.generalMasteryExperienceFactor} * {MasteryExperienceFactor(skill)})", LogLevel.Trace);

                        if (newMasterygain > 0) //mod increases mastery exp gain
                        {
                            Game1.stats.Set("MasteryExp", modMasteryExpi);
                            //handles mastery level ups
                            if (oldLevels[5] > StardewValley.Menus.MasteryTrackerMenu.getCurrentMasteryLevel())
                            {
                                Game1.showGlobalMessage(Game1.content.LoadString("Strings\\1_6_Strings:Mastery_newlevel"));
                                Game1.playSound("newArtifact");
                            }
                        }
                        else if (newMasterygain < 0) //mod reduces mastery exp gain
                        {
                            //check if mastery level was increased but modMasteryExpi would decrease the level again
                            //in this case do nothing since otherwise the player might get notified multiple times about the new mastery level
                            if (GetMasteryLevelFromExp(modMasteryExpi) == StardewValley.Menus.MasteryTrackerMenu.getCurrentMasteryLevel())
                            {
                                Game1.stats.Set("MasteryExp", modMasteryExpi);
                            }
                        }

                        if (conf.expNotification)
                        {
                            expAnimations.Add(new ExpAnimation(modMasteryDiff, skill));
                        }

                        oldExperiencePoints[5] = (int)Game1.stats.Get("MasteryExp"); // Sets the new Mastery exp amount for cases like Book Of Stars which raise multiple types of experience in a single event
                    }

                    if (conf.expNotification && Game1.player.Level < 25)
                    {
                        expAnimations.Add(new ExpAnimation(moddiff, skill));
                    }
                }
            }

            if (false)//(conf.levelNotification)
            {
                if (Game1.player.farmingLevel != oldLevels[FARMING_SKILL])
                {
                    expAnimations.Clear();
                    expAnimations.Add(new ExpAnimation(FARMING_SKILL));
                }

                if (Game1.player.fishingLevel != oldLevels[FISHING_SKILL])
                {
                    expAnimations.Clear();
                    expAnimations.Add(new ExpAnimation(FISHING_SKILL));
                }

                if (Game1.player.miningLevel != oldLevels[MINING_SKILL])
                {
                    expAnimations.Clear();
                    expAnimations.Add(new ExpAnimation(MINING_SKILL));
                }

                if (Game1.player.foragingLevel != oldLevels[FORAGING_SKILL])
                {
                    expAnimations.Clear();
                    expAnimations.Add(new ExpAnimation(FORAGING_SKILL));
                }

                if (Game1.player.combatLevel != oldLevels[COMBAT_SKILL])
                {
                    expAnimations.Clear();
                    expAnimations.Add(new ExpAnimation(COMBAT_SKILL));
                }
            }

            if (expchanged)
                SetOldExpArray();
        }

        //private void SetLevel(int skill, int exp)
        //{
        //    int level = GetLevelFromExp(exp);
        //    switch (skill)
        //    {
        //        case FARMING_SKILL:
        //            Game1.player.FarmingLevel = level;
        //            return;
        //        case MINING_SKILL:
        //            Game1.player.MiningLevel = level;
        //            return;
        //        case FISHING_SKILL:
        //            Game1.player.FishingLevel = level;
        //            return;
        //        case FORAGING_SKILL:
        //            Game1.player.ForagingLevel = level;
        //            return;
        //        case COMBAT_SKILL:
        //            Game1.player.CombatLevel = level;
        //            return;
        //        default:
        //            Monitor.Log($"SetLevel({skill})", LogLevel.Error);
        //            return;
        //    }
        //}

        /// <summary>
        /// Returns the value of the player level for the given skill number
        /// </summary>
        private int GetSkillLevelValue(int skill)
        {
            switch (skill)
            {
                //lower-case fields should be used (farming instead of Farming) 
                case FARMING_SKILL:
                    return Game1.player.farmingLevel;
                case MINING_SKILL:
                    return Game1.player.miningLevel;
                case FISHING_SKILL:
                    return Game1.player.fishingLevel;
                case FORAGING_SKILL:
                    return Game1.player.foragingLevel;
                case COMBAT_SKILL:
                    return Game1.player.combatLevel;
                default:
                    Monitor.Log($"GetSkillLevelValue({skill})", LogLevel.Error);
                    return 0;
            }
        }

        /// <summary>
        /// Returns the expected level for the given exp value
        /// </summary>
        private int GetLevelFromExp(int exp)
        {
            if (0 <= exp && exp < 100)
            {
                return 0;
            }
            if (100 <= exp && exp < 380)
            {
                return 1;
            }
            if (380 <= exp && exp < 770)
            {
                return 2;
            }
            if (770 <= exp && exp < 1300)
            {
                return 3;
            }
            if (1300 <= exp && exp < 2150)
            {
                return 4;
            }
            if (2150 <= exp && exp < 3300)
            {
                return 5;
            }
            if (3300 <= exp && exp < 4800)
            {
                return 6;
            }
            if (4800 <= exp && exp < 6900)
            {
                return 7;
            }
            if (6900 <= exp && exp < 10000)
            {
                return 8;
            }
            if (10000 <= exp && exp < 15000)
            {
                return 9;
            }
            if (15000 <= exp)
            {
                return 10;
            }
            Monitor.Log($"GetLevelFromExp({exp})", LogLevel.Error);
            return -1;
        }

        /// <summary>
        /// Returns the expected Mastery level for the given exp value
        /// </summary>
        private int GetMasteryLevelFromExp(int exp)
        {
            if (0 <= exp && exp < 10000)
            {
                return 0;
            }
            if (10000 <= exp && exp < 25000)
            {
                return 1;
            }
            if (25000 <= exp && exp < 45000)
            {
                return 2;
            }
            if (45000 <= exp && exp < 70000)
            {
                return 3;
            }
            if (70000 <= exp && exp < 100000)
            {
                return 4;
            }
            if (100000 <= exp)
            {
                return 5;
            }
            Monitor.Log($"GetMasteryLevelFromExp({exp})", LogLevel.Error);
            return -1;
        }


        private double ExperienceFactor(int skill)
        {
            switch (skill)
            {
                case FARMING_SKILL:
                    return conf.farmingExperienceFactor;
                case MINING_SKILL:
                    return conf.miningExperienceFactor;
                case FISHING_SKILL:
                    return conf.fishingExperienceFactor;
                case FORAGING_SKILL:
                    return conf.foragingExperienceFactor;
                case COMBAT_SKILL:
                    return conf.combatExperienceFactor;
                default:
                    Monitor.Log($"ExperienceFactor({skill})", LogLevel.Error);
                    return 1;
            }
        }

        private double MasteryExperienceFactor(int skill)
        {
            switch (skill)
            {
                case FARMING_SKILL:
                    return conf.farmingMasteryExperienceFactor;
                case MINING_SKILL:
                    return conf.miningMasteryExperienceFactor;
                case FISHING_SKILL:
                    return conf.fishingMasteryExperienceFactor;
                case FORAGING_SKILL:
                    return conf.foragingMasteryExperienceFactor;
                case COMBAT_SKILL:
                    return conf.combatMasteryExperienceFactor;
                default:
                    Monitor.Log($"MasteryExperienceFactor({skill})", LogLevel.Error);
                    return 1;
            }
        }

        private string SkillName(int i)
        {
            switch (i)
            {
                case FARMING_SKILL:
                    return "farmingLevel";
                case MINING_SKILL:
                    return "miningLevel";
                case FISHING_SKILL:
                    return "fishingLevel";
                case FORAGING_SKILL:
                    return "foragingLevel";
                case LUCK_SKILL:
                    return "luckLevel";
                case COMBAT_SKILL:
                    return "combatLevel";
                default:
                    return "unknownLevel";
            }
        }
    }
}
