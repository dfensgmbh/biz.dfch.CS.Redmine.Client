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

            Trace.WriteLine(string.Format("RedmineClient.Login({0}, {1}, {2}, {3}, {4})", redmineUrl, username, password, totalAttempts, baseRetryIntervallMilliseconds));

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

            Project project = RedmineClient.InvokeWithRetries(() =>
                {
                    RedmineManager redmineManager = this.GetRedmineManager();
                    return redmineManager.GetObject<Project>(id.ToString(), new NameValueCollection());
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
                Project parent = this.GetProjectByIdentifier(parentIdentifier);
                Contract.Assert(null != parent, string.Format("No project with identifier {0} found", parentIdentifier));
                project.Parent = new IdentifiableName() { Id = parent.Id };
            }

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
                Project parent = this.GetProjectByIdentifier(parentIdentifier);
                Contract.Assert(null != parent, string.Format("No project with identifier {0} found", parentIdentifier));
                project.Parent = new IdentifiableName() { Id = parent.Id };
            }

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
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The list of issues for a project</returns>
        public IList<Issue> GetIssues(int? projectId, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
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
            return this.CreateIssue(issue, null);
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
            return this.CreateIssue(issue, null, totalAttempts, baseRetryIntervallMilliseconds);
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
            return this.UpdateIssue(issue, null);
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
            return this.UpdateIssue(issue, null, totalAttempts, baseRetryIntervallMilliseconds);
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

            IList<User> users = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                return redmineManager.GetObjectList<User>(new NameValueCollection());
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

            User user = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                return redmineManager.GetObject<User>(id.ToString(), new NameValueCollection());
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

            IList<User> users = this.GetUsers(totalAttempts, baseRetryIntervallMilliseconds);

            User user = users.FirstOrDefault(s => s.Login == login);
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

            User createdUser = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                return redmineManager.CreateObject<User>(user);
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

            User updatedUser = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                redmineManager.UpdateObject(user.Id.ToString(), user);
                return this.GetUser(user.Id, totalAttempts, baseRetryIntervallMilliseconds);
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

            bool success = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                redmineManager.DeleteObject<User>(id.ToString(), new NameValueCollection());
                return true;
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return success;
        }

        #endregion Users

        #region Membership

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
                return redmineManager.GetObjectList<ProjectMembership>(parameters);
            }, totalAttempts, baseRetryIntervallMilliseconds);

            IList<User> users = this.GetUsers(totalAttempts, baseRetryIntervallMilliseconds);
            List<ProjectUser> projectUsers = new List<ProjectUser>();
            foreach (ProjectMembership membership in memberships.Where(m => null != m.User)) //ignore memberships of groups
            {
                ProjectUser projectUser = RedmineClient.CreateProjectUser(users, membership);
                projectUsers.Add(projectUser);
            }

            return projectUsers;
        }

        /// <summary>
        /// Add a user to a project
        /// </summary>
        /// <param name="projectId">The id of the project</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="rolesNames">The names of the roles the user will have i the project</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
        /// <returns>The info objec for the user in the project</returns>
        public ProjectUser AddUserToProject(int projectId, int userId, List<string> rolesNames, int totalAttempts, int baseRetryIntervallMilliseconds)
        {
            #region Contract
            Contract.Requires(this.IsLoggedIn, "Not logged in, call method login first");
            Contract.Requires(projectId > 0, "No project id defined");
            Contract.Requires(userId > 0, "No user id defined");
            Contract.Requires(null != rolesNames, "No roles defined");
            Contract.Requires(rolesNames.Count > 0, "Role list can not be empty");
            Contract.Requires(totalAttempts > 0, "TotalAttempts must be greater than 0");
            Contract.Requires(baseRetryIntervallMilliseconds > 0, "BaseRetryIntervallMilliseconds must be greater than 0");
            #endregion Contract

            Trace.WriteLine(string.Format("RedmineClient.AddUserToProject({0}, {1}, {2}, {3}, {4})", projectId, userId, string.Join("|", rolesNames), totalAttempts, baseRetryIntervallMilliseconds));

            ProjectMembership projectMembership = new ProjectMembership()
            {
                Project = new IdentifiableName() { Id = projectId },
                User = new IdentifiableName() { Id = userId },
                Roles = new List<MembershipRole>()
            };

            IList<Role> roles = this.GetRoles(totalAttempts, baseRetryIntervallMilliseconds);
            foreach (string roleName in rolesNames)
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
            List<ProjectUser> projectUsers = new List<ProjectUser>();
            ProjectUser projectUser = RedmineClient.CreateProjectUser(users, createdMembership);

            return projectUser;
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
                return redmineManager.GetObjectList<ProjectMembership>(parameters);
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
        private static ProjectUser CreateProjectUser(IList<User> users, ProjectMembership membership)
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

            IList<IssueStatus> states = RedmineClient.InvokeWithRetries(() =>
                {
                    RedmineManager redmineManager = this.GetRedmineManager();
                    return redmineManager.GetObjectList<IssueStatus>(new NameValueCollection());
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

            IList<IssueStatus> states = this.GetIssueStates(totalAttempts, baseRetryIntervallMilliseconds);
            IssueStatus state = states.FirstOrDefault(s => s.Name == name);
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

            IList<IssuePriority> priorities = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                return redmineManager.GetObjectList<IssuePriority>(new NameValueCollection());
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

            IList<IssuePriority> priortities = this.GetIssuePriorities(totalAttempts, baseRetryIntervallMilliseconds);
            IssuePriority priority = priortities.FirstOrDefault(s => s.Name == name);
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

            IList<Tracker> trackers = RedmineClient.InvokeWithRetries(() =>
            {
                RedmineManager redmineManager = this.GetRedmineManager();
                return redmineManager.GetObjectList<Tracker>(new NameValueCollection());
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return trackers;
        }

        /// <summary>
        /// Gets a tracker (issue type) by name
        /// </summary>
        /// <param name="login">The name of the tracker</param>
        /// <returns>The tracker with the specified name</returns>
        public Tracker GetTrackerByName(string name)
        {
            return this.GetTrackerByName(name, this.TotalAttempts, this.BaseRetryIntervallMilliseconds);
        }

        /// <summary>
        /// Gets a tracker (issue type) by name
        /// </summary>
        /// <param name="login">The name of the tracker</param>
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

            IList<Tracker> trackers = this.GetTrackers(totalAttempts, baseRetryIntervallMilliseconds);

            Tracker tracker = trackers.FirstOrDefault(s => s.Name == name);
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
                return redmineManager.GetObjectList<Role>(new NameValueCollection());
            }, totalAttempts, baseRetryIntervallMilliseconds);

            return roles;
        }

        /// <summary>
        /// Gets a role by the role name
        /// </summary>
        /// <param name="name">The name of the role</param>
        /// <param name="totalAttempts">Total attempts that are made for a request</param>
        /// <param name="baseRetryIntervallMilliseconds">Default base retry intervall milliseconds</param>
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
            return new RedmineManager(redmineUrl, username, password, MimeFormat.json, false);
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
