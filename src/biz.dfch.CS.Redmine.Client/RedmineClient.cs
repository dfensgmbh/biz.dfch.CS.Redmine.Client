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
using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using biz.dfch.CS.Utilities.Logging;
using Redmine.Net.Api;
using Redmine.Net.Api.Types;

namespace biz.dfch.CS.Redmine.Client
{
    public class RedmineClient
    {
        #region Constants

        /// <summary>
        /// Default total attempts for calls
        /// </summary>
        protected const int TOTAL_ATTEMPTS = 5;
        /// <summary>
        /// Default base retry intervall milliseconds
        /// </summary>
        private const int BASE_RETRY_INTERVAL_MILLISECONDS = 5 * 1000;

        #endregion Constants

        #region Properties

        /// <summary>
        /// Current total attempts that are made for a request
        /// </summary>
        public int TotalAttempts { get; set; }
        /// <summary>
        /// Current base wait intervall between request attempts in milliseconds
        /// </summary>
        public int BaseRetryIntervallMilliseconds { get; set; }
        /// <summary>
        /// Url to the redmine API
        /// </summary>
        public string RedmineUrl { get; set; }
        /// <summary>
        /// Authencation key for the redmine API
        /// </summary>
        public string ApiKey { get; set; }
        /// <summary>
        /// True if the the user could succsefully be authorized on the server
        /// </summary>
        public bool IsLoggedIn { get; private set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Creates an Instance of the Class <see cref="RedmineClient"/>
        /// </summary>
        public RedmineClient()
        {
            this.TotalAttempts = RedmineClient.TOTAL_ATTEMPTS;
            this.BaseRetryIntervallMilliseconds = RedmineClient.BASE_RETRY_INTERVAL_MILLISECONDS;
        }

        #endregion Constructors

        #region Login


        /// <summary>
        ///  Checks if the user can be authorized on the server
        /// </summary>
        /// <param name="redmineUrl">Url of the redmine api</param>
        /// <param name="apiKey">Key for the redmine api</param>
        /// <returns>True if the user could be authorized on the server</returns>
        public bool Login(string redmineUrl, string apiKey)
        {
            return this.Login(redmineUrl, apiKey, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        ///  Checks if the user can be authorized on the server
        /// </summary>
        /// <param name="redmineUrl">Url of the redmine api</param>
        /// <param name="apiKey">Key for the redmine api</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseWaitingMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>True if the user could be authorized on the server</returns>
        public bool Login(string redmineUrl, string apiKey, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(!string.IsNullOrEmpty(redmineUrl), "No redmine url defined");
            Contract.Requires(!string.IsNullOrEmpty(apiKey), "No api key defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.Login({0}, {1}, {2}, {3})", redmineUrl, apiKey, totalAttempts, baseRetryIntervallMilliseconds));

            this.Logout(); // Ensure old login info is removed

            this.IsLoggedIn = RedmineClient.InvokeWithRetries(() =>
                {
                    //Test if the credentials are valid
                    RedmineManager redmineManager = this.GetRedmineManager(redmineUrl, apiKey);
                    IList<Project> projects = redmineManager.GetObjectList<Project>(new NameValueCollection());

                    //if there is no error the credentials are valid
                    return null != projects;
                }, totalAttempts, baseRetryIntervallMilliseconds);

            if (this.IsLoggedIn)
            {
                this.RedmineUrl = redmineUrl;
                this.ApiKey = apiKey;
            }
            else
            {
                throw new Exception("User could not be authorized");
            }

            return this.IsLoggedIn;
        }

        /// <summary>
        /// Removes all login information
        /// </summary>
        public void Logout()
        {
            Trace.WriteLine(string.Format("RedmineClient.Logout()"));
            this.ApiKey = null;
            this.RedmineUrl = null;
            this.IsLoggedIn = false;
        }

        #endregion Login

        #region Projects

        /// <summary>
        /// Gets the list of projects
        /// </summary>
        /// <returns>The list of projects</returns>
        public IList<Project> GetProjects()
        {
            return this.GetProjects(this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets the list of projects
        /// </summary>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseWaitingMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>The list of projects</returns>
        public IList<Project> GetProjects(int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetProjects({0}, {1})", totalAttempts, baseRetryIntervallMilliseconds));

            IList<Project> projects = RedmineClient.InvokeWithRetries(() =>
                {
                    RedmineManager redmineManager = this.GetRedmineManager();
                    return redmineManager.GetObjectList<Project>(new NameValueCollection());
                }, totalAttempts, baseRetryIntervallMilliseconds);

            return projects;
        }

        /// <summary>
        /// Get a project
        /// </summary>
        /// <param name="id">ID of the project</param>
        /// <returns>The project specified by the ID</returns>
        public Project GetProject(int id)
        {
            return this.GetProject(id, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Get a project
        /// </summary>
        /// <param name="id">ID of the project</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseWaitingMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>The project specified by the ID</returns>
        public Project GetProject(int id, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts != 0, "No project id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetProjects({0}, {1}, {2})", id, totalAttempts, baseRetryIntervallMilliseconds));

            Project project = RedmineClient.InvokeWithRetries(() =>
                {
                    RedmineManager redmineManager = this.GetRedmineManager();
                    return redmineManager.GetObject<Project>(id.ToString(), new NameValueCollection());
                }, totalAttempts, baseRetryIntervallMilliseconds);

            return project;
        }

        #endregion Projects

        #region Redmine API Access

        /// <summary>
        /// Create a redmine manager
        /// </summary>
        /// <returns>A new redmine manager</returns>
        private RedmineManager GetRedmineManager()
        {
            return this.GetRedmineManager(this.RedmineUrl, this.ApiKey);
        }

        /// <summary>
        /// Create a redmine manager
        /// </summary>
        /// <param name="redmineUrl">Url of the redmine API</param>
        /// <param name="apiKey">Key for the redmine API</param>
        /// <returns>A new redmine manager</returns>
        private RedmineManager GetRedmineManager(string redmineUrl, string apiKey)
        {
            return new RedmineManager(redmineUrl, apiKey, MimeFormat.json, false);
        }

        #endregion Redmine API Access

        #region Retry Mechanism

        /// <summary>
        /// Invokes a function and retries it a specified number of times if there was an error
        /// </summary>
        /// <typeparam name="ResultType">The type of the result</typeparam>
        /// <param name="function">The function to invoke</param>
        /// <param name="totalAttempts">The total number of attempts that should be made to invoke the function</param>
        /// <param name="baseRetryIntervallMilliseconds">The base time to wait for between two attempts in milliseconds</param>
        /// <returns>The result of the function</returns>
        private static ResultType InvokeWithRetries<ResultType>(Func<ResultType> function, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            ResultType result = default(ResultType);
            int currentWaitingMillis = baseRetryIntervallMilliseconds;
            for (var i = 1; i <= totalAttempts; i++)
            {
                try
                {
                    result = function();
                    break;
                }
                catch (Exception ex)
                {
                    var logEx = ex;
                    while (logEx != null)
                    {
                        Trace.WriteLine(string.Format("ExecuteWithRetries: [{0}/{1}] FAILED.", i, totalAttempts));
                        Trace.WriteLine(logEx.Message);
                        Trace.WriteLine(logEx.StackTrace);
                        logEx = logEx.InnerException;
                    }

                    if (i >= totalAttempts)
                    {
                        throw;
                    }
                }
                Thread.Sleep(currentWaitingMillis);
                currentWaitingMillis = currentWaitingMillis * 2;
            }
            return result;
        }

        #endregion Retry Mechanism

    }
}
