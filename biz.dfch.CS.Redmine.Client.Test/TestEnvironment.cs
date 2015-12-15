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
    public static class TestEnvironment
    {
        public static string RedminUrl { get; set; } 
        public static string ApiKey { get; set; }
        public static int TotalAttempts { get; set; }
        public static int BaseRetryIntervallMilliseconds { get; set; }
        public static int ProjectId { get; set; }
        public static int IssueId { get; set; }
        public static string UserLogin1 { get; set; }
        public static string UserLogin2 { get; set; }
        public static string ProjectIdentifier { get; set; }

        static TestEnvironment()
        {
            TestEnvironment.RedminUrl = "http://192.168.213.128:10080/redmine";
            TestEnvironment.ApiKey = "d28258aff3fb6117b49770a9ff1cd868cdfe7ac5";
            TestEnvironment.TotalAttempts = 3;
            TestEnvironment.BaseRetryIntervallMilliseconds = 100;
            TestEnvironment.ProjectId = 5;
            TestEnvironment.IssueId = 1;
            TestEnvironment.UserLogin1 = "niklaus";
            TestEnvironment.UserLogin2 = "test";
            TestEnvironment.ProjectIdentifier = "testprojekt";
        }

        
    }
}
