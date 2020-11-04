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
using StardewValley;
using StardewValley.Menus;

namespace AccessibilityForBlind.Menus
{
    public class AccessDialogBox : AccessMenu
    {
        private bool initialized = false;
        public AccessDialogBox(IClickableMenu menu) : base(menu)
        {
            SpeakCurrentText();
        }

        private void initialize()
        {
            DialogueBox dialogueBox = (stardewMenu as DialogueBox);
            List<Response> responses = ModEntry.GetHelper().Reflection.GetField<List<Response>>(dialogueBox, "responses").GetValue();

            int j = dialogueBox.allClickableComponents.Count - 1;
            for (int i=0; i<dialogueBox.allClickableComponents.Count; i++)
            {
                AddItem(MenuItem.MenuItemFromComponent(dialogueBox.allClickableComponents[i], dialogueBox, responses[j].responseText));
                j--;
            }

            initialized = true;
        }

        private void SpeakCurrentText()
        {
            TextToSpeech.Speak((stardewMenu as DialogueBox).getCurrentString());
        }

        public override string GetTitle()
        {
            return "";
        }

        public override void ButtonPressed(SButton button)
        {
            if (!initialized)
                initialize();
            base.ButtonPressed(button);
        }
    }
}