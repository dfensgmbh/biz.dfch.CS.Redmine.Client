/**
 * Copyright 2015 d-fens GmbH
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
 
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace biz.dfch.CS.Redmine.Client.Test
{
    public class TextCreator
    {
        private static string projectNameTemplate = "Project {0} {1}";
        private static string projectDescriptionTemplate = "We want to create a {0} for {1} so that {2} can handle {3}";
        private static string issueNameTemplate = "{0} must support {1}";
        private static string issueDescriptionTemplate = "Add support for {0} in {1} so that {2} can handle {3}";
        private static string[] wordlist = 
            {
                "belong", "myotonia", "mariner", "basque", "rainbow", "batten", "tussle", "lollygag", 
                "sporty", "barogram", "droll", "mainland", "joy", "petrosal", "surname", "recoup", 
                "footsie", "potash", "missal", "floater", "aggrade", "mandrake", "port", "repel", 
                "derma", "riant", "caroche", "covert", "carabao", "aloof", "deadhead", "tandem", 
                "mannish", "lax", "oxidase", "deodand", "cheeky", "drain", "terrapin", "journey", 
                "febrile", "fitly", "overtake", "cleft", "ikon", "lurdan", "enclasp", "basis", 
                "baronet", "abhenry", "timeous", "resale", "benzoin", "glazier", "bullring", "chip", 
                "glucinum", "varices", "tapestry", "avaunt", "ultra", "espy", "gaslit", "ramrod", 
                "heronry", "matins", "peen", "squeeze", "ardeb", "vendace", "hindmost", "grating", 
                "link", "toneme", "oodles", "melic", "unwashed", "pitman", "sonic", "still", "flicker", 
                "fraenum", "stagnate", "epicotyl", "locally", "ecbolic", "frag", "recess", "leal", 
                "compo", "glycol", "broom", "oculist", "sarcenet", "saintly", "papaya", "potheen", 
                "dating", "revolute", "spicule", "nee", "vitellin", "leaky", "mooned", "locate", 
                "tortilla", "whiles", "quarrel", "sneak", "aside", "absence", "yeti", "hent", "jut", 
                "ars", "swain", "thanks", "fro", "proa", "nutbrown"
            };


        private static string GetText(string template, int numberOfPlaceHolder)
        {
            string[] fillWords = new string[numberOfPlaceHolder];
            Random random = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < fillWords.Length; i++)
            {
                int wordIndex = random.Next(0, TextCreator.wordlist.Length - 1);
                fillWords[i] = TextCreator.wordlist[wordIndex];
            }
            return string.Format(template, fillWords);
        }

        public static string GetProjectName()
        {
            return TextCreator.GetText(TextCreator.projectNameTemplate, 2);
        }

        public static string GetProjectDesctiption()
        {
            return TextCreator.GetText(TextCreator.projectDescriptionTemplate, 4);
        }

        public static string GetIssueName()
        {
            return TextCreator.GetText(TextCreator.issueNameTemplate, 2);
        }

        public static string GetIssueDescription()
        {
            return TextCreator.GetText(TextCreator.issueDescriptionTemplate, 4);
        }
    }
}
