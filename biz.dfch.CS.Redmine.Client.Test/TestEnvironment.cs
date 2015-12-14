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
        public static string Hostname { get; set; } //"http://192.168.213.128:10080/redmine";
        public static string ApiKey { get; set; } // "d28258aff3fb6117b49770a9ff1cd868cdfe7ac5";

        static TestEnvironment()
        {
            TestEnvironment.Hostname = "http://192.168.213.128:10080/redmine";
            TestEnvironment.ApiKey = "d28258aff3fb6117b49770a9ff1cd868cdfe7ac5";
        }
    }
}
