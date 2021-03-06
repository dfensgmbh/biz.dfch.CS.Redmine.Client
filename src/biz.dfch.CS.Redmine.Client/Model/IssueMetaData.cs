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

namespace biz.dfch.CS.Redmine.Client.Model
{
    public class IssueMetaData
    {

        public const string AssignedToLoginKey = "AssignedToLogin";
        public const string ProjectIdentifierKey = "ProjectIdentifier";
        public const string PriorityNameKey = "PriorityName";
        public const string StateNameKey = "StateName";
        public const string TrackerNameKey = "TrackerName";
        public const string NotesKey = "Notes";
        public const string PrivateNotesKey = "PrivateNotes";

        /// <summary>
        /// Login of the user to who the issue is assigend
        /// </summary>
        public string AssignedToLogin { get; set; }
        /// <summary>
        /// Identifier of the project the issue belongs to
        /// </summary>
        public string ProjectIdentifier { get; set; }
        /// <summary>
        /// Name of the priority of the issue (eg. Low, High, Urgent, ...)
        /// </summary>
        public string PriorityName { get; set; }
        /// <summary>
        /// Name of the state of the issue (eg. New, In Progress, Done, ...)
        /// </summary>
        public string StateName { get; set; }
        /// <summary>
        /// Name of the tracker (type) of the issue (eg. Bug, Feature, ...)
        /// </summary>
        public string TrackerName { get; set; }
        /// <summary>
        /// The notes for the issue
        /// </summary>
        public string Notes { get; set; }
        /// <summary>
        /// Defines if the the notes entry will be private or not
        /// </summary>
        public bool PrivateNotes { get; set; }

        public IssueMetaData()
        {
        }

        public IssueMetaData(Dictionary<string, object> values)
        {
            this.AssignedToLogin = Util.GetValue<string>(values, IssueMetaData.AssignedToLoginKey);
            this.ProjectIdentifier = Util.GetValue<string>(values, IssueMetaData.ProjectIdentifierKey);
            this.PriorityName = Util.GetValue<string>(values, IssueMetaData.PriorityNameKey);
            this.StateName = Util.GetValue<string>(values, IssueMetaData.StateNameKey);
            this.TrackerName = Util.GetValue<string>(values, IssueMetaData.TrackerNameKey);
            this.Notes = Util.GetValue<string>(values, IssueMetaData.NotesKey);
            this.PrivateNotes = Util.GetValue<bool>(values, IssueMetaData.PrivateNotesKey);
        }
    }
}
