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
using System.Threading;
﻿using biz.dfch.CS.Redmine.Client.Model;
using biz.dfch.CS.Utilities.Logging;
using Redmine.Net.Api;
﻿using Redmine.Net.Api.Exceptions;
﻿using Redmine.Net.Api.Types;
using System.Net;

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
        /// <summary>
        /// The page size for redmine (can at most be 100 in current redmine version (3.2.0), should be configurable later)
        /// </summary>
        private const int PAGE_SIZE = 100;

        #endregion Constants

        #region Members

        private readonly MemoryCacheHelper _cache;

        #endregion

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
        /// The user name for authentication
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// The password for authentication
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// True if the the user could succsefully be authorized on the server
        /// </summary>
        public bool IsLoggedIn { get; private set; }
        /// <summary>
        /// The page size to get in a list request (request will be repeated until all items are loaded).
        /// Can at most be 100 in current redmine version (3.2.0), should be configurable later.
        /// </summary>
        public int PageSize { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Creates an Instance of the Class <see cref="RedmineClient"/>
        /// </summary>
        public RedmineClient()
        {
            this.TotalAttempts = RedmineClient.TOTAL_ATTEMPTS;
            this.BaseRetryIntervallMilliseconds = RedmineClient.BASE_RETRY_INTERVAL_MILLISECONDS;
            this.PageSize = RedmineClient.PAGE_SIZE;
            this._cache = new MemoryCacheHelper();
        }

        #endregion Constructors

        #region Login

        /// <summary>
        ///  Checks if the user can be authorized on the server
        /// </summary>
        /// <param name="redmineUrl">Url of the redmine api</param>
        /// <param name="username">The user name for authentication</param>
        /// <param name="password">The password for authentication</param>
        /// <returns>True if the user could be authorized on the server</returns>
        public bool Login(string redmineUrl, string username, string password)
        {
            return this.Login(redmineUrl, username, password, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        ///  Checks if the user can be authorized on the server
        /// </summary>
        /// <param name="redmineUrl">Url of the redmine api</param>
        /// <param name="username">The user name for authentication</param>
        /// <param name="password">The password for authentication</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>True if the user could be authorized on the server</returns>
        public bool Login(string redmineUrl, string username, string password, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(!string.IsNullOrEmpty(redmineUrl), "No redmine url defined");
            Contract.Requires(!string.IsNullOrEmpty(username), "No username defined");
            Contract.Requires(!string.IsNullOrEmpty(password), "No password defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.Login({0}, {1}, {2}, {3})", redmineUrl, username, totalAttempts, baseRetryIntervallMilliseconds));

            this.Logout(); // Ensure old login info is removed

            this.IsLoggedIn = RedmineClient.InvokeWithRetries(() =>
            {
                //Test if the credentials are valid
                RedmineManager redmineManager = this.GetRedmineManager(redmineUrl, username, password);
                IList<Project> projects = redmineManager.GetObjectList<Project>(new NameValueCollection());

                //if there is no error the credentials are valid
                return null != projects;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            if (this.IsLoggedIn)
            {
                this.RedmineUrl = redmineUrl;
                this.Username = username;
                this.Password = password;
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
            this.Username = null;
            this.Password = null;
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
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The list of projects</returns>
        public IList<Project> GetProjects(int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetProjects({0}, {1})", totalAttempts, baseRetryIntervallMilliseconds));

            var cachedItems = GetCachedItems<Project>();

            if (cachedItems.Any())
                return cachedItems;

            IList<Project> projects = InvokeWithRetries(() =>
            {
                var redmineManager = GetRedmineManager();
                var items = redmineManager.GetObjects<Project>(new NameValueCollection());

                AddOrUpdateCachedIdentifiableNameItems(items);

                return items;
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
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The project specified by the ID</returns>
        public Project GetProject(int id, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(id > 0, "No project id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetProject({0}, {1}, {2})", id, totalAttempts, baseRetryIntervallMilliseconds));

            var cachedItem = GetCachedItem<Project>(id.ToString());

            if (cachedItem != null)
                return cachedItem;

            var project = InvokeWithRetries(() =>
            {
                var redmineManager = this.GetRedmineManager();
                var item = redmineManager.GetObject<Project>(id.ToString(), new NameValueCollection());

                AddOrUpdateCachedIdentifiableNameItem(item);

                return item;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return project;
        }

        /// <summary>
        /// Get a project by identifier
        /// </summary>
        /// <param name="identifier">Identifier of the project</param>
        /// <returns>The project specified by the identifier</returns>
        public Project GetProjectByIdentifier(string identifier)
        {
            return this.GetProjectByIdentifier(identifier, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Get a project by identifier
        /// </summary>
        /// <param name="identifier">Identifier of the project</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The project specified by the identifier</returns>
        public Project GetProjectByIdentifier(string identifier, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(!string.IsNullOrEmpty(identifier), "No project identifier defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetProjectByIdentifier({0}, {1}, {2})", identifier, totalAttempts, baseRetryIntervallMilliseconds));

            IList<Project> projects = this.GetProjects(totalAttempts, baseRetryIntervallMilliseconds);

            Project project = projects.FirstOrDefault(p => p.Identifier == identifier);
            if (project == null)
            {
                //If there is no project with the specified identifier throw an exception so that the behaviour is the same as for other redmine objects
                throw new RedmineException("Not Found");
            }

            return project;
        }

        /// <summary>
        /// Creates a new project
        /// </summary>
        /// <param name="project">The project to create</param>
        /// <param name="parentIdentifier">Identifier of the parent or null if the parent is specified in the project object or the project is a root project</param>
        /// <returns>The created project</returns>
        public Project CreateProject(Project project, string parentIdentifier)
        {
            return this.CreateProject(project, parentIdentifier, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Creates a new project
        /// </summary>
        /// <param name="project">The project to create</param>
        /// <param name="parentIdentifier">Identifier of the parent or null if the parent is specified in the project object or the project is a root project</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The created project</returns>
        public Project CreateProject(Project project, string parentIdentifier, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(null != project, "No project defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.CreateProject({0}, {1}, {2}, {3})", project.Name, parentIdentifier, totalAttempts, baseRetryIntervallMilliseconds));

            if (!string.IsNullOrEmpty(parentIdentifier))
            {
                var parent = GetCachedItem<Project>(parentIdentifier) ?? GetProjectByIdentifier(parentIdentifier);

                Contract.Assert(null != parent, string.Format("No project with identifier {0} found", parentIdentifier));

                project.Parent = new IdentifiableName { Id = parent.Id };
            }

            var createdProject = InvokeWithRetries(() =>
            {
                var redmineManager = GetRedmineManager();
                var item = redmineManager.CreateObject(project);

                AddOrUpdateCachedIdentifiableNameItem(item);

                return item;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return createdProject;
        }

        /// <summary>
        /// Updates a project
        /// </summary>
        /// <param name="project">The new project data</param>
        /// <returns>The updated project</returns>
        public Project UpdateProject(Project project, string parentIdentifier)
        {
            return this.UpdateProject(project, parentIdentifier, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Updates a project
        /// </summary>
        /// <param name="project">The new project data</param>
        /// <param name="parentIdentifier">Identifier of the parent or null if the parent is specified in the project object or the project is a root project</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The updated project</returns>
        public Project UpdateProject(Project project, string parentIdentifier, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(null != project, "No project defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.UpdateProject({0}, {1}, {2},  {3})", project.Name, parentIdentifier, totalAttempts, baseRetryIntervallMilliseconds));

            if (!string.IsNullOrEmpty(parentIdentifier))
            {
                var parent = GetCachedItem<Project>(parentIdentifier) ?? GetProjectByIdentifier(parentIdentifier);

                Contract.Assert(null != parent, string.Format("No project with identifier {0} found", parentIdentifier));

                project.Parent = new IdentifiableName() { Id = parent.Id };
            }

            var updatedProject = InvokeWithRetries(() =>
            {
                var redmineManager = GetRedmineManager();
                redmineManager.UpdateObject(project.Id.ToString(), project);

                RemoveCachedItem<Project>(project.Id.ToString());

                return GetProject(project.Id, totalAttempts, baseRetryIntervallMilliseconds);
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return updatedProject;
        }

        /// <summary>
        /// Deletes a project
        /// </summary>
        /// <param name="id">The id of the project</param>
        /// <returns>True if the project could be deleted</returns>
        public bool DeleteProject(int id)
        {
            return this.DeleteProject(id, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Deletes a project
        /// </summary>
        /// <param name="id">The id of the project</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>True if the project could be deleted</returns>
        public bool DeleteProject(int id, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(id > 0, "No project id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.DeleteProject({0}, {1}, {2})", id, totalAttempts, baseRetryIntervallMilliseconds));

            var success = InvokeWithRetries(() =>
            {
                var redmineManager = GetRedmineManager();

                redmineManager.DeleteObject<Project>(id.ToString(), new NameValueCollection());
                RemoveCachedItem<Project>(id.ToString());

                return true;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return success;
        }

        #endregion Projects

        #region Issues

        /// <summary>
        /// Gets the list of issues
        /// </summary>
        /// <returns>The list of issues for a project</returns>
        public IList<Issue> GetIssues()
        {
            return this.GetIssues((IssueQueryParameters)null);
        }
        /// <summary>
        /// Gets the list of issues matching the query parameters (null values are ignored)
        /// </summary>
        /// <param name="queryParameters">The parameters for the query (null values are ignored)</param>
        /// <returns>The list of issues for a project</returns>
        public IList<Issue> GetIssues(object queryParameters)
        {
            #region Contract
            Contract.Requires((queryParameters is Dictionary<string, object>) || (queryParameters is IssueQueryParameters), "queryParameters must be Dictionary<string, object> or IssueQueryParameters");
            #endregion Contract

            IList<Issue> issues = null;
            if (queryParameters is Dictionary<string, object>)
            {
                issues = this.GetIssues((Dictionary<string, object>)queryParameters);
            }
            else if (queryParameters is IssueMetaData)
            {
                issues = this.GetIssues((IssueQueryParameters)queryParameters);
            }
            return issues;
        }
        /// <summary>
        /// Gets the list of issues matching the query parameters (null values are ignored)
        /// </summary>
        /// <param name="queryParameters">The parameters for the query (null values are ignored)</param>
        /// <returns>The list of issues for a project</returns>
        public IList<Issue> GetIssues(Dictionary<string, object> queryParameters)
        {
            #region Contract
            Contract.Requires(null != queryParameters, "No query parameters defined");
            #endregion Contract

            IssueQueryParameters queryParameterObject = new IssueQueryParameters(queryParameters);
            return this.GetIssues(queryParameterObject);
        }

        /// <summary>
        /// Gets the list of issues matching the query parameters (null values are ignored)
        /// </summary>
        /// <param name="queryParameters">The parameters for the query (null values are ignored)</param>
        /// <returns>The list of issues for a project</returns>
        public IList<Issue> GetIssues(IssueQueryParameters queryParameters)
        {
            return this.GetIssues(queryParameters, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets the list of issues matching the query parameters (null values are ignored)
        /// </summary>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The list of issues for a project</returns>
        public IList<Issue> GetIssues(int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            return this.GetIssues((IssueQueryParameters)null, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets the list of issues
        /// </summary>
        /// <param name="queryParameters">The parameters for the query (null values are ignored)</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The list of issues for a project</returns>
        public IList<Issue> GetIssues(object queryParameters, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires((queryParameters is Dictionary<string, object>) || (queryParameters is IssueQueryParameters), "queryParameters must be Dictionary<string, object> or IssueQueryParameters");
            #endregion Contract

            IList<Issue> issues = null;
            if (queryParameters is Dictionary<string, object>)
            {
                issues = this.GetIssues((Dictionary<string, object>)queryParameters, totalAttempts, baseRetryIntervallMilliseconds);
            }
            else if (queryParameters is IssueMetaData)
            {
                issues = this.GetIssues((IssueQueryParameters)queryParameters, totalAttempts, baseRetryIntervallMilliseconds);
            }
            return issues;
        }

        /// <summary>
        /// Gets the list of issues matching the query parameters (null values are ignored)
        /// </summary>
        /// <param name="queryParameters">The parameters for the query (null values are ignored)</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The list of issues for a project</returns>
        public IList<Issue> GetIssues(Dictionary<string, object> queryParameters, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(null != queryParameters, "No query parameters defined");
            #endregion Contract

            IssueQueryParameters queryParameterObject = new IssueQueryParameters(queryParameters);
            return this.GetIssues(queryParameterObject, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets the list of issues matching the query parameters (null values are ignored)
        /// </summary>
        /// <param name="queryParameters">The parameters for the query (null values are ignored)</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The list of issues for a project</returns>
        public IList<Issue> GetIssues(IssueQueryParameters queryParameters, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetIssues({0}, {1}, {2})", "queryParameters", totalAttempts, baseRetryIntervallMilliseconds));

            IList<Issue> issues = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                NameValueCollection parameters = new NameValueCollection();
                if (null != queryParameters)
                {
                    if (!string.IsNullOrEmpty(queryParameters.ProjectIdentifier))
                    {
                        Trace.WriteLine(string.Format("RedmineClient.GetIssues QueryParameter Project: {0}", queryParameters.ProjectIdentifier));
                        Project project = this.GetProjectByIdentifier(queryParameters.ProjectIdentifier);
                        parameters.Add(RedmineKeys.PROJECT_ID, project.Id.ToString());
                    }
                    if (!string.IsNullOrEmpty(queryParameters.StateName))
                    {
                        Trace.WriteLine(string.Format("RedmineClient.GetIssues QueryParameter State: {0}", queryParameters.StateName));
                        IssueStatus state = this.GetIssueStateByName(queryParameters.StateName);
                        parameters.Add(RedmineKeys.STATUS_ID, state.Id.ToString());
                    }
                    if (!string.IsNullOrEmpty(queryParameters.PriorityName))
                    {
                        Trace.WriteLine(string.Format("RedmineClient.GetIssues QueryParameter Priority: {0}", queryParameters.PriorityName));
                        IssuePriority priority = this.GetIssuePriorityByName(queryParameters.PriorityName);
                        parameters.Add(RedmineKeys.PRIORITY_ID, priority.Id.ToString());
                    }
                    if (!string.IsNullOrEmpty(queryParameters.TrackerName))
                    {
                        Trace.WriteLine(string.Format("RedmineClient.GetIssues QueryParameter State: {0}", queryParameters.TrackerName));
                        Tracker tracker = this.GetTrackerByName(queryParameters.TrackerName);
                        parameters.Add(RedmineKeys.TRACKER_ID, tracker.Id.ToString());
                    }
                    if (!string.IsNullOrEmpty(queryParameters.AssignedToLogin))
                    {
                        Trace.WriteLine(string.Format("RedmineClient.GetIssues QueryParameter Assignee: {0}", queryParameters.AssignedToLogin));
                        User assignee = this.GetUserByLogin(queryParameters.AssignedToLogin);
                        parameters.Add(RedmineKeys.ASSIGNED_TO_ID, assignee.Id.ToString());
                    }
                }
                return redmineManager.GetObjects<Issue>(parameters);
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return issues;
        }

        /// <summary>
        /// Gets an issue
        /// </summary>
        /// <param name="id">The id of the issue</param>
        /// <returns>The issue</returns>
        public Issue GetIssue(int id)
        {
            return this.GetIssue(id, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets an issue
        /// </summary>
        /// <param name="id">The id of the issue</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The issue</returns>
        public Issue GetIssue(int id, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(id > 0, "No issue id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetIssue({0}, {1}, {2})", id, totalAttempts, baseRetryIntervallMilliseconds));

            Issue issue = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                return redmineManager.GetObject<Issue>(id.ToString(), new NameValueCollection());
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return issue;
        }

        /// <summary>
        /// Creates a new issue
        /// </summary>
        /// <param name="issue">The data for the issue to create</param>
        /// <returns>The new created issue</returns>
        public Issue CreateIssue(Issue issue)
        {
            return this.CreateIssue(issue, (IssueMetaData)null);
        }

        /// <summary>
        /// Creates a new issue
        /// </summary>
        /// <param name="issue">The data for the issue to create</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The new created issue</returns>
        public Issue CreateIssue(Issue issue, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            return this.CreateIssue(issue, (IssueMetaData)null, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Creates a new issue
        /// </summary>
        /// <param name="issue">The data for the issue to create</param>
        /// <param name="issueData">The meta data object for the issue</param>
        /// <returns>The new created issue</returns>
        public Issue CreateIssue(Issue issue, object issueData)
        {
            #region Contract
            Contract.Requires((issueData is Dictionary<string, object>) || (issueData is IssueMetaData), "issueData must be Dictionary<string, object> or IssueMetaData");
            #endregion Contract

            Issue createdIssue = null;
            if (issueData is Dictionary<string, object>)
            {
                createdIssue = this.CreateIssue(issue, (Dictionary<string, object>)issueData);
            }
            else if (issueData is IssueMetaData)
            {
                createdIssue = this.CreateIssue(issue, (AttachmentData)issueData);
            }
            return createdIssue;
        }

        /// <summary>
        /// Creates a new issue
        /// </summary>
        /// <param name="issue">The data for the issue to create</param>
        /// <param name="issueData">The meta data object for the issue</param>
        /// <returns>The new created issue</returns>
        public Issue CreateIssue(Issue issue, Dictionary<string, object> issueData)
        {
            #region Contract
            Contract.Requires(null != issueData, "No issue data defined");
            #endregion Contract

            IssueMetaData issueDataObject = new IssueMetaData(issueData);
            return this.CreateIssue(issue, issueDataObject);
        }

        /// <summary>
        /// Creates a new issue
        /// </summary>
        /// <param name="issue">The data for the issue to create</param>
        /// <param name="issueData">The meta data object for the issue</param>
        /// <returns>The new created issue</returns>
        public Issue CreateIssue(Issue issue, IssueMetaData issueData)
        {
            return this.CreateIssue(issue, issueData, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Creates a new issue
        /// </summary>
        /// <param name="issue">The data for the issue to create</param>
        /// <param name="issueData">The meta data object for the issue</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The new created issue</returns>
        public Issue CreateIssue(Issue issue, object issueData, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires((issueData is Dictionary<string, object>) || (issueData is IssueMetaData), "issueData must be Dictionary<string, object> or IssueMetaData");
            #endregion Contract

            Issue createdIssue = null;
            if (issueData is Dictionary<string, object>)
            {
                createdIssue = this.CreateIssue(issue, (Dictionary<string, object>)issueData, totalAttempts, baseRetryIntervallMilliseconds);
            }
            else if (issueData is IssueMetaData)
            {
                createdIssue = this.CreateIssue(issue, (AttachmentData)issueData, totalAttempts, baseRetryIntervallMilliseconds);
            }
            return createdIssue;
        }

        /// <summary>
        /// Creates a new issue
        /// </summary>
        /// <param name="issue">The data for the issue to create</param>
        /// <param name="issueData">The meta data object for the issue</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The new created issue</returns>
        public Issue CreateIssue(Issue issue, Dictionary<string, object> issueData, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(null != issueData, "No issue data defined");
            #endregion Contract

            IssueMetaData issueDataObject = new IssueMetaData(issueData);
            return this.CreateIssue(issue, issueDataObject, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Creates a new issue
        /// </summary>
        /// <param name="issue">The data for the issue to create</param>
        /// <param name="issueData">The meta data object for the issue</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The new created issue</returns>
        public Issue CreateIssue(Issue issue, IssueMetaData issueData, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(null != issue, "No issue defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.CreateIssue({0}, {1}, {2})", issue.Subject, totalAttempts, baseRetryIntervallMilliseconds));

            this.SetIssueMetaData(issueData, issue, totalAttempts, baseRetryIntervallMilliseconds);
            Issue createdIssue = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                return redmineManager.CreateObject(issue);
            }, totalAttempts, baseRetryIntervallMilliseconds);

            //Notes will not be saved when creating a new issue, so we have to make an update to save them.
            if (!string.IsNullOrEmpty(issueData.Notes))
            {
                createdIssue.Notes = issueData.Notes;
                createdIssue.PrivateNotes = issueData.PrivateNotes;
                createdIssue = this.UpdateIssue(createdIssue, totalAttempts, baseRetryIntervallMilliseconds);
            }

            return createdIssue;
        }

        /// <summary>
        /// Updates an issue
        /// </summary>
        /// <param name="issue">The data for the issue to update</param>
        /// <returns>The updated issue</returns>
        public Issue UpdateIssue(Issue issue)
        {
            return this.UpdateIssue(issue, (IssueMetaData)null);
        }

        /// <summary>
        /// Updates an issue
        /// </summary>
        /// <param name="issue">The data for the issue to update</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The updated issue</returns>
        public Issue UpdateIssue(Issue issue, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            return this.UpdateIssue(issue, (IssueMetaData)null, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Updates an issue
        /// </summary>
        /// <param name="issue">The data for the issue to update</param>
        /// <param name="issueData">The meta data object for the issue</param>
        /// <returns>The updated issue</returns>
        public Issue UpdateIssue(Issue issue, object issueData)
        {
            #region Contract
            Contract.Requires((issueData is Dictionary<string, object>) || (issueData is IssueMetaData), "issueData must be Dictionary<string, object> or IssueMetaData");
            #endregion Contract

            Issue updatedIssue = null;
            if (issueData is Dictionary<string, object>)
            {
                updatedIssue = this.UpdateIssue(issue, (Dictionary<string, object>)issueData);
            }
            else if (issueData is IssueMetaData)
            {
                updatedIssue = this.UpdateIssue(issue, (IssueMetaData)issueData);
            }
            return updatedIssue;
        }

        /// <summary>
        /// Updates an issue
        /// </summary>
        /// <param name="issue">The data for the issue to update</param>
        /// <param name="issueData">The meta data object for the issue</param>
        /// <returns>The updated issue</returns>
        public Issue UpdateIssue(Issue issue, Dictionary<string, object> issueData)
        {
            #region Contract
            Contract.Requires(null != issueData, "No issue data defined");
            #endregion Contract

            IssueMetaData issueDataObject = new IssueMetaData(issueData);
            return this.UpdateIssue(issue, issueDataObject);
        }

        /// <summary>
        /// Updates an issue
        /// </summary>
        /// <param name="issue">The data for the issue to update</param>
        /// <param name="issueData">The meta data object for the issue</param>
        /// <returns>The updated issue</returns>
        public Issue UpdateIssue(Issue issue, IssueMetaData issueData)
        {
            return this.UpdateIssue(issue, issueData, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Updates an issue
        /// </summary>
        /// <param name="issue">The data for the issue to update</param>
        /// <param name="issueData">The meta data object for the issue</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The updated issue</returns>
        public Issue UpdateIssue(Issue issue, object issueData, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires((issueData is Dictionary<string, object>) || (issueData is IssueMetaData), "issueData must be Dictionary<string, object> or IssueMetaData");
            #endregion Contract

            Issue updatedIssue = null;
            if (issueData is Dictionary<string, object>)
            {
                updatedIssue = this.UpdateIssue(issue, (Dictionary<string, object>)issueData, totalAttempts, baseRetryIntervallMilliseconds);
            }
            else if (issueData is IssueMetaData)
            {
                updatedIssue = this.UpdateIssue(issue, (IssueMetaData)issueData, totalAttempts, baseRetryIntervallMilliseconds);
            }
            return updatedIssue;
        }

        /// <summary>
        /// Updates an issue
        /// </summary>
        /// <param name="issue">The data for the issue to update</param>
        /// <param name="issueData">The meta data object for the issue</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The updated issue</returns>
        public Issue UpdateIssue(Issue issue, Dictionary<string, object> issueData, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(null != issueData, "No issue data defined");
            #endregion Contract

            IssueMetaData issueDataObject = new IssueMetaData(issueData);
            return this.UpdateIssue(issue, issueDataObject, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Updates an issue
        /// </summary>
        /// <param name="issue">The data for the issue to update</param>
        /// <param name="issueData">The meta data object for the issue</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The updated issue</returns>
        public Issue UpdateIssue(Issue issue, IssueMetaData issueData, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(null != issue, "No issue defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.CreateIssue({0}, {1}, {2})", issue.Subject, totalAttempts, baseRetryIntervallMilliseconds));

            this.SetIssueMetaData(issueData, issue, totalAttempts, baseRetryIntervallMilliseconds);
            Issue updatedIssue = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                redmineManager.UpdateObject(issue.Id.ToString(), issue);
                return this.GetIssue(issue.Id, totalAttempts, baseRetryIntervallMilliseconds);
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return updatedIssue;
        }

        /// <summary>
        /// Deletes an issue
        /// </summary>
        /// <param name="id">The id of the issue</param>
        /// <returns>True if the issue could be deleted</returns>
        public bool DeleteIssue(int id)
        {
            return this.DeleteIssue(id, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Deletes an issue
        /// </summary>
        /// <param name="id">The id of the issue</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>True if the issue could be deleted</returns>
        public bool DeleteIssue(int id, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(id > 0, "No issue id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.DeleteIssue({0}, {1}, {2})", id, totalAttempts, baseRetryIntervallMilliseconds));

            bool success = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                redmineManager.DeleteObject<Issue>(id.ToString(), new NameValueCollection());
                return true;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return success;
        }

        /// <summary>
        /// Sets the data in the issue according to the meta data object provided
        /// </summary>
        /// <param name="issueData">The meta data to set in the issue</param>
        /// <param name="issue">The issue to change</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        private void SetIssueMetaData(IssueMetaData issueData, Issue issue, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            if (null != issueData)
            {
                // Set User
                if (!string.IsNullOrEmpty(issueData.AssignedToLogin))
                {
                    IList<User> users = this.GetUsers(totalAttempts, baseRetryIntervallMilliseconds);
                    if (!string.IsNullOrEmpty(issueData.AssignedToLogin))
                    {
                        User assignedToUser = users.FirstOrDefault(u => u.Login == issueData.AssignedToLogin);
                        Contract.Assert(null != assignedToUser, string.Format("User '{0}' could not be found", issueData.AssignedToLogin));
                        issue.AssignedTo = new IdentifiableName()
                        {
                            Id = assignedToUser.Id,
                            Name = assignedToUser.Login,
                        };
                    }
                }

                // Set Project
                if (!string.IsNullOrEmpty(issueData.ProjectIdentifier))
                {
                    IList<Project> projects = this.GetProjects(totalAttempts, baseRetryIntervallMilliseconds);
                    Project project = projects.FirstOrDefault(p => p.Identifier == issueData.ProjectIdentifier);
                    Contract.Assert(null != project, string.Format("Project with identifier '{0}' could not be found", issueData.ProjectIdentifier));
                    issue.Project = new IdentifiableName()
                    {
                        Id = project.Id,
                        Name = project.Identifier,
                    };
                }

                // Set State
                if (!string.IsNullOrEmpty(issueData.StateName))
                {
                    IList<IssueStatus> states = this.GetIssueStates(totalAttempts, baseRetryIntervallMilliseconds);
                    IssueStatus state = states.FirstOrDefault(s => s.Name == issueData.StateName);
                    Contract.Assert(null != state, string.Format("State '{0}' could not be found", issueData.StateName));
                    issue.Status = new IdentifiableName()
                    {
                        Id = state.Id,
                        Name = state.Name,
                    };
                }

                // Set Priority
                if (!string.IsNullOrEmpty(issueData.PriorityName))
                {
                    IList<IssuePriority> priorities = this.GetIssuePriorities(totalAttempts, baseRetryIntervallMilliseconds);
                    IssuePriority priority = priorities.FirstOrDefault(p => p.Name == issueData.PriorityName);
                    Contract.Assert(null != priority, string.Format("Priority '{0}' could not be found", issueData.PriorityName));
                    issue.Priority = new IdentifiableName()
                    {
                        Id = priority.Id,
                        Name = priority.Name,
                    };
                }

                // Set Tracker
                if (!string.IsNullOrEmpty(issueData.TrackerName))
                {
                    IList<Tracker> trackers = this.GetTrackers(totalAttempts, baseRetryIntervallMilliseconds);
                    Tracker tracker = trackers.FirstOrDefault(p => p.Name == issueData.TrackerName);
                    Contract.Assert(null != tracker, string.Format("Tracker '{0}' could not be found", issueData.PriorityName));
                    issue.Tracker = new IdentifiableName()
                    {
                        Id = tracker.Id,
                        Name = tracker.Name,
                    };
                }

                //Set Notes
                if (!string.IsNullOrEmpty(issueData.Notes))
                {
                    issue.Notes = issueData.Notes;
                    issue.PrivateNotes = issueData.PrivateNotes;
                }
            }
        }

        #endregion Issues

        #region Attachments

        /// <summary>
        /// Gets the attachments of an issue
        /// </summary>
        /// <param name="issueId">The id of the issue</param>
        /// <returns>The attachments of the issues</returns>
        public IList<Attachment> GetAttachments(int issueId)
        {
            return this.GetAttachments(issueId, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets the attachments of an issue
        /// </summary>
        /// <param name="issueId">The id of the issue</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The attachments of the issues</returns>
        public IList<Attachment> GetAttachments(int issueId, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(issueId > 0, "No issue id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetAttachements({0}, {1}, {2})", issueId, totalAttempts, baseRetryIntervallMilliseconds));

            IList<Attachment> attachments = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                NameValueCollection parameters = new NameValueCollection();
                parameters.Add("include", "attachments");
                Issue issue = redmineManager.GetObject<Issue>(issueId.ToString(), parameters);
                return issue.Attachments;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return attachments;
        }

        /// <summary>
        /// Gets an attachment
        /// </summary>
        /// <param name="id">The id of the attachment</param>
        /// <returns>The specified attachment</returns>
        public Attachment GetAttachment(int id)
        {
            return this.GetAttachment(id, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets an attachment
        /// </summary>
        /// <param name="id">The id of the attachment</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The specified attachment</returns>
        public Attachment GetAttachment(int id, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(id > 0, "No attachment id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetAttachment({0}, {1}, {2})", id, totalAttempts, baseRetryIntervallMilliseconds));

            Attachment attachment = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                return redmineManager.GetObject<Attachment>(id.ToString(), new NameValueCollection());
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return attachment;
        }

        /// <summary>
        /// Creates a new attachment and appends it to an existing issue
        /// </summary>
        /// <param name="issueId">Issue to append the attachment</param>
        /// <param name="attachmentData">The data for the attachment</param>
        /// <returns>The new created attachment</returns>
        public Attachment CreateAttachment(int issueId, object attachmentData)
        {
            #region Contract
            Contract.Requires((attachmentData is Dictionary<string, object>) || (attachmentData is AttachmentData), "attachmentData must be Dictionary<string, object> or AttachmentData");
            #endregion Contract

            Attachment attachment = null;
            if (attachmentData is Dictionary<string, object>)
            {
                attachment = this.CreateAttachment(issueId, (Dictionary<string, object>)attachmentData);
            }
            else if (attachmentData is AttachmentData)
            {
                attachment = this.CreateAttachment(issueId, (AttachmentData)attachmentData);
            }
            return attachment;
        }

        /// <summary>
        /// Creates a new attachment and appends it to an existing issue
        /// </summary>
        /// <param name="issueId">Issue to append the attachment</param>
        /// <param name="attachmentData">The data for the attachment</param>
        /// <returns>The new created attachment</returns>
        public Attachment CreateAttachment(int issueId, Dictionary<string, object> attachmentData)
        {
            #region Contract
            Contract.Requires(null != attachmentData, "No attachment data defined");
            #endregion Contract

            AttachmentData attachmentDataObject = new AttachmentData(attachmentData);
            return this.CreateAttachment(issueId, attachmentDataObject);
        }

        /// <summary>
        /// Creates a new attachment and appends it to an existing issue
        /// </summary>
        /// <param name="issueId">Issue to append the attachment</param>
        /// <param name="attachmentData">The data for the attachment</param>
        /// <returns>The new created attachment</returns>
        public Attachment CreateAttachment(int issueId, AttachmentData attachmentData)
        {
            return this.CreateAttachment(issueId, attachmentData);
        }

        /// <summary>
        /// Creates a new attachment and appends it to an existing issue
        /// </summary>
        /// <param name="issueId">Issue to append the attachment</param>
        /// <param name="attachmentData">The data for the attachment</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The new created attachment</returns>
        public Attachment CreateAttachment(int issueId, object attachmentData, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires((attachmentData is Dictionary<string, object>) || (attachmentData is AttachmentData), "attachmentData must be Dictionary<string, object> or AttachmentData");
            #endregion Contract

            Attachment attachment = null;
            if (attachmentData is Dictionary<string, object>)
            {
                attachment = this.CreateAttachment(issueId, (Dictionary<string, object>)attachmentData, totalAttempts, baseRetryIntervallMilliseconds);
            }
            else if (attachmentData is AttachmentData)
            {
                attachment = this.CreateAttachment(issueId, (AttachmentData)attachmentData, totalAttempts, baseRetryIntervallMilliseconds);
            }
            return attachment;
        }

        /// <summary>
        /// Creates a new attachment and appends it to an existing issue
        /// </summary>
        /// <param name="issueId">Issue to append the attachment</param>
        /// <param name="attachmentData">The data for the attachment</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The new created attachment</returns>
        public Attachment CreateAttachment(int issueId, Dictionary<string, object> attachmentData, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(null != attachmentData, "No attachment data defined");
            #endregion Contract

            AttachmentData attachmentDataObject = new AttachmentData(attachmentData);
            return this.CreateAttachment(issueId, attachmentDataObject, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Creates a new attachment and appends it to an existing issue
        /// </summary>
        /// <param name="issueId">Issue to append the attachment</param>
        /// <param name="attachmentData">The data for the attachment</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The new created attachment</returns>
        public Attachment CreateAttachment(int issueId, AttachmentData attachmentData, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(issueId > 0, "No issue id defined");
            Contract.Requires(null != attachmentData, "No attachmentData defined");
            Contract.Requires(null != attachmentData.Content, "No attachment content defined");
            Contract.Requires(attachmentData.Content.Length > 0, "Attachment content is empty");
            Contract.Requires(!string.IsNullOrEmpty(attachmentData.FileName), "No file name defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetAttachment({0}, {1}, {2}, {3})", issueId, attachmentData.FileName, totalAttempts, baseRetryIntervallMilliseconds));

            Upload uploadedFile = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                return redmineManager.UploadFile(attachmentData.Content);
            }, totalAttempts, baseRetryIntervallMilliseconds);

            Issue issue = this.GetIssue(issueId, totalAttempts, baseRetryIntervallMilliseconds);

            issue.Uploads = new List<Upload>()
            {
                new Upload()
                {
                     ContentType = attachmentData.ContentType,
                     Description = attachmentData.Description,
                     FileName = attachmentData.FileName,
                     Token = uploadedFile.Token,                      
                }
            };
            issue.Notes = attachmentData.Notes;
            issue.PrivateNotes = attachmentData.PrivateNotes;

            Issue updatedIssue = this.UpdateIssue(issue, totalAttempts, baseRetryIntervallMilliseconds);
            IList<Attachment> attachments = this.GetAttachments(updatedIssue.Id, totalAttempts, baseRetryIntervallMilliseconds);

            //unfortunately we do not have the ID of the attachment, and the file name is not unique. So we load the latest file attached to the specified issue with the specified name.
            Attachment createdAttachment = attachments.OrderByDescending(a => a.Id)
                .FirstOrDefault(a => a.FileName == attachmentData.FileName);

            return createdAttachment;
        }

        #endregion Attachements

        #region Journals

        /// <summary>
        /// Gets the journal entries of an issue
        /// </summary>
        /// <param name="issueId">The id of the issue</param>
        /// <returns>The journal entries of the issues</returns>
        public IList<Journal> GetJournals(int issueId)
        {
            return this.GetJournals(issueId, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets the journal entries of an issue
        /// </summary>
        /// <param name="issueId">The id of the issue</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The journal entries of the issues</returns>
        public IList<Journal> GetJournals(int issueId, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(issueId > 0, "No issue id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetJournals({0}, {1}, {2})", issueId, totalAttempts, baseRetryIntervallMilliseconds));

            IList<Journal> journals = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                NameValueCollection parameters = new NameValueCollection();
                parameters.Add("include", "journals");
                Issue issue = redmineManager.GetObject<Issue>(issueId.ToString(), parameters);
                return issue.Journals;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return journals;
        }

        /// <summary>
        /// Gets a journal entry
        /// </summary>
        /// <param name="issueId">The id of the issue containing the journal entry</param>
        /// <param name="id">The id of the journal entry</param>
        /// <returns>The specified journal entry</returns>
        public Journal GetJournal(int issueId, int id)
        {
            return this.GetJournal(issueId, id, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets a journal entry
        /// </summary>
        /// <param name="issueId">The id of the issue containing the journal entry</param>
        /// <param name="id">The id of the journal entry</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The specified journal entry</returns>
        public Journal GetJournal(int issueId, int id, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(issueId > 0, "No journal id defined");
            Contract.Requires(id > 0, "No journal id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetJournals({0}, {1}, {2}, {3})", issueId, id, totalAttempts, baseRetryIntervallMilliseconds));

            IList<Journal> journals = this.GetJournals(issueId, totalAttempts, baseRetryIntervallMilliseconds);
            Journal journal = journals.FirstOrDefault(j => j.Id == id);

            if (null == journal)
            {
                //If there is no journal with the specified id throw an exception so that the behaviour is the same as for other redmine objects
                throw new RedmineException("Not Found");
            }

            return journal;
        }

        /// <summary>
        /// Creates a new journal entry and appends it to an existing issue
        /// </summary>
        /// <param name="issueId">Issue to append the journal entry</param>
        /// <param name="journalData">The data of the journal entry</param>
        /// <returns>The new created journal entry</returns>
        public Journal CreateJournal(int issueId, object journalData)
        {
            #region Contract
            Contract.Requires((journalData is Dictionary<string, object>) || (journalData is JournalData), "journalData must be Dictionary<string, object> or JournalData");
            #endregion Contract

            Journal journal = null;
            if (journalData is Dictionary<string, object>)
            {
                journal = this.CreateJournal(issueId, (Dictionary<string, object>)journalData);
            }
            else if (journalData is JournalData)
            {
                journal = this.CreateJournal(issueId, (JournalData)journalData);
            }
            return journal;
        }

        /// <summary>
        /// Creates a new journal entry and appends it to an existing issue
        /// </summary>
        /// <param name="issueId">Issue to append the journal entry</param>
        /// <param name="journalData">The data of the journal entry</param>
        /// <returns>The new created journal entry</returns>
        public Journal CreateJournal(int issueId, Dictionary<string, object> journalData)
        {
            #region Contract
            Contract.Requires(null != journalData, "No journal data defined");
            #endregion Contract

            JournalData journalDataObject = new JournalData(journalData);
            return this.CreateJournal(issueId, journalDataObject);
        }

        /// <summary>
        /// Creates a new journal entry and appends it to an existing issue
        /// </summary>
        /// <param name="issueId">Issue to append the journal entry</param>
        /// <param name="journalData">The data of the journal entry</param>
        /// <returns>The new created journal entry</returns>
        public Journal CreateJournal(int issueId, JournalData journalData)
        {
            return this.CreateJournal(issueId, journalData, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Creates a new journal entry and appends it to an existing issue
        /// </summary>
        /// <param name="issueId">Issue to append the journal entry</param>
        /// <param name="journalData">The data of the journal entry</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The new created journal entry</returns>
        public Journal CreateJournal(int issueId, object journalData, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires((journalData is Dictionary<string, object>) || (journalData is JournalData), "journalData must be Dictionary<string, object> or JournalData");
            #endregion Contract

            Journal journal = null;
            if (journalData is Dictionary<string, object>)
            {
                journal = this.CreateJournal(issueId, (Dictionary<string, object>)journalData, totalAttempts, baseRetryIntervallMilliseconds);
            }
            else if (journalData is JournalData)
            {
                journal = this.CreateJournal(issueId, (JournalData)journalData, totalAttempts, baseRetryIntervallMilliseconds);
            }
            return journal;
        }

        /// <summary>
        /// Creates a new journal entry and appends it to an existing issue
        /// </summary>
        /// <param name="issueId">Issue to append the journal entry</param>
        /// <param name="journalData">The data of the journal entry</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The new created journal entry</returns>
        public Journal CreateJournal(int issueId, Dictionary<string, object> journalData, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(null != journalData, "No journal data defined");
            #endregion Contract

            JournalData journalDataObject = new JournalData(journalData);
            return this.CreateJournal(issueId, journalDataObject, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Creates a new journal entry and appends it to an existing issue
        /// </summary>
        /// <param name="issueId">Issue to append the journal entry</param>
        /// <param name="journalData">The data of the journal entry</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The new created journal entry</returns>
        public Journal CreateJournal(int issueId, JournalData journalData, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(issueId > 0, "No issue id defined");
            Contract.Requires(null != journalData, "No journal data defined");
            Contract.Requires(!string.IsNullOrEmpty(journalData.Notes), "No journal content defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetJournals({0}, {1}, {2}, {3})", issueId, journalData.Notes, totalAttempts, baseRetryIntervallMilliseconds));

            Issue issue = this.GetIssue(issueId, totalAttempts, baseRetryIntervallMilliseconds);
            issue.Notes = journalData.Notes;
            issue.PrivateNotes = journalData.PrivateNotes;

            Issue updatedIssue = this.UpdateIssue(issue, totalAttempts, baseRetryIntervallMilliseconds);
            IList<Journal> journals = this.GetJournals(updatedIssue.Id, totalAttempts, baseRetryIntervallMilliseconds);

            //unfortunately journals we do not have the ID of the journal entry. So we load the latest journal entry.
            Journal createdJournal = journals.OrderByDescending(a => a.CreatedOn).FirstOrDefault();

            return createdJournal;
        }

        #endregion Notes

        #region Users

        /// <summary>
        /// Loads list of users
        /// </summary>
        /// <returns>The list of users</returns>
        public IList<User> GetUsers()
        {
            return this.GetUsers(this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Loads list of users
        /// </summary>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The list of users</returns>
        public IList<User> GetUsers(int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetUsers({0}, {1})", totalAttempts, baseRetryIntervallMilliseconds));

            var cachedUsers = GetCachedItems<User>();

            if (cachedUsers.Any())
                return cachedUsers;

            IList<User> users = InvokeWithRetries(() =>
            {
                var redmineManager = this.GetRedmineManager();
                var items = redmineManager.GetObjects<User>(new NameValueCollection());

                AddOrUpdateCachedIdentifiableItems(items);

                return items;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return users;
        }

        /// <summary>
        /// Loads a user
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <returns>The specified user</returns>
        public User GetUser(int id)
        {
            return this.GetUser(id, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Loads a user
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The specified user</returns>
        public User GetUser(int id, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(id > 0, "No user id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetUsers({0}, {1})", totalAttempts, baseRetryIntervallMilliseconds));

            var cachedItem = GetCachedItem<User>(id.ToString());

            if (cachedItem != null)
                return cachedItem;

            var user = InvokeWithRetries(() =>
            {
                var redmineManager = this.GetRedmineManager();
                var item = redmineManager.GetObject<User>(id.ToString(), new NameValueCollection());

                AddOrUpdateCachedIdentifiableItem(item);

                return item;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return user;
        }

        /// <summary>
        /// Gets a user by login name
        /// </summary>
        /// <param name="login">The login of the user</param>
        /// <returns>The user with the specified login</returns>
        public User GetUserByLogin(string login)
        {
            return this.GetUserByLogin(login, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets a user by login name
        /// </summary>
        /// <param name="login">The login of the user</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The user with the specified login</returns>
        public User GetUserByLogin(string login, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(!string.IsNullOrEmpty(login), "No user login name defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetUserByLogin({0}, {1}, {2})", login, totalAttempts, baseRetryIntervallMilliseconds));

            var users = GetUsers(totalAttempts, baseRetryIntervallMilliseconds);
            var user = users.FirstOrDefault(s => s.Login == login);

            if (user == null)
            {
                //If there is no user with the specified login throw an exception so that the behaviour is the same as for other redmine objects
                throw new RedmineException("Not Found");
            }

            return user;
        }

        /// <summary>
        /// Creates a user
        /// </summary>
        /// <param name="user">The data for the user to create</param>
        /// <returns>The specified user</returns>
        public User CreateUser(User user)
        {
            return this.CreateUser(user, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Creates a user
        /// </summary>
        /// <param name="user">The data for the user to create</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The specified user</returns>
        public User CreateUser(User user, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(null != user, "No user defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.CreateUser({0}, {1}, {2})", user.Id, totalAttempts, baseRetryIntervallMilliseconds));

            var createdUser = InvokeWithRetries(() =>
            {
                var redmineManager = GetRedmineManager();
                var item = redmineManager.CreateObject(user);

                AddOrUpdateCachedIdentifiableItem(item);

                return item;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return createdUser;
        }

        /// <summary>
        /// Creates a user
        /// </summary>
        /// <param name="user">The data for the user to create</param>
        /// <returns>The specified user</returns>
        public User UpdateUser(User user)
        {
            return this.UpdateUser(user, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Creates a user
        /// </summary>
        /// <param name="user">The data for the user to create</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The specified user</returns>
        public User UpdateUser(User user, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(null != user, "No user defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.UpdateUser({0}, {1}, {2})", user.Id, totalAttempts, baseRetryIntervallMilliseconds));

            var updatedUser = InvokeWithRetries(() =>
            {
                var redmineManager = GetRedmineManager();
                redmineManager.UpdateObject(user.Id.ToString(), user);
                RemoveCachedItem<User>(user.Id.ToString());

                return GetUser(user.Id, totalAttempts, baseRetryIntervallMilliseconds);

            }, totalAttempts, baseRetryIntervallMilliseconds);

            return updatedUser;
        }

        /// <summary>
        /// Deletes a user
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <returns>True if the issue could be deleted</returns>
        public bool DeleteUser(int id)
        {
            return this.DeleteUser(id, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Deletes a user
        /// </summary>
        /// <param name="id">The id of the user</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>True if the issue could be deleted</returns>
        public bool DeleteUser(int id, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(id > 0, "No user id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.DeleteUser({0}, {1}, {2})", id, totalAttempts, baseRetryIntervallMilliseconds));

            var success = InvokeWithRetries(() =>
            {
                var redmineManager = GetRedmineManager();
                redmineManager.DeleteObject<User>(id.ToString(), new NameValueCollection());
                RemoveCachedItem<User>(id.ToString());

                return true;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return success;
        }

        #endregion Users

        #region Membership

        /// <summary>
        /// Gets the list of users for the specified project
        /// </summary>
        /// <param name="projectIdentifier">The identifier of the project to get the users for</param>
        /// <returns>The list of users for the specified project</returns>
        public IList<ProjectUser> GetUsersInProject(string projectIdentifier)
        {
            return this.GetUsersInProject(projectIdentifier, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets the list of users for the specified project
        /// </summary>
        /// <param name="projectIdentifier">The identifier of the project to get the users for</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The list of users for the specified project</returns>
        public IList<ProjectUser> GetUsersInProject(string projectIdentifier, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(!string.IsNullOrEmpty(projectIdentifier), "No project idendifier defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Project project = this.GetProjectByIdentifier(projectIdentifier, totalAttempts, baseRetryIntervallMilliseconds);
            return this.GetUsersInProject(project.Id, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets the list of users for the specified project
        /// </summary>
        /// <param name="projectId">The id of the project to get the users for</param>
        /// <returns>The list of users for the specified project</returns>
        public IList<ProjectUser> GetUsersInProject(int projectId)
        {
            return this.GetUsersInProject(projectId, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets the list of users for the specified project
        /// </summary>
        /// <param name="projectId">The id of the project to get the users for</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The list of users for the specified project</returns>
        public IList<ProjectUser> GetUsersInProject(int projectId, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(projectId > 0, "No project id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetUsersInProject({0}, {1}, {2})", projectId, totalAttempts, baseRetryIntervallMilliseconds));

            IList<ProjectMembership> memberships = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                NameValueCollection parameters = new NameValueCollection();
                parameters.Add(RedmineKeys.PROJECT_ID, projectId.ToString());
                return redmineManager.GetObjects<ProjectMembership>(parameters);
            }, totalAttempts, baseRetryIntervallMilliseconds);

            IList<User> users = this.GetUsers(totalAttempts, baseRetryIntervallMilliseconds);
            List<ProjectUser> projectUsers = new List<ProjectUser>();
            foreach (ProjectMembership membership in memberships.Where(m => null != m.User)) //ignore memberships of groups
            {
                ProjectUser projectUser = RedmineClient.ToProjectUser(users, membership);
                projectUsers.Add(projectUser);
            }

            return projectUsers;
        }

        /// <summary>
        /// Add a user to a project
        /// </summary>
        /// <param name="projectIdentifier">The identifier of the project to get the users for</param>
        /// <param name="userLogin">The login of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser AddUserToProject(string projectIdentifier, string userLogin, object roleNames)
        {
            #region Contract
            Contract.Requires(roleNames is IList<string>, "roleNames must be IList<string>");
            #endregion Contract

            return this.AddUserToProject(projectIdentifier, userLogin, (IList<string>)roleNames);
        }

        /// <summary>
        /// Add a user to a project
        /// </summary>
        /// <param name="projectIdentifier">The identifier of the project to get the users for</param>
        /// <param name="userLogin">The login of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser AddUserToProject(string projectIdentifier, string userLogin, IList<string> roleNames)
        {
            return this.AddUserToProject(projectIdentifier, userLogin, roleNames, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Add a user to a project
        /// </summary>
        /// <param name="projectIdentifier">The identifier of the project to get the users for</param>
        /// <param name="userLogin">The login of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser AddUserToProject(string projectIdentifier, string userLogin, object roleNames, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(roleNames is IList<string>, "roleNames must be IList<string>");
            #endregion Contract

            return this.AddUserToProject(projectIdentifier, userLogin, (IList<string>)roleNames, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Add a user to a project
        /// </summary>
        /// <param name="projectIdentifier">The identifier of the project to get the users for</param>
        /// <param name="userLogin">The login of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser AddUserToProject(string projectIdentifier, string userLogin, IList<string> roleNames, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(!string.IsNullOrEmpty(projectIdentifier), "No project identifier defined");
            Contract.Requires(!string.IsNullOrEmpty(userLogin), "No user login defined");
            Contract.Requires(null != roleNames, "No roles defined");
            Contract.Requires(roleNames.Count > 0, "Role list can not be empty");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Project project = this.GetProjectByIdentifier(projectIdentifier, totalAttempts, baseRetryIntervallMilliseconds);
            User user = this.GetUserByLogin(userLogin, totalAttempts, baseRetryIntervallMilliseconds);

            return this.AddUserToProject(project.Id, user.Id, roleNames, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Add a user to a project
        /// </summary>
        /// <param name="projectId">The id of the project</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser AddUserToProject(int projectId, int userId, object roleNames)
        {
            #region Contract
            Contract.Requires(roleNames is IList<string>, "roleNames must be IList<string>");
            #endregion Contract

            return this.AddUserToProject(projectId, userId, (IList<string>)roleNames);
        }

        /// <summary>
        /// Add a user to a project
        /// </summary>
        /// <param name="projectId">The id of the project</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser AddUserToProject(int projectId, int userId, IList<string> roleNames)
        {
            return this.AddUserToProject(projectId, userId, roleNames, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Add a user to a project
        /// </summary>
        /// <param name="projectId">The id of the project</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser AddUserToProject(int projectId, int userId, object roleNames, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(roleNames is IList<string>, "roleNames must be IList<string>");
            #endregion Contract

            return this.AddUserToProject(projectId, userId, (IList<string>)roleNames, totalAttempts, baseRetryIntervallMilliseconds);
        }


        /// <summary>
        /// Add a user to a project
        /// </summary>
        /// <param name="projectId">The id of the project</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser AddUserToProject(int projectId, int userId, IList<string> roleNames, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(projectId > 0, "No project id defined");
            Contract.Requires(userId > 0, "No user id defined");
            Contract.Requires(null != roleNames, "No roles defined");
            Contract.Requires(roleNames.Count > 0, "Role list can not be empty");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.AddUserToProject({0}, {1}, {2}, {3}, {4})", projectId, userId, string.Join("|", roleNames), totalAttempts, baseRetryIntervallMilliseconds));

            ProjectMembership projectMembership = new ProjectMembership()
            {
                Project = new IdentifiableName() { Id = projectId },
                User = new IdentifiableName() { Id = userId },
                Roles = new List<MembershipRole>()
            };

            IList<Role> roles = this.GetRoles(totalAttempts, baseRetryIntervallMilliseconds);
            foreach (string roleName in roleNames)
            {
                Role role = roles.FirstOrDefault(r => r.Name == roleName);
                Contract.Assert(null != role, string.Format("No role wich name {0} found", roleName));
                MembershipRole membershipRole = new MembershipRole() { Id = role.Id };
                projectMembership.Roles.Add(membershipRole);
            }

            ProjectMembership createdMembership = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                return redmineManager.CreateObject<ProjectMembership>(projectMembership, projectId.ToString());
            }, totalAttempts, baseRetryIntervallMilliseconds);

            IList<User> users = this.GetUsers(totalAttempts, baseRetryIntervallMilliseconds);
            ProjectUser projectUser = RedmineClient.ToProjectUser(users, createdMembership);

            return projectUser;
        }

        /// <summary>
        /// Gets the roles of a user in a project
        /// </summary>
        /// <param name="projectIdentifier">The identifier of the project to get the users for</param>
        /// <param name="userLogin">The login of the user</param>
        /// <returns>The roles of a user in a project</returns>
        public IList<string> GetUserRoles(string projectIdentifier, string userLogin)
        {
            return this.GetUserRoles(projectIdentifier, userLogin, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets the roles of a user in a project
        /// </summary>
        /// <param name="projectIdentifier">The identifier of the project to get the users for</param>
        /// <param name="userLogin">The login of the user</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The roles of a user in a project</returns>
        public IList<string> GetUserRoles(string projectIdentifier, string userLogin, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(!string.IsNullOrEmpty(projectIdentifier), "No project identifier defined");
            Contract.Requires(!string.IsNullOrEmpty(userLogin), "No user login defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Project project = this.GetProjectByIdentifier(projectIdentifier, totalAttempts, baseRetryIntervallMilliseconds);
            User user = this.GetUserByLogin(userLogin, totalAttempts, baseRetryIntervallMilliseconds);

            return this.GetUserRoles(project.Id, user.Id, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets the roles of a user in a project
        /// </summary>
        /// <param name="projectId">The id of the project</param>
        /// <param name="userId">The id of the user</param>
        /// <returns>The roles of a user in a project</returns>
        public IList<string> GetUserRoles(int projectId, int userId)
        {
            return this.GetUserRoles(projectId, userId, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets the roles of a user in a project
        /// </summary>
        /// <param name="projectId">The id of the project</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The roles of a user in a project</returns>
        public IList<string> GetUserRoles(int projectId, int userId, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(projectId > 0, "No project id defined");
            Contract.Requires(userId > 0, "No user id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.AddUserToProject({0}, {1}, {2}, {3})", projectId, userId, totalAttempts, baseRetryIntervallMilliseconds));

            IList<ProjectUser> projectUsers = this.GetUsersInProject(projectId, totalAttempts, baseRetryIntervallMilliseconds);
            ProjectUser projectUser = projectUsers.FirstOrDefault(pu => pu.UserId == userId);
            if (null == projectUser)
            {
                //If there is no ProjectMembership with the specified user and project throw an exception so that the behaviour is the same as for other redmine objects
                throw new RedmineException("Not Found");
            }

            return projectUser.Roles;
        }

        /// <summary>
        /// Updates the roles a user has in a project
        /// </summary>
        /// <param name="projectIdentifier">The identifier of the project to get the users for</param>
        /// <param name="userLogin">The login of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser UpdateUserRoles(string projectIdentifier, string userLogin, object roleNames)
        {
            #region Contract
            Contract.Requires(roleNames is IList<string>, "roleNames must be IList<string>");
            #endregion Contract

            return this.UpdateUserRoles(projectIdentifier, userLogin, (IList<string>)roleNames);

        }

        /// <summary>
        /// Updates the roles a user has in a project
        /// </summary>
        /// <param name="projectIdentifier">The identifier of the project to get the users for</param>
        /// <param name="userLogin">The login of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser UpdateUserRoles(string projectIdentifier, string userLogin, IList<string> roleNames)
        {
            return this.UpdateUserRoles(projectIdentifier, userLogin, roleNames, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Updates the roles a user has in a project
        /// </summary>
        /// <param name="projectIdentifier">The identifier of the project to get the users for</param>
        /// <param name="userLogin">The login of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser UpdateUserRoles(string projectIdentifier, string userLogin, object roleNames, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(roleNames is IList<string>, "roleNames must be IList<string>");
            #endregion Contract

            return this.UpdateUserRoles(projectIdentifier, userLogin, (IList<string>)roleNames, totalAttempts, baseRetryIntervallMilliseconds);
        }


        /// <summary>
        /// Updates the roles a user has in a project
        /// </summary>
        /// <param name="projectIdentifier">The identifier of the project to get the users for</param>
        /// <param name="userLogin">The login of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser UpdateUserRoles(string projectIdentifier, string userLogin, IList<string> roleNames, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(!string.IsNullOrEmpty(projectIdentifier), "No project identifier defined");
            Contract.Requires(!string.IsNullOrEmpty(userLogin), "No user login defined");
            Contract.Requires(null != roleNames, "No roles defined");
            Contract.Requires(roleNames.Count > 0, "Role list can not be empty");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Project project = this.GetProjectByIdentifier(projectIdentifier, totalAttempts, baseRetryIntervallMilliseconds);
            User user = this.GetUserByLogin(userLogin, totalAttempts, baseRetryIntervallMilliseconds);

            return this.UpdateUserRoles(project.Id, user.Id, roleNames, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Updates the roles a user has in a project
        /// </summary>
        /// <param name="projectId">The id of the project</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser UpdateUserRoles(int projectId, int userId, object roleNames)
        {
            #region Contract
            Contract.Requires(roleNames is IList<string>, "roleNames must be IList<string>");
            #endregion Contract

            return this.UpdateUserRoles(projectId, userId, (IList<string>)roleNames);
        }

        /// <summary>
        /// Updates the roles a user has in a project
        /// </summary>
        /// <param name="projectId">The id of the project</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser UpdateUserRoles(int projectId, int userId, IList<string> roleNames)
        {
            return this.UpdateUserRoles(projectId, userId, roleNames, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Updates the roles a user has in a project
        /// </summary>
        /// <param name="projectId">The id of the project</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser UpdateUserRoles(int projectId, int userId, object roleNames, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(roleNames is IList<string>, "roleNames must be IList<string>");
            #endregion Contract

            return this.UpdateUserRoles(projectId, userId, (IList<string>)roleNames, totalAttempts, baseRetryIntervallMilliseconds);
        }


        /// <summary>
        /// Updates the roles a user has in a project
        /// </summary>
        /// <param name="projectId">The id of the project</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="roleNames">The names of the roles the user will have in the project</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser UpdateUserRoles(int projectId, int userId, IList<string> roleNames, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(projectId > 0, "No project id defined");
            Contract.Requires(userId > 0, "No user id defined");
            Contract.Requires(null != roleNames, "No roles defined");
            Contract.Requires(roleNames.Count > 0, "Role list can not be empty");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.AddUserToProject({0}, {1}, {2}, {3}, {4})", projectId, userId, string.Join("|", roleNames), totalAttempts, baseRetryIntervallMilliseconds));

            IList<ProjectMembership> memberships = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                NameValueCollection parameters = new NameValueCollection();
                parameters.Add(RedmineKeys.PROJECT_ID, projectId.ToString());
                return redmineManager.GetObjects<ProjectMembership>(parameters);
            }, totalAttempts, baseRetryIntervallMilliseconds);

            ProjectMembership toUpdate = memberships.FirstOrDefault(ms => ms.Project.Id == projectId && null != ms.User && ms.User.Id == userId);
            if (toUpdate == null)
            {
                //If there is no ProjectMembership with the specified user and project throw an exception so that the behaviour is the same as for other redmine objects
                throw new RedmineException("Not Found");
            }

            IList<Role> roles = this.GetRoles(totalAttempts, baseRetryIntervallMilliseconds);
            toUpdate.Roles = new List<MembershipRole>();
            foreach (string roleName in roleNames)
            {
                Role role = roles.FirstOrDefault(r => r.Name == roleName);
                Contract.Assert(null != role, string.Format("No role wich name {0} found", roleName));
                MembershipRole membershipRole = new MembershipRole() { Id = role.Id };
                toUpdate.Roles.Add(membershipRole);
            }

            ProjectMembership updatedMembership = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                redmineManager.UpdateObject<ProjectMembership>(toUpdate.Id.ToString(), toUpdate, projectId.ToString());
                return redmineManager.GetObject<ProjectMembership>(toUpdate.Id.ToString(), new NameValueCollection());
            }, totalAttempts, baseRetryIntervallMilliseconds);

            IList<User> users = this.GetUsers(totalAttempts, baseRetryIntervallMilliseconds);
            ProjectUser projectUser = RedmineClient.ToProjectUser(users, updatedMembership);

            return projectUser;
        }

        /// <summary>
        /// Removes a user from a project
        /// </summary>
        /// <param name="projectIdentifier">The identifier of the project to get the users for</param>
        /// <param name="userLogin">The login of the user</param>
        /// <returns>True if the user could be removed from the project</returns>
        public bool RemoveUserFromProject(string projectIdentifier, string userLogin)
        {
            return this.RemoveUserFromProject(projectIdentifier, userLogin, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Removes a user from a project
        /// </summary>
        /// <param name="projectIdentifier">The identifier of the project to get the users for</param>
        /// <param name="userLogin">The login of the user</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>True if the user could be removed from the project</returns>
        public bool RemoveUserFromProject(string projectIdentifier, string userLogin, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(!string.IsNullOrEmpty(projectIdentifier), "No project identifier defined");
            Contract.Requires(!string.IsNullOrEmpty(userLogin), "No user login defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Project project = this.GetProjectByIdentifier(projectIdentifier, totalAttempts, baseRetryIntervallMilliseconds);
            User user = this.GetUserByLogin(userLogin, totalAttempts, baseRetryIntervallMilliseconds);

            return this.RemoveUserFromProject(project.Id, user.Id, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Removes a user from a project
        /// </summary>
        /// <param name="projectId">The id of the project</param>
        /// <param name="userId">The id of the user</param>
        /// <returns>True if the user could be removed from the project</returns>
        public bool RemoveUserFromProject(int projectId, int userId)
        {
            return this.RemoveUserFromProject(projectId, userId, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Removes a user from a project
        /// </summary>
        /// <param name="projectId">The id of the project</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>True if the user could be removed from the project</returns>
        public bool RemoveUserFromProject(int projectId, int userId, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(projectId > 0, "No project id defined");
            Contract.Requires(userId > 0, "No user id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.RemoveUserFromProject({0}, {1}, {2}, {3})", projectId, userId, totalAttempts, baseRetryIntervallMilliseconds));

            IList<ProjectMembership> memberships = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                NameValueCollection parameters = new NameValueCollection();
                parameters.Add(RedmineKeys.PROJECT_ID, projectId.ToString());
                return redmineManager.GetObjects<ProjectMembership>(parameters);
            }, totalAttempts, baseRetryIntervallMilliseconds);

            ProjectMembership toDelete = memberships.FirstOrDefault(ms => ms.Project.Id == projectId && null != ms.User && ms.User.Id == userId);
            if (toDelete == null)
            {
                //If there is no ProjectMembership with the specified user and project throw an exception so that the behaviour is the same as for other redmine objects
                throw new RedmineException("Not Found");
            }

            bool success = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                redmineManager.DeleteObject<ProjectMembership>(toDelete.Id.ToString(), new NameValueCollection());
                return true;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return success;
        }

        /// <summary>
        /// Creates a project user from a project membership
        /// </summary>
        /// <param name="users">The list of all users</param>
        /// <param name="membership">The project membership</param>
        /// <returns>The project user created from the project membership</returns>
        private static ProjectUser ToProjectUser(IList<User> users, ProjectMembership membership)
        {
            User user = users.FirstOrDefault(u => u.Id == membership.User.Id);
            ProjectUser projectUser = new ProjectUser();
            projectUser.UserId = membership.User.Id;
            projectUser.UserLogin = user.Login;
            foreach (MembershipRole role in membership.Roles)
            {
                projectUser.Roles.Add(role.Name);
            }
            return projectUser;
        }

        #endregion Membership

        #region Load Items Selections

        /// <summary>
        /// Loads issue states
        /// </summary>
        /// <returns>The list of issue states</returns>
        public IList<IssueStatus> GetIssueStates()
        {
            return this.GetIssueStates(this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Loads issue states
        /// </summary>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The list of issue states</returns>
        public IList<IssueStatus> GetIssueStates(int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetIssueStates({0}, {1})", totalAttempts, baseRetryIntervallMilliseconds));

            var cachedItems = GetCachedItems<IssueStatus>();

            if (cachedItems.Any())
                return cachedItems;

            IList<IssueStatus> states = InvokeWithRetries(() =>
            {
                var redmineManager = GetRedmineManager();
                var items = redmineManager.GetObjects<IssueStatus>(new NameValueCollection());

                AddOrUpdateCachedIdentifiableNameItems(items);

                return items;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return states;
        }

        /// <summary>
        /// Gets a issue state object by the state name
        /// </summary>
        /// <param name="name">The name of the state</param>
        /// <returns>The state with the specified name</returns>
        public IssueStatus GetIssueStateByName(string name)
        {
            return this.GetIssueStateByName(name, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets a issue state object by the state name
        /// </summary>
        /// <param name="name">The name of the state</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The state with the specified name</returns>
        public IssueStatus GetIssueStateByName(string name, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(!string.IsNullOrEmpty(name), "No state name defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetIssueStateByName({0}, {1}, {2})", name, totalAttempts, baseRetryIntervallMilliseconds));

            var states = GetIssueStates(totalAttempts, baseRetryIntervallMilliseconds);
            var state = states.FirstOrDefault(s => s.Name == name);

            if (state == null)
            {
                //If there is no state with the specified name throw an exception so that the behaviour is the same as for other redmine objects
                throw new RedmineException("Not Found");
            }

            return state;
        }

        /// <summary>
        /// Loads issue priorities
        /// </summary>
        /// <returns>The list of issue priorities</returns>
        public IList<IssuePriority> GetIssuePriorities()
        {
            return this.GetIssuePriorities(this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Loads issue priorities
        /// </summary>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The list of issue priorities</returns>
        public IList<IssuePriority> GetIssuePriorities(int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetIssuePriorities({0}, {1})", totalAttempts, baseRetryIntervallMilliseconds));

            var cachedItems = GetCachedItems<IssuePriority>();

            if (cachedItems.Any())
                return cachedItems;

            IList<IssuePriority> priorities = InvokeWithRetries(() =>
            {
                var redmineManager = GetRedmineManager();
                var items = redmineManager.GetObjects<IssuePriority>(new NameValueCollection());

                AddOrUpdateCachedIdentifiableNameItems(items);

                return items;

            }, totalAttempts, baseRetryIntervallMilliseconds);

            return priorities;
        }

        /// <summary>
        /// Gets a issue priority object by the priority name
        /// </summary>
        /// <param name="name">The name of the priority</param>
        /// <returns>The priority with the specified name</returns>
        public IssuePriority GetIssuePriorityByName(string name)
        {
            return this.GetIssuePriorityByName(name, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets a issue priority object by the priority name
        /// </summary>
        /// <param name="name">The name of the priority</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The priority with the specified name</returns>
        public IssuePriority GetIssuePriorityByName(string name, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(!string.IsNullOrEmpty(name), "No priority name defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetIssuePriorityByName({0}, {1}, {2})", name, totalAttempts, baseRetryIntervallMilliseconds));

            var priortities = GetIssuePriorities(totalAttempts, baseRetryIntervallMilliseconds);
            var priority = priortities.FirstOrDefault(s => s.Name == name);

            if (priority == null)
            {
                //If there is no priority with the specified name throw an exception so that the behaviour is the same as for other redmine objects
                throw new RedmineException("Not Found");
            }

            return priority;
        }

        /// <summary>
        /// Loads list of trackers (issue type)
        /// </summary>
        /// <returns>Return the list of trackers (issue type)</returns>
        public IList<Tracker> GetTrackers()
        {
            return this.GetTrackers(this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Loads list of trackers (issue type)
        /// </summary>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The list of trackers (issue type)</returns>
        public IList<Tracker> GetTrackers(int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "baseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetUsers({0}, {1})", totalAttempts, baseRetryIntervallMilliseconds));

            var cachedItems = GetCachedItems<Tracker>();

            if (cachedItems.Any())
                return cachedItems;

            IList<Tracker> trackers = InvokeWithRetries(() =>
            {
                var redmineManager = GetRedmineManager();
                var items = redmineManager.GetObjects<Tracker>(new NameValueCollection());

                AddOrUpdateCachedIdentifiableNameItems(items);

                return items;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return trackers;
        }

        /// <summary>
        /// Gets a tracker (issue type) by name
        /// </summary>
        /// <param name="name">The name of the tracker</param>
        /// <returns>The tracker with the specified name</returns>
        public Tracker GetTrackerByName(string name)
        {
            return this.GetTrackerByName(name, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets a tracker (issue type) by name
        /// </summary>
        /// <param name="name">The name of the tracker</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The tracker with the specified name</returns>
        public Tracker GetTrackerByName(string name, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(!string.IsNullOrEmpty(name), "No tracker name defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetTrackerByName({0}, {1}, {2})", name, totalAttempts, baseRetryIntervallMilliseconds));

            var trackers = GetTrackers(totalAttempts, baseRetryIntervallMilliseconds);
            var tracker = trackers.FirstOrDefault(s => s.Name == name);

            if (tracker == null)
            {
                //If there is no tracker with the specified name throw an exception so that the behaviour is the same as for other redmine objects
                throw new RedmineException("Not Found");
            }

            return tracker;
        }

        /// <summary>
        /// Loads roles 
        /// </summary>
        /// <returns>The list of roles </returns>
        public IList<Role> GetRoles()
        {
            return this.GetRoles(this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Loads roles 
        /// </summary>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The list of roles </returns>
        public IList<Role> GetRoles(int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetMembershipRoles({0}, {1})", totalAttempts, baseRetryIntervallMilliseconds));

            IList<Role> roles = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                return redmineManager.GetObjects<Role>(new NameValueCollection());
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return roles;
        }

        /// <summary>
        /// Gets a role by the role name
        /// </summary>
        /// <param name="name">The name of the role</param>
        /// <returns>The role with the specified name</returns>
        public Role GetRoleByName(string name)
        {
            return this.GetRoleByName(name, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets a role by the role name
        /// </summary>
        /// <param name="name">The name of the role</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The role with the specified name</returns>
        public Role GetRoleByName(string name, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(!string.IsNullOrEmpty(name), "No membership role name defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetMembershipRoleByName({0}, {1}, {2})", name, totalAttempts, baseRetryIntervallMilliseconds));

            IList<Role> roles = this.GetRoles(totalAttempts, baseRetryIntervallMilliseconds);
            Role role = roles.FirstOrDefault(s => s.Name == name);
            if (role == null)
            {
                //If there is no priority with the specified name throw an exception so that the behaviour is the same as for other redmine objects
                throw new RedmineException("Not Found");
            }

            return role;
        }

        #endregion Load Items Selections

        #region Redmine API Access

        /// <summary>
        /// Create a redmine manager
        /// </summary>
        /// <returns>A new redmine manager</returns>
        private RedmineManager GetRedmineManager()
        {
            return this.GetRedmineManager(this.RedmineUrl, this.Username, this.Password);
        }

        /// <summary>
        /// Create a redmine manager
        /// </summary>
        /// <param name="redmineUrl">Url of the redmine API</param>
        /// <param name="username">The user name for authentication</param>
        /// <param name="password">The password for authentication</param>
        /// <returns>A new redmine manager</returns>
        private RedmineManager GetRedmineManager(string redmineUrl, string username, string password)
        {
            RedmineManager redmineManager = new RedmineManager
                (
                    redmineUrl
                    , 
                    username
                    , 
                    password
                    , 
                    MimeFormat.Json
                    , 
                    // ignore server certificate error (i.e. do not validate)
                    false
                    , 
                    // we really do not care about the proxy, but have to specify null 
                    // as this is a default parameter and we need to specify the next one
                    null
                    , 
                    // this is an ugly hack: as the underlying Redmine client sets the 
                    // SecurityProtocol and that enum does not have a default value, we 
                    // pass in the current setting (and know that the Redmine client will 
                    // use that one to overwrite the global setting again)
                    ServicePointManager.SecurityProtocol
                );
            redmineManager.PageSize = this.PageSize;
            return redmineManager;
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

        #region Caching

        private T GetCachedItem<T>(string id)
            where T : class, IEquatable<T>
        {
            var item = _cache.Get<T>(GenerateKey<T>(id));

            return item.Item;
        }

        private IList<T> GetCachedItems<T>()
            where T : class, IEquatable<T>
        {
            var cachedItems = _cache.GetAll<T>();
            var items = cachedItems as IList<CacheItem<T>> ?? cachedItems.ToList();

            return items.Any(p => p.CacheItemType == CacheItemType.Collection)
                ? items.Select(p => p.Item).ToList()
                : new List<T>();
        }

        private void AddOrUpdateCachedIdentifiableNameItem<T>(T item)
            where T : IdentifiableName, IEquatable<T>
        {
            AddOrUpdateCachedItem(item.Id.ToString(), item);
        }

        private void AddOrUpdateCachedIdentifiableItem<T>(T item)
            where T : Identifiable<T>, IEquatable<T>
        {
            AddOrUpdateCachedItem(item.Id.ToString(), item);
        }

        private void AddOrUpdateCachedItem<T>(string id, T item)
            where T : class, IEquatable<T>
        {
            var cacheItem = new CacheItem<T>
            {
                Key = GenerateKey<T>(id),
                Item = item,
                CacheItemType = CacheItemType.Entity
            };

            _cache.AddOrUpdate(GenerateKey<T>(id), cacheItem);
        }

        private void AddOrUpdateCachedIdentifiableNameItems<T>(IEnumerable<T> items)
            where T: IdentifiableName, IEquatable<T>
        {
            foreach (var item in items)
            {
                var cacheItem = new CacheItem<T>
                {
                    Key = GenerateKey<T>(item.Id.ToString()),
                    Item = item,
                    CacheItemType = CacheItemType.Collection
                };

                _cache.AddOrUpdate<T>(GenerateKey<T>(item.Id.ToString()), cacheItem);
            }
        }

        private void AddOrUpdateCachedIdentifiableItems<T>(IEnumerable<T> items)
            where T : Identifiable<T>, IEquatable<T>
        {
            foreach (var item in items)
            {
                var cacheItem = new CacheItem<T>
                {
                    Key = GenerateKey<T>(item.Id.ToString()),
                    Item = item,
                    CacheItemType = CacheItemType.Collection
                };

                _cache.AddOrUpdate(cacheItem.Key, cacheItem);
            }
        }

        private void RemoveCachedItem<T>(string id)
             where T : class, IEquatable<T>
        {
            _cache.Remove(GenerateKey<T>(id));
        }

        private static string GenerateKey<T>(string id)
            where T : class, IEquatable<T>
        {
            return GenerateKey(typeof(T).Name, id);
        }

        private static string GenerateKey(string typeName, string id)
        {
            return string.Format("{0}_{1}", typeName, id);
        }

        #endregion
    }
}
