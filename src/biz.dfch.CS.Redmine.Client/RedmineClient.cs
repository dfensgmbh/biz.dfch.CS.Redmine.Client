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
using biz.dfch.CS.Redmine.Client.Model;
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
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
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
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
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
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>The project specified by the ID</returns>
        public Project GetProject(int id, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(id > 0, "No project id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetProject({0}, {1}, {2})", id, totalAttempts, baseRetryIntervallMilliseconds));

            Project project = RedmineClient.InvokeWithRetries(() =>
                {
                    RedmineManager redmineManager = this.GetRedmineManager();
                    return redmineManager.GetObject<Project>(id.ToString(), new NameValueCollection());
                }, totalAttempts, baseRetryIntervallMilliseconds);

            return project;
        }

        /// <summary>
        /// Creates a new project
        /// </summary>
        /// <param name="project">The project to create</param>
        /// <returns>The created project</returns>
        public Project CreateProject(Project project)
        {
            return this.CreateProject(project, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Creates a new project
        /// </summary>
        /// <param name="project">The project to create</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>The created project</returns>
        public Project CreateProject(Project project, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(null != project, "No project defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.CreateProject({0}, {1}, {2})", project.Name, totalAttempts, baseRetryIntervallMilliseconds));

            Project createdProject = RedmineClient.InvokeWithRetries(() =>
                {
                    RedmineManager redmineManager = this.GetRedmineManager();
                    return redmineManager.CreateObject(project);
                }, totalAttempts, baseRetryIntervallMilliseconds);

            return createdProject;
        }

        /// <summary>
        /// Updates a project
        /// </summary>
        /// <param name="project">The new project data</param>
        /// <returns>The updated project</returns>
        public Project UpdateProject(Project project)
        {
            return this.UpdateProject(project, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Updates a project
        /// </summary>
        /// <param name="project">The new project data</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>The updated project</returns>
        public Project UpdateProject(Project project, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(null != project, "No project defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.UpdateProject({0}, {1}, {2})", project.Name, totalAttempts, baseRetryIntervallMilliseconds));

            Project updatedProject = RedmineClient.InvokeWithRetries(() =>
                {
                    RedmineManager redmineManager = this.GetRedmineManager();
                    redmineManager.UpdateObject(project.Id.ToString(), project);
                    return this.GetProject(project.Id, totalAttempts, baseRetryIntervallMilliseconds);
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
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>True if the project could be deleted</returns>
        public bool DeleteProject(int id, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(id > 0, "No project id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.DeleteProject({0}, {1}, {2})", id, totalAttempts, baseRetryIntervallMilliseconds));

            bool success = RedmineClient.InvokeWithRetries(() =>
                {
                    RedmineManager redmineManager = this.GetRedmineManager();
                    redmineManager.DeleteObject<Project>(id.ToString(), new NameValueCollection());
                    return true;
                }, totalAttempts, baseRetryIntervallMilliseconds);

            return success;
        }

        #endregion Projects

        #region Issues

        /// <summary>
        /// Gets the list of issues for the specified project or for all projects if project id is not specified
        /// </summary>
        /// <param name="projectId">The id of the project to get the issues for</param>
        /// <returns>The list of issues for a project</returns>
        public IList<Issue> GetIssues(int? projectId)
        {
            return this.GetIssues(projectId, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets the list of issues for the specified project or for all projects if project id is not specified
        /// </summary>
        /// <param name="projectId">The id of the project to get the issues for</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>The list of issues for a project</returns>
        public IList<Issue> GetIssues(int? projectId, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetIssues({0}, {1}, {2})", projectId, totalAttempts, baseRetryIntervallMilliseconds));

            IList<Issue> issues = RedmineClient.InvokeWithRetries(() =>
                {
                    RedmineManager redmineManager = this.GetRedmineManager();
                    NameValueCollection parameters = new NameValueCollection();
                    if (projectId.HasValue)
                    {
                        parameters.Add(RedmineKeys.PROJECT_ID, projectId.ToString());
                    }
                    return redmineManager.GetObjectList<Issue>(parameters);
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
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>The issue</returns>
        public Issue GetIssue(int id, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(id > 0, "No issue id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
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
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>The new created issue</returns>
        public Issue CreateIssue(Issue issue)
        {
            return this.CreateIssue(issue, null);
        }

        /// <summary>
        /// Creates a new issue
        /// </summary>
        /// <param name="issue">The data for the issue to create</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>The new created issue</returns>
        public Issue CreateIssue(Issue issue, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            return this.CreateIssue(issue, null, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Creates a new issue
        /// </summary>
        /// <param name="issue">The data for the issue to create</param>
        /// <param name="issueData">The meta data object for the issue</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
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
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>The new created issue</returns>
        public Issue CreateIssue(Issue issue, IssueMetaData issueData, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(null != issue, "No issue defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.CreateIssue({0}, {1}, {2})", issue.Subject, totalAttempts, baseRetryIntervallMilliseconds));

            this.SetIssueMetaData(issueData, issue, totalAttempts, baseRetryIntervallMilliseconds);
            Issue createdIssue = RedmineClient.InvokeWithRetries(() =>
                {
                    RedmineManager redmineManager = this.GetRedmineManager();
                    return redmineManager.CreateObject(issue);
                }, totalAttempts, baseRetryIntervallMilliseconds);

            return createdIssue;
        }

        /// <summary>
        /// Updates an issue
        /// </summary>
        /// <param name="issue">The data for the issue to update</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>The updated issue</returns>
        public Issue UpdateIssue(Issue issue)
        {
            return this.UpdateIssue(issue, null);
        }

        /// <summary>
        /// Updates an issue
        /// </summary>
        /// <param name="issue">The data for the issue to update</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>The updated issue</returns>
        public Issue UpdateIssue(Issue issue, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            return this.UpdateIssue(issue, null, totalAttempts, baseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Updates an issue
        /// </summary>
        /// <param name="issue">The data for the issue to update</param>
        /// <param name="issueData">The meta data object for the issue</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
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
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>The updated issue</returns>
        public Issue UpdateIssue(Issue issue, IssueMetaData issueData, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(null != issue, "No issue defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.CreateIssue({0}, {1}, {2})", issue.Subject, totalAttempts, baseRetryIntervallMilliseconds));

            this.SetIssueMetaData(issueData, issue, totalAttempts, baseRetryIntervallMilliseconds);
            Issue createdIssue = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                redmineManager.UpdateObject(issue.Id.ToString(), issue);
                return this.GetIssue(issue.Id, totalAttempts, baseRetryIntervallMilliseconds);
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return createdIssue;
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
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>True if the issue could be deleted</returns>
        public bool DeleteIssue(int id, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(id > 0, "No issue id defined");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
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
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds in job polling</param>
        private void SetIssueMetaData(IssueMetaData issueData, Issue issue, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            if (null != issueData)
            {
                // Set User
                if ((!string.IsNullOrEmpty(issueData.AssignedToLogin)) || (!string.IsNullOrEmpty(issueData.AuthorLogin)))
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

                        User authorUser = users.FirstOrDefault(u => u.Login == issueData.AuthorLogin);
                        Contract.Assert(null != authorUser, string.Format("User '{0}' could not be found", issueData.AuthorLogin));
                        issue.Author = new IdentifiableName()
                        {
                            Id = authorUser.Id,
                            Name = authorUser.Login,
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
                if (!string.IsNullOrEmpty(issueData.StateName))
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
            }
        }

        #endregion Issues

        #region Load Items Selections

        /// <summary>
        /// Loads issues states
        /// </summary>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseWaitingMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>Return the list of issues states</returns>
        private IList<IssueStatus> GetIssueStates(int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetIssueStates({0}, {1})", totalAttempts, baseRetryIntervallMilliseconds));

            IList<IssueStatus> states = RedmineClient.InvokeWithRetries(() =>
                {
                    RedmineManager redmineManager = this.GetRedmineManager();
                    return redmineManager.GetObjectList<IssueStatus>(new NameValueCollection());
                }, totalAttempts, baseRetryIntervallMilliseconds);

            return states;
        }

        /// <summary>
        /// Loads issues priorities
        /// </summary>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseWaitingMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>Return the list of issues priorities</returns>
        private IList<IssuePriority> GetIssuePriorities(int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetIssuePriorities({0}, {1})", totalAttempts, baseRetryIntervallMilliseconds));

            IList<IssuePriority> priorities = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                return redmineManager.GetObjectList<IssuePriority>(new NameValueCollection());
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return priorities;
        }

        /// <summary>
        /// Loads list of users
        /// </summary>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseWaitingMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>Return the list of users</returns>
        private IList<User> GetUsers(int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetUsers({0}, {1})", totalAttempts, baseRetryIntervallMilliseconds));

            IList<User> users = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                return redmineManager.GetObjectList<User>(new NameValueCollection());
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return users;
        }

        /// <summary>
        /// Loads list of trackers (issue type)
        /// </summary>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseWaitingMilliseconds">Default base retry intervall milliseconds in job polling</param>
        /// <returns>Return the list of trackers (issue type)</returns>
        private IList<Tracker> GetTrackers(int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseWaitingMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.GetUsers({0}, {1})", totalAttempts, baseRetryIntervallMilliseconds));

            IList<Tracker> trackers = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                return redmineManager.GetObjectList<Tracker>(new NameValueCollection());
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return trackers;
        }

        #endregion Load Items Selections

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
