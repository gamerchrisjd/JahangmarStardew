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

            configMenu.AddSectionTitle(
            mod: this.ModManifest,
            text: () => "Skill Experience",
            tooltip: () => "The below factors affect normal skill experience gains"
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "General Experience Factor",
            tooltip: () => "All experience gains are multiplied by this value. Stacks multiplicatively with the individual Skill Experience Factors below",
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
            tooltip: () => "The below factors affect Mastery point gains once all skills are at level 10. These stack multiplicatively with the Experience Factors above, and replace the vanilla Mastery point formula. In vanilla in 1.6.4, Mastery points are calculated by taking any Skill experience earned and multiplying by 0.5"
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "General Mastery Factor",
            tooltip: () => "All Mastery point gains are multiplied by this value (In vanilla 1.6.4 all Mastery point gains are calculated by multiplying the skill experience by 0.5, so setting this value to 0.5 and leaving all Skill Mastery Factors below at 1 will result in vanilla behavior). Stacks multiplicatively with the individual Skill Mastery Factors below",
            getValue: () => (float)this.conf.generalMasteryExperienceFactor,
            setValue: value => this.conf.generalMasteryExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Farming Mastery Factor",
            tooltip: () => "All Farming Mastery point gains are multiplied by this value. Stacks multiplicatively with the General Mastery Factor above. (In vanilla 1.6.4 Farming Mastery point gains are stated by be multiplied by 0.5, but in practice all Skill mastery point gains are multiplied by 0.5)",
            getValue: () => (float)this.conf.farmingMasteryExperienceFactor,
            setValue: value => this.conf.farmingMasteryExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Foraging Mastery Factor",
            tooltip: () => "All Foraging Mastery point gains are multiplied by this value. Stacks multiplicatively with the General Mastery Factor above",
            getValue: () => (float)this.conf.foragingMasteryExperienceFactor,
            setValue: value => this.conf.foragingMasteryExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Fishing Mastery Factor",
            tooltip: () => "All Fishing Mastery point gains are multiplied by this value. Stacks multiplicatively with the General Mastery Factor above",
            getValue: () => (float)this.conf.fishingMasteryExperienceFactor,
            setValue: value => this.conf.fishingMasteryExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Mining Mastery Factor",
            tooltip: () => "All Mining Mastery point gains are multiplied by this value. Stacks multiplicatively with the General Mastery Factor above",
            getValue: () => (float)this.conf.miningMasteryExperienceFactor,
            setValue: value => this.conf.miningMasteryExperienceFactor = value
            );

            configMenu.AddNumberOption(
            mod: this.ModManifest,
            name: () => "Combat Mastery Factor",
            tooltip: () => "All Combat Mastery point gains are multiplied by this value. Stacks multiplicatively with the General Mastery Factor above",
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

            var gameexp = Game1.player.experiencePoints.Fields.ToArray();
            for (int skill = 0; skill < SKILL_COUNT; skill++)
            {
                int gameexpi = gameexp[skill];
                int diff = gameexpi - oldExperiencePoints[skill];
                if (diff > 0)
                {
                    expchanged = true;

                    Monitor.Log(SkillName(skill) + " exp is " + oldExperiencePoints[skill], LogLevel.Trace);
                    Monitor.Log(SkillName(skill) + " exp increased by " + diff + " (game)", LogLevel.Trace);

                    int modexpi = (int)System.Math.Ceiling(oldExperiencePoints[skill] + diff * conf.generalExperienceFactor * ExperienceFactor(skill));
                    int moddiff = modexpi - oldExperiencePoints[skill];
                    int newgain = moddiff - diff;
                    Monitor.Log(SkillName(skill) + " exp increased by " + moddiff + " (with factor " + conf.generalExperienceFactor + "*" + ExperienceFactor(skill), LogLevel.Trace);

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
                        int masteryDiff = (int)Game1.stats.Get("MasteryExp") - oldExperiencePoints[5];
                        Monitor.Log($"Mastery exp is {oldExperiencePoints[5]}", LogLevel.Trace);
                        Monitor.Log($"Mastery exp increased by {masteryDiff} (game)", LogLevel.Trace);
                        // Basing new Mastery exp gains off the skill gains above instead of the game's mastery exp gains, to circumvent vanilla's mastery exp formula (in 1.6.4 vanilla multiplies all mastery exp gains by 0.5)
                        //int modMasteryExpi = (int)System.Math.Ceiling(oldExperiencePoints[5] + masteryDiff * conf.generalMasteryExperienceFactor * MasteryExperienceFactor(skill));
                        int modMasteryExpi = (int)System.Math.Ceiling(oldExperiencePoints[5] + moddiff * conf.generalMasteryExperienceFactor * MasteryExperienceFactor(skill));
                        int modMasteryDiff = modMasteryExpi - oldExperiencePoints[5];
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
