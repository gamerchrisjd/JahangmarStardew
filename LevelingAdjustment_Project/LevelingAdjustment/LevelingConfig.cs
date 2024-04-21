﻿// Copyright (c) 2019 Jahangmar
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
using System;

namespace LevelingAdjustment
{
    public class LevelingConfig
    {
        public bool expNotification = false;
        //public bool levelNotification = false;
        public double generalExperienceFactor = 1;
        public double farmingExperienceFactor = 1;
        public double fishingExperienceFactor = 1;
        public double foragingExperienceFactor = 1;
        public double miningExperienceFactor = 1;
        public double combatExperienceFactor = 1;

        public double generalMasteryExperienceFactor = 1;
        public double farmingMasteryExperienceFactor = 0.5;
        public double fishingMasteryExperienceFactor = 1;
        public double foragingMasteryExperienceFactor = 1;
        public double miningMasteryExperienceFactor = 1;
        public double combatMasteryExperienceFactor = 1;
    }
}
