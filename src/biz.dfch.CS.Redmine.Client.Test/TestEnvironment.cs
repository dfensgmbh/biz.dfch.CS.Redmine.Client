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
        public static string ProjectIdentifier1 { get; set; }
        public static string ProjectIdentifier2 { get; set; }
        public static int AttachmentId { get; set; }
        public static string AttachmentFilePath { get; set; }

        static TestEnvironment()
        {
            //local
            //TestEnvironment.RedminUrl = "http://192.168.99.1:10080/redmine";
            //TestEnvironment.ApiKey = "d28258aff3fb6117b49770a9ff1cd868cdfe7ac5";
            //TestEnvironment.ProjectId = 5;
            //TestEnvironment.IssueId = 1;
            //TestEnvironment.UserLogin1 = "niklaus";
            //TestEnvironment.UserLogin2 = "test";
            //TestEnvironment.ProjectIdentifier1 = "testprojekt";
            //TestEnvironment.ProjectIdentifier2 = "d27f27bf-e2e9-47c6-b87f-a93fe31dec32";
            //TestEnvironment.AttachmentId = 2;

            //lab3
            TestEnvironment.RedminUrl = "http://172.19.115.27";
            TestEnvironment.ApiKey = "1e1d5e8de8a9f3bc8bbc6b0bec2a9fdd30786dc3";
            TestEnvironment.ProjectId = 1;
            TestEnvironment.IssueId = 1;
            TestEnvironment.UserLogin1 = "tgdkuni7";
            TestEnvironment.UserLogin2 = "niklaus";
            TestEnvironment.ProjectIdentifier1 = "project-for-testing";
            TestEnvironment.ProjectIdentifier2 = "project-for-testing-2";
            TestEnvironment.AttachmentId = 1;

            TestEnvironment.TotalAttempts = 3;
            TestEnvironment.BaseRetryIntervallMilliseconds = 100;
            TestEnvironment.AttachmentFilePath = @"C:\Users\Administrator\Desktop\test.txt";
        }


    }
}
