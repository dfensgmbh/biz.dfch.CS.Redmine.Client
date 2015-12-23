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
    public class IssueQueryParameters
    {
        public const string AssignedToLoginKey = "AssignedToLogin";
        public const string ProjectIdentifierKey = "ProjectIdentifier";
        public const string PriorityNameKey = "PriorityName";
        public const string StateNameKey = "StateName";
        public const string TrackerNameKey = "TrackerName";

        public string ProjectIdentifier { get; set; }
        public string StateName { get; set; }
        public string AssignedToLogin { get; set; }
        public string TrackerName { get; set; }
        public string PriorityName { get; set; }

         public IssueQueryParameters()
        {
        }

         public IssueQueryParameters(Dictionary<string, object> values)
        {
            this.AssignedToLogin = Util.GetValue<string>(values, IssueQueryParameters.AssignedToLoginKey);
            this.ProjectIdentifier = Util.GetValue<string>(values, IssueQueryParameters.ProjectIdentifierKey);
            this.PriorityName = Util.GetValue<string>(values, IssueQueryParameters.PriorityNameKey);
            this.StateName = Util.GetValue<string>(values, IssueQueryParameters.StateNameKey);
            this.TrackerName = Util.GetValue<string>(values, IssueQueryParameters.TrackerNameKey);
        }
    }
}
