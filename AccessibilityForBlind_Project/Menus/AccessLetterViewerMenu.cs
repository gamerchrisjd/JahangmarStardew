﻿// Copyright (c) 2020 Jahangmar
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
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley.Menus;

namespace AccessibilityForBlind.Menus
{
    public class AccessLetterViewerMenu : AccessMenu
    {
        public AccessLetterViewerMenu(IClickableMenu menu) : base(menu)
        {
            TextToSpeech.Speak(GetTitle(), true);

            AddItem(MenuItem.MenuItemFromComponent(menu.upperRightCloseButton, menu, "close letter"));

            ReadMessage();
        }

        private string ConvertMessage(string message)
        {
            return message.Replace('^', '\n');
        }

        private void ReadMessage()
        {
            foreach (string message in ModEntry.GetHelper().Reflection.GetField<List<string>>((stardewMenu as LetterViewerMenu), "mailMessage").GetValue())
                TextToSpeech.Speak(ConvertMessage(message), false);
        }

        public override string GetTitle()
        {
            return ModEntry.GetHelper().Reflection.GetField<string>((stardewMenu as LetterViewerMenu), "mailTitle").GetValue();
        }

        public override void ButtonPressed(SButton button)
        {
            switch (button)
            {
                case SButton.F1:
                    ReadMessage();
                    return;
            }
            base.ButtonPressed(button);
        }
    }
}
