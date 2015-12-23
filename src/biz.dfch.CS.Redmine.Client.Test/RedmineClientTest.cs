﻿using System;
using System.Collections.Generic;
using System.IO;
using biz.dfch.CS.Redmine.Client.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Redmine.Net.Api.Types;
using System.Linq;

namespace biz.dfch.CS.Redmine.Client.Test
{
    [TestClass]
    public class RedmineClientTest
    {
        [TestMethod]
        public void DummyTestForTeamCity()
        {
        }

        #region Login

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void LoginCorrectCredentials()
        {
            RedmineClient redmineClient = new RedmineClient();
            bool success = redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsTrue(success, "Could not log in.");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void LoginWrongServerUrl()
        {
            RedmineClient redmineClient = new RedmineClient();

            try
            {
                bool success = redmineClient.Login("http://notAServer:8080/redmine", TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Bad domain name"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void LoginInvalidApiKey()
        {
            RedmineClient redmineClient = new RedmineClient();

            try
            {
                bool success = redmineClient.Login(TestEnvironment.RedminUrl, "NotAUser", TestEnvironment.RedminePassword, 3, 100);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Unauthorized"));
            }

        }

        #endregion Login

        #region Projects

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetProjectList()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<Project> projects = redmineClient.GetProjects(TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(projects, "No projects received");
            Assert.IsTrue(projects.Count > 0, "Project list is empty");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Project project = redmineClient.GetProject(TestEnvironment.ProjectId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(project, "No project received");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetProjectInvalidId()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                Project project = redmineClient.GetProject(Int16.MaxValue, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetProjectByIdentifier()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Project project = redmineClient.GetProjectByIdentifier(TestEnvironment.ProjectIdentifier1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(project, "No user received");
            Assert.AreEqual(TestEnvironment.ProjectIdentifier1, project.Identifier, "Wrong project returned");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetProjectInvalidIdentifier()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                Project project = redmineClient.GetProjectByIdentifier("NotAProject", TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Project project = new Project()
            {
                Description = "This project was created via API",
                Identifier = Guid.NewGuid().ToString(),
                IsPublic = false,
                Name = "Created via API",
            };
            Project createdProject = redmineClient.CreateProject(project, null, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(createdProject, "No project received");
            Assert.IsTrue(createdProject.Id > 0, "No Id defined in returned project");
            Assert.AreEqual(project.Description, createdProject.Description, "Description was not set correctly");
            Assert.AreEqual(project.Identifier, createdProject.Identifier, "Identifier was not set correctly");
            Assert.AreEqual(project.IsPublic, createdProject.IsPublic, "IsPublic was not set correctly");
            Assert.AreEqual(project.Name, createdProject.Name, "Name was not set correctly");
            Assert.IsNull(project.Parent, "Project was created as child project");

            redmineClient.DeleteProject(createdProject.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateChildProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Project project = new Project()
            {
                Description = "This child project was created via API",
                Identifier = Guid.NewGuid().ToString(),
                IsPublic = false,
                Name = "Created via API",
            };
            Project createdProject = redmineClient.CreateProject(project, TestEnvironment.ProjectIdentifier1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(createdProject, "No project received");
            Assert.IsTrue(createdProject.Id > 0, "No Id defined in returned project");
            Assert.AreEqual(project.Description, createdProject.Description, "Description was not set correctly");
            Assert.AreEqual(project.Identifier, createdProject.Identifier, "Identifier was not set correctly");
            Assert.AreEqual(project.IsPublic, createdProject.IsPublic, "IsPublic was not set correctly");
            Assert.AreEqual(project.Name, createdProject.Name, "Name was not set correctly");
            Assert.IsNotNull(project.Parent, "No parent project received");
            Assert.AreEqual(redmineClient.GetProjectByIdentifier(TestEnvironment.ProjectIdentifier1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds).Id, createdProject.Parent.Id, "Parent project was not set correctly");

            redmineClient.DeleteProject(createdProject.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Project project = new Project()
            {
                Description = "This project must be updated",
                Identifier = Guid.NewGuid().ToString(),
                IsPublic = false,
                Name = "To Update",
            };
            Project createdProject = redmineClient.CreateProject(project, null, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            createdProject.Description = "This project was updated via API";
            createdProject.Name = "Update Project";
            createdProject.IsPublic = true;

            Project updatedProject = redmineClient.UpdateProject(createdProject, null, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(updatedProject, "No project received");
            Assert.AreEqual(createdProject.Description, updatedProject.Description, "CreatedOn was not set correctly");
            Assert.AreEqual(createdProject.IsPublic, updatedProject.IsPublic, "CreatedOn was not set correctly");
            Assert.AreEqual(createdProject.Name, updatedProject.Name, "CreatedOn was not set correctly");

            redmineClient.DeleteProject(createdProject.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateProjectToChildProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Project project = new Project()
            {
                Description = "This project must be updated",
                Identifier = Guid.NewGuid().ToString(),
                IsPublic = false,
                Name = "To Update",
            };
            Project createdProject = redmineClient.CreateProject(project, null, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNull(createdProject.Parent, "Parent already defined before updating");

            Project updatedProject = redmineClient.UpdateProject(createdProject, TestEnvironment.ProjectIdentifier1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(updatedProject, "No project received");
            Assert.IsNotNull(updatedProject.Parent, "No parent project received");
            Assert.AreEqual(redmineClient.GetProjectByIdentifier(TestEnvironment.ProjectIdentifier1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds).Id, updatedProject.Parent.Id, "Parent project was not set correctly");

            redmineClient.DeleteProject(createdProject.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateProjectToRootProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Project project = new Project()
            {
                Description = "This project must be updated",
                Identifier = Guid.NewGuid().ToString(),
                IsPublic = false,
                Name = "To Update",
            };
            Project createdProject = redmineClient.CreateProject(project, TestEnvironment.ProjectIdentifier1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(createdProject.Parent, "Parent not defined before updating");

            Project updatedProject = redmineClient.UpdateProject(createdProject, TestEnvironment.ProjectIdentifier2, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(updatedProject, "No project received");
            Assert.IsNotNull(updatedProject.Parent, "No parent project received");
            Assert.AreEqual(redmineClient.GetProjectByIdentifier(TestEnvironment.ProjectIdentifier2, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds).Id, updatedProject.Parent.Id, "Parent project was not set correctly");

            redmineClient.DeleteProject(createdProject.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateProjectChangeParent()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Project project = new Project()
            {
                Description = "This project must be updated",
                Identifier = Guid.NewGuid().ToString(),
                IsPublic = false,
                Name = "To Update",
            };
            Project createdProject = redmineClient.CreateProject(project, TestEnvironment.ProjectIdentifier1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(createdProject.Parent, "Parent not defined before updating");
            createdProject.Parent = null;

            Project updatedProject = redmineClient.UpdateProject(createdProject, null, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(updatedProject, "No project received");
            Assert.IsNull(updatedProject.Parent, "Parent not removed");

            redmineClient.DeleteProject(createdProject.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void DeleteProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Project project = new Project()
            {
                Description = "This project was created via API",
                Identifier = Guid.NewGuid().ToString(),
                IsPublic = false,
                Name = "Created via API",
            };
            Project createdProject = redmineClient.CreateProject(project, null, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Project loadedProject = redmineClient.GetProject(createdProject.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsNotNull(loadedProject, "Project was not created correctly");

            bool success = redmineClient.DeleteProject(createdProject.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsTrue(success, "Did not receive success");

            try
            {
                Project loadedProjectAfterDeletion = redmineClient.GetProject(createdProject.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        #endregion Projects

        #region Issues

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueList()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<Issue> issues = redmineClient.GetIssues(TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(issues, "No issues received");
            Assert.IsTrue(issues.Count > 0, "Issue list is empty");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueListOfProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IssueQueryParameters queryParameters = new IssueQueryParameters()
            {
                ProjectIdentifier = TestEnvironment.ProjectIdentifier1,
            };
            IList<Issue> issues = redmineClient.GetIssues(queryParameters, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(issues, "No issues received");
            Assert.IsTrue(issues.Count > 0, "Issue list is empty");
            foreach (Issue issue in issues)
            {
                Assert.AreEqual(TestEnvironment.ProjectId, issue.Project.Id, "Issue from wrong project loaded");
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueListFilteredAccordingToState()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IssueQueryParameters queryParameters = new IssueQueryParameters()
            {
                ProjectIdentifier = TestEnvironment.ProjectIdentifier1,
                StateName = "In Progress",
            };
            IList<Issue> issues = redmineClient.GetIssues(queryParameters, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(issues, "No issues received");
            Assert.IsTrue(issues.Count > 0, "Issue list is empty");
            foreach (Issue issue in issues)
            {
                Assert.AreEqual(TestEnvironment.ProjectId, issue.Project.Id, "Issue from wrong project loaded");
                Assert.AreEqual(queryParameters.StateName, issue.Status.Name, "Issue with wrong state loaded");
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueListFilteredAccordingToStateUsingObjects()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            string stateName = "In Progress";
            object queryParameters = new Dictionary<string, object>()
            {
                {IssueQueryParameters.StateNameKey, stateName},
                {IssueQueryParameters.ProjectIdentifierKey,TestEnvironment.ProjectIdentifier1},
            };
            IList<Issue> issues = redmineClient.GetIssues(queryParameters, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(issues, "No issues received");
            Assert.IsTrue(issues.Count > 0, "Issue list is empty");
            foreach (Issue issue in issues)
            {
                Assert.AreEqual(TestEnvironment.ProjectId, issue.Project.Id, "Issue from wrong project loaded");
                Assert.AreEqual(stateName, issue.Status.Name, "Issue with wrong state loaded");
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueListFilteredAccordingToPriority()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IssueQueryParameters queryParameters = new IssueQueryParameters()
            {
                ProjectIdentifier = TestEnvironment.ProjectIdentifier1,
                PriorityName = "High",
            };
            IList<Issue> issues = redmineClient.GetIssues(queryParameters, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(issues, "No issues received");
            Assert.IsTrue(issues.Count > 0, "Issue list is empty");
            foreach (Issue issue in issues)
            {
                Assert.AreEqual(TestEnvironment.ProjectId, issue.Project.Id, "Issue from wrong project loaded");
                Assert.AreEqual(queryParameters.PriorityName, issue.Priority.Name, "Issue with wrong priority loaded");
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueListFilteredAccortingToTracker()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IssueQueryParameters queryParameters = new IssueQueryParameters()
            {
                ProjectIdentifier = TestEnvironment.ProjectIdentifier1,
                TrackerName = "Feature",
            };
            IList<Issue> issues = redmineClient.GetIssues(queryParameters, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(issues, "No issues received");
            Assert.IsTrue(issues.Count > 0, "Issue list is empty");
            foreach (Issue issue in issues)
            {
                Assert.AreEqual(TestEnvironment.ProjectId, issue.Project.Id, "Issue from wrong project loaded");
                Assert.AreEqual(queryParameters.TrackerName, issue.Tracker.Name, "Issue from wrong tracker loaded");
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueListFilteredAccortingToAssignee()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IssueQueryParameters queryParameters = new IssueQueryParameters()
            {
                ProjectIdentifier = TestEnvironment.ProjectIdentifier1,
                AssignedToLogin = "test",
            };
            IList<Issue> issues = redmineClient.GetIssues(queryParameters, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(issues, "No issues received");
            Assert.IsTrue(issues.Count > 0, "Issue list is empty");
            foreach (Issue issue in issues)
            {
                Assert.AreEqual(TestEnvironment.ProjectId, issue.Project.Id, "Issue from wrong project loaded");
                Assert.AreEqual(TestEnvironment.UserId2, issue.AssignedTo.Id, "Issue from wrong user loaded");
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueListInvalidProjectId()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                IssueQueryParameters queryParameters = new IssueQueryParameters()
                {
                    ProjectIdentifier = "NotAProject",
                };
                IList<Issue> issues = redmineClient.GetIssues(queryParameters, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssue()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Issue issue = redmineClient.GetIssue(TestEnvironment.IssueId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(issue, "No issues received");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueInvalidId()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                Issue issue = redmineClient.GetIssue(int.MaxValue, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateIssue()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Issue issue = new Issue()
            {
                Subject = "Created via API",
                Description = "This issue was created via API",
                DueDate = DateTime.Today.AddDays(3),
                IsPrivate = true,
            };
            IssueMetaData metaData = new IssueMetaData()
            {
                AssignedToLogin = TestEnvironment.UserLogin2,
                PriorityName = "High",
                TrackerName = "Feature",
                StateName = "New",
                ProjectIdentifier = TestEnvironment.ProjectIdentifier1,
                Notes = "This is a new issue with a note",
                PrivateNotes = true,
            };
            Issue createdIssue = redmineClient.CreateIssue(issue, metaData, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(createdIssue, "No issue received");
            Assert.IsTrue(createdIssue.Id > 0, "No Id defined in returned issue");
            Assert.AreEqual(issue.Subject, createdIssue.Subject, "Subject was not set correctly");
            Assert.AreEqual(issue.Description, createdIssue.Description, "Description was not set correctly");
            Assert.AreEqual(issue.DueDate, createdIssue.DueDate, "DueDate was not set correctly");
            Assert.AreEqual(issue.IsPrivate, createdIssue.IsPrivate, "IsPrivate was not set correctly");

            Assert.AreEqual(redmineClient.GetUserByLogin(metaData.AssignedToLogin, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds).Id, createdIssue.AssignedTo.Id, "AssignedTo was not set correctly");
            Assert.AreEqual(metaData.PriorityName, createdIssue.Priority.Name, "Priority was not set correctly");
            Assert.AreEqual(metaData.TrackerName, createdIssue.Tracker.Name, "Tracker was not set correctly");
            Assert.AreEqual(metaData.StateName, createdIssue.Status.Name, "Status was not set correctly");
            Assert.AreEqual(redmineClient.GetProjectByIdentifier(metaData.ProjectIdentifier, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds).Id, createdIssue.Project.Id, "Project was not set correctly");

            IList<Journal> journals = redmineClient.GetJournals(createdIssue.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Journal lastJournal = journals.OrderBy(j => j.CreatedOn).LastOrDefault();

            Assert.IsNotNull(lastJournal, "No journal entry created");
            Assert.AreEqual(metaData.Notes, lastJournal.Notes, "Journal notes not set correctly");

            redmineClient.DeleteIssue(createdIssue.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateIssueUsingObject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Issue issue = new Issue()
            {
                Subject = "Created via API",
                Description = "This issue was created via API",
                DueDate = DateTime.Today.AddDays(3),
                IsPrivate = true,
            };
            string assignedToLogin = TestEnvironment.UserLogin2;
            string priorityName = "High";
            string trackerName = "Feature";
            string stateName = "New";
            string projectIdentifier = TestEnvironment.ProjectIdentifier1;
            string notes = "This is a new issue with a note";
            object metaData = new Dictionary<string, object>()
            {
                {IssueMetaData.AssignedToLoginKey, assignedToLogin},
                {IssueMetaData.PriorityNameKey, priorityName},
                {IssueMetaData.TrackerNameKey, trackerName},
                {IssueMetaData.StateNameKey, stateName},
                {IssueMetaData.ProjectIdentifierKey, projectIdentifier},
                {IssueMetaData.NotesKey, notes},
                {IssueMetaData.PrivateNotesKey, true},
            };

            Issue createdIssue = redmineClient.CreateIssue(issue, metaData, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(createdIssue, "No issue received");
            Assert.IsTrue(createdIssue.Id > 0, "No Id defined in returned issue");
            Assert.AreEqual(issue.Subject, createdIssue.Subject, "Subject was not set correctly");
            Assert.AreEqual(issue.Description, createdIssue.Description, "Description was not set correctly");
            Assert.AreEqual(issue.DueDate, createdIssue.DueDate, "DueDate was not set correctly");
            Assert.AreEqual(issue.IsPrivate, createdIssue.IsPrivate, "IsPrivate was not set correctly");

            Assert.AreEqual(redmineClient.GetUserByLogin(assignedToLogin, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds).Id, createdIssue.AssignedTo.Id, "AssignedTo was not set correctly");
            Assert.AreEqual(priorityName, createdIssue.Priority.Name, "Priority was not set correctly");
            Assert.AreEqual(trackerName, createdIssue.Tracker.Name, "Tracker was not set correctly");
            Assert.AreEqual(stateName, createdIssue.Status.Name, "Status was not set correctly");
            Assert.AreEqual(redmineClient.GetProjectByIdentifier(projectIdentifier, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds).Id, createdIssue.Project.Id, "Project was not set correctly");

            IList<Journal> journals = redmineClient.GetJournals(createdIssue.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Journal lastJournal = journals.OrderBy(j => j.CreatedOn).LastOrDefault();

            Assert.IsNotNull(lastJournal, "No journal entry created");
            Assert.AreEqual(notes, lastJournal.Notes, "Journal notes not set correctly");

            redmineClient.DeleteIssue(createdIssue.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateIssue()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Issue issue = new Issue()
            {
                Subject = "Created via API",
                Description = "This issue was created via API",
                DueDate = DateTime.Today.AddDays(3),
            };
            IssueMetaData metaData = new IssueMetaData()
            {
                AssignedToLogin = TestEnvironment.UserLogin2,
                PriorityName = "High",
                TrackerName = "Feature",
                StateName = "New",
                ProjectIdentifier = TestEnvironment.ProjectIdentifier1,
            };
            Issue createdIssue = redmineClient.CreateIssue(issue, metaData, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            createdIssue.Description = "This issue was updated via API";
            createdIssue.Subject = "Update Issue";
            createdIssue.DueDate = DateTime.Today.AddDays(5);
            createdIssue.IsPrivate = true;
            IssueMetaData updateMetaData = new IssueMetaData()
            {
                AssignedToLogin = TestEnvironment.UserLogin1,
                PriorityName = "Urgent",
                TrackerName = "Bug",
                StateName = "In Progress",
                ProjectIdentifier = TestEnvironment.ProjectIdentifier2,
                Notes = "This issue had to be changed",
                PrivateNotes = true,
            };

            Issue updatedIssue = redmineClient.UpdateIssue(createdIssue, updateMetaData, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(updatedIssue, "No issue received");
            Assert.IsTrue(updatedIssue.Id > 0, "No Id defined in returned issue");
            Assert.AreEqual(createdIssue.Subject, updatedIssue.Subject, "Subject was not set correctly");
            Assert.AreEqual(createdIssue.Description, updatedIssue.Description, "Description was not set correctly");
            Assert.AreEqual(createdIssue.DueDate, updatedIssue.DueDate, "DueDate was not set correctly");
            Assert.AreEqual(createdIssue.IsPrivate, updatedIssue.IsPrivate, "IsPrivate was not set correctly");

            Assert.AreEqual(redmineClient.GetUserByLogin(updateMetaData.AssignedToLogin, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds).Id, updatedIssue.AssignedTo.Id, "AssignedTo was not set correctly");
            Assert.AreEqual(updateMetaData.PriorityName, updatedIssue.Priority.Name, "Priority was not set correctly");
            Assert.AreEqual(updateMetaData.TrackerName, updatedIssue.Tracker.Name, "Tracker was not set correctly");
            Assert.AreEqual(updateMetaData.StateName, updatedIssue.Status.Name, "Status was not set correctly");
            Assert.AreEqual(redmineClient.GetProjectByIdentifier(updateMetaData.ProjectIdentifier, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds).Id, createdIssue.Project.Id, "Project was not set correctly");

            IList<Journal> journals = redmineClient.GetJournals(createdIssue.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Journal lastJournal = journals.OrderBy(j => j.CreatedOn).LastOrDefault();

            Assert.IsNotNull(lastJournal, "No journal entry created");
            Assert.AreEqual(updateMetaData.Notes, lastJournal.Notes, "Journal notes not set correctly");

            redmineClient.DeleteIssue(createdIssue.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateIssueUsingObjects()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Issue issue = new Issue()
            {
                Subject = "Created via API",
                Description = "This issue was created via API",
                DueDate = DateTime.Today.AddDays(3),
            };
            IssueMetaData metaData = new IssueMetaData()
            {
                AssignedToLogin = TestEnvironment.UserLogin2,
                PriorityName = "High",
                TrackerName = "Feature",
                StateName = "New",
                ProjectIdentifier = TestEnvironment.ProjectIdentifier1,
            };
            Issue createdIssue = redmineClient.CreateIssue(issue, metaData, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            createdIssue.Description = "This issue was updated via API";
            createdIssue.Subject = "Update Issue";
            createdIssue.DueDate = DateTime.Today.AddDays(5);
            createdIssue.IsPrivate = true;

            string assignedToLogin = TestEnvironment.UserLogin1;
            string priorityName = "Urgent";
            string trackerName = "Bug";
            string stateName = "In Progress";
            string projectIdentifier = TestEnvironment.ProjectIdentifier2;
            string notes = "This issue had to be changed";
            object updateMetaData = new Dictionary<string, object>()
            {
                {IssueMetaData.AssignedToLoginKey, assignedToLogin},
                {IssueMetaData.PriorityNameKey, priorityName},
                {IssueMetaData.TrackerNameKey, trackerName},
                {IssueMetaData.StateNameKey, stateName},
                {IssueMetaData.ProjectIdentifierKey, projectIdentifier},
                {IssueMetaData.NotesKey, notes},
                {IssueMetaData.PrivateNotesKey, true},
            };

            Issue updatedIssue = redmineClient.UpdateIssue(createdIssue, updateMetaData, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(updatedIssue, "No issue received");
            Assert.IsTrue(updatedIssue.Id > 0, "No Id defined in returned issue");
            Assert.AreEqual(createdIssue.Subject, updatedIssue.Subject, "Subject was not set correctly");
            Assert.AreEqual(createdIssue.Description, updatedIssue.Description, "Description was not set correctly");
            Assert.AreEqual(createdIssue.DueDate, updatedIssue.DueDate, "DueDate was not set correctly");
            Assert.AreEqual(createdIssue.IsPrivate, updatedIssue.IsPrivate, "IsPrivate was not set correctly");

            Assert.AreEqual(redmineClient.GetUserByLogin(assignedToLogin, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds).Id, updatedIssue.AssignedTo.Id, "AssignedTo was not set correctly");
            Assert.AreEqual(priorityName, updatedIssue.Priority.Name, "Priority was not set correctly");
            Assert.AreEqual(trackerName, updatedIssue.Tracker.Name, "Tracker was not set correctly");
            Assert.AreEqual(stateName, updatedIssue.Status.Name, "Status was not set correctly");
            Assert.AreEqual(redmineClient.GetProjectByIdentifier(projectIdentifier, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds).Id, createdIssue.Project.Id, "Project was not set correctly");

            IList<Journal> journals = redmineClient.GetJournals(createdIssue.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Journal lastJournal = journals.OrderBy(j => j.CreatedOn).LastOrDefault();

            Assert.IsNotNull(lastJournal, "No journal entry created");
            Assert.AreEqual(notes, lastJournal.Notes, "Journal notes not set correctly");

            redmineClient.DeleteIssue(createdIssue.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void DeleteIssue()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Issue issue = new Issue()
            {
                Subject = "Created via API",
                Description = "This issue was created via API",
                DueDate = DateTime.Now.AddDays(3),
            };
            IssueMetaData metaData = new IssueMetaData()
            {
                AssignedToLogin = TestEnvironment.UserLogin2,
                PriorityName = "High",
                TrackerName = "Feature",
                StateName = "New",
                ProjectIdentifier = TestEnvironment.ProjectIdentifier1,
            };
            Issue createdIssue = redmineClient.CreateIssue(issue, metaData, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Issue loadedIssue = redmineClient.GetIssue(createdIssue.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsNotNull(loadedIssue, "Issue was not created correctly");

            bool success = redmineClient.DeleteIssue(createdIssue.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsTrue(success, "Did not receive success");

            try
            {
                Issue loadedIssueAfterDeletion = redmineClient.GetIssue(createdIssue.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        #endregion Issues

        #region Attachments

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetAttachmentList()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<Attachment> attachements = redmineClient.GetAttachments(TestEnvironment.IssueId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(attachements, "No attachements received");
            Assert.IsTrue(attachements.Count > 0, "Attachement list is empty");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetAttachmentListInvalidIssueId()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                IList<Attachment> attachements = redmineClient.GetAttachments(int.MaxValue, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetAttachment()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Attachment attachment = redmineClient.GetAttachment(TestEnvironment.AttachmentId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(attachment, "No attachment received");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetAttachmentInvalidId()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                Attachment attachment = redmineClient.GetAttachment(int.MaxValue, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateAttachment()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            AttachmentData attachmentData = new AttachmentData()
            {
                Content = File.ReadAllBytes(TestEnvironment.AttachmentFilePath),
                ContentType = "text/plain",
                FileName = "APIUpload.txt",
                Description = "Uploadet via API",
                Notes = "Note for the attachment",
                PrivateNotes = true,
            };

            Attachment createdAttachment = redmineClient.CreateAttachment(TestEnvironment.IssueId, attachmentData, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(createdAttachment, "No attachment received");
            Assert.AreEqual(attachmentData.Description, createdAttachment.Description, "Description was not set correctly");
            Assert.AreEqual(attachmentData.FileName, createdAttachment.FileName, "File name was not set correctly");
            Assert.AreEqual(attachmentData.ContentType, createdAttachment.ContentType, "Content type was not set correctly");

            IList<Journal> journals = redmineClient.GetJournals(TestEnvironment.IssueId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Journal lastJournal = journals.OrderBy(j => j.CreatedOn).LastOrDefault();

            Assert.IsNotNull(lastJournal, "No journal entry created");
            Assert.AreEqual(attachmentData.Notes, lastJournal.Notes, "Journal notes not set correctly");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateAttachmentUsingObjects()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            string contentType = "text/plain";
            string fileName = "APIUpload.txt";
            string description = "Uploadet via API";
            string notes = "Note for the attachment";
            object attachmentData = new Dictionary<string, object>()
            {
                {AttachmentData.ContentKey, File.ReadAllBytes(TestEnvironment.AttachmentFilePath)},
                {AttachmentData.ContentTypeKey, contentType},
                {AttachmentData.FileNameKey, fileName},
                {AttachmentData.DescriptionKey, description},
                {AttachmentData.NotesKey, notes},
                {AttachmentData.PrivateNotesKey, true},
            };

            Attachment createdAttachment = redmineClient.CreateAttachment(TestEnvironment.IssueId, attachmentData, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(createdAttachment, "No attachment received");
            Assert.AreEqual(description, createdAttachment.Description, "Description was not set correctly");
            Assert.AreEqual(fileName, createdAttachment.FileName, "File name was not set correctly");
            Assert.AreEqual(contentType, createdAttachment.ContentType, "Content type was not set correctly");

            IList<Journal> journals = redmineClient.GetJournals(TestEnvironment.IssueId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Journal lastJournal = journals.OrderBy(j => j.CreatedOn).LastOrDefault();

            Assert.IsNotNull(lastJournal, "No journal entry created");
            Assert.AreEqual(notes, lastJournal.Notes, "Journal notes not set correctly");
        }

        #endregion Attachments

        #region Journals

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetJournalList()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<Journal> journals = redmineClient.GetJournals(TestEnvironment.IssueId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(journals, "No journals received");
            Assert.IsTrue(journals.Count > 0, "Journal list is empty");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetJournalListInvalidIssueId()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                IList<Journal> journals = redmineClient.GetJournals(int.MaxValue, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetJournal()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Journal journal = redmineClient.GetJournal(TestEnvironment.IssueId, TestEnvironment.JournalId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(journal, "No journal received");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetJournalInvalidId()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                Journal journal = redmineClient.GetJournal(TestEnvironment.IssueId, int.MaxValue, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateJournal()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            JournalData journalData = new JournalData()
            {
                Notes = "The quick brown fox jumps over the lazy dog.",
                PrivateNotes = true,
            };
            Journal createdJournal = redmineClient.CreateJournal(TestEnvironment.IssueId, journalData, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(createdJournal, "No journal received");
            Assert.AreEqual(journalData.Notes, createdJournal.Notes, "Journal text not set correctly");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateJournalUsingObjects()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            string notes = "The quick brown fox jumps over the lazy dog.";

            object journalData = new Dictionary<string, object>
            {
                {JournalData.NotesKey, notes},
                {JournalData.PrivateNotesKey, true},
            };

            Journal createdJournal = redmineClient.CreateJournal(TestEnvironment.IssueId, journalData, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(createdJournal, "No journal received");
            Assert.AreEqual(notes, createdJournal.Notes, "Journal text not set correctly");
        }

        #endregion Journals

        #region Users

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetUserList()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<User> users = redmineClient.GetUsers(TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(users, "No users received");
            Assert.IsTrue(users.Count > 0, "List of users is empty");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetUser()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            User user = redmineClient.GetUser(TestEnvironment.UserId1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(user, "No user received");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetUserInvalidId()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                User user = redmineClient.GetUser(int.MaxValue, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetUserByLogin()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            User user = redmineClient.GetUserByLogin(TestEnvironment.UserLogin1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(user, "No user received");
            Assert.AreEqual(TestEnvironment.UserLogin1, user.Login, "Wrong user returned");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetUserInvalidLogin()
        {
            string login = "NotAUser";
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                User user = redmineClient.GetUserByLogin(login, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateUser()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            User user = new User()
            {
                Email = "some.mail@serv.ch",
                FirstName = "Luke",
                LastName = "Skywalker",
                Login = "lukesky",
                Password = "redmine$01"
            };
            User createdUser = redmineClient.CreateUser(user, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(createdUser, "No user received");
            Assert.IsTrue(createdUser.Id > 0, "No Id defined in returned user");
            Assert.AreEqual(user.Email, createdUser.Email, "Email was not set correctly");
            Assert.AreEqual(user.FirstName, createdUser.FirstName, "FirstName was not set correctly");
            Assert.AreEqual(user.LastName, createdUser.LastName, "LastName was not set correctly");
            Assert.AreEqual(user.Login, createdUser.Login, "Login was not set correctly");
            //password is not returned by the API
            //Assert.AreEqual(user.Password, createdUser.Password, "Password was not set correctly");

            redmineClient.DeleteUser(createdUser.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateUser()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            User user = new User()
            {
                Email = "some.mail@serv.ch",
                FirstName = "Luke",
                LastName = "Skywalker",
                Login = "lukesky",
                Password = "redmine$01",
                Status = UserStatus.STATUS_ACTIVE
            };
            User createdUser = redmineClient.CreateUser(user, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            createdUser.Email = "other.mail@bla.com";
            createdUser.FirstName = "Obiwan";
            createdUser.LastName = "Kenobi";
            createdUser.Login = "wanken";
            createdUser.Password = "redmine$02";

            User updatedUser = redmineClient.UpdateUser(createdUser, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(updatedUser, "No user received");
            Assert.IsTrue(updatedUser.Id > 0, "No Id defined in returned user");
            Assert.AreEqual(createdUser.Email, updatedUser.Email, "Email was not set correctly");
            Assert.AreEqual(createdUser.FirstName, updatedUser.FirstName, "FirstName was not set correctly");
            Assert.AreEqual(createdUser.LastName, updatedUser.LastName, "LastName was not set correctly");
            Assert.AreEqual(createdUser.Login, updatedUser.Login, "Login was not set correctly");
            //password is not returned by the API
            //Assert.AreEqual(createdUser.Password, updatedUser.Password, "Password was not set correctly");

            redmineClient.DeleteUser(createdUser.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void DeleteUser()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            User user = new User()
            {
                Email = "some.mail@serv.ch",
                FirstName = "Luke",
                LastName = "Skywalker",
                Login = "lukesky",
                Password = "redmine$01",
                Status = UserStatus.STATUS_ACTIVE
            };
            User createdUser = redmineClient.CreateUser(user, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            User loadedUser = redmineClient.GetUser(createdUser.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsNotNull(loadedUser, "User was not created correctly");

            bool success = redmineClient.DeleteUser(createdUser.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsTrue(success, "Did not receive success");

            try
            {
                User loadedUserAfterDeletion = redmineClient.GetUser(createdUser.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        #endregion Users

        #region Memberships

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetUsersOfProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<ProjectUser> projectUsers = redmineClient.GetUsersInProject(TestEnvironment.ProjectId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(projectUsers, "No project users received");
            Assert.IsTrue(projectUsers.Count > 0, "Project user list is empty");
            foreach (ProjectUser projectUser in projectUsers)
            {
                Assert.IsNotNull(projectUser.Roles, "No roles received for user");
                Assert.IsTrue(projectUser.Roles.Count > 0, "Role list empty for user");
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetUsersOfProjectUsingKeys()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<ProjectUser> projectUsers = redmineClient.GetUsersInProject(TestEnvironment.ProjectIdentifier1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(projectUsers, "No project users received");
            Assert.IsTrue(projectUsers.Count > 0, "Project user list is empty");
            foreach (ProjectUser projectUser in projectUsers)
            {
                Assert.IsNotNull(projectUser.Roles, "No roles received for user");
                Assert.IsTrue(projectUser.Roles.Count > 0, "Role list empty for user");
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void AddUserToProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<ProjectUser> projectUsers = redmineClient.GetUsersInProject(TestEnvironment.ProjectId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsFalse(projectUsers.Any(pu => pu.UserLogin == TestEnvironment.UserLogin2), "User already in project befor update");

            ProjectUser addedProjectUser = redmineClient.AddUserToProject(TestEnvironment.ProjectId, TestEnvironment.UserId2,
                new List<string> { "Reporter" }, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(addedProjectUser.Roles, "No roles received for user");
            Assert.IsTrue(addedProjectUser.Roles.Count > 0, "Role list empty for user");
            Assert.IsTrue(addedProjectUser.Roles.Contains("Reporter"), "User has no the defined role in the project");

            IList<ProjectUser> projectUsersAfterUpdate = redmineClient.GetUsersInProject(TestEnvironment.ProjectId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsTrue(projectUsersAfterUpdate.Any(pu => pu.UserLogin == TestEnvironment.UserLogin2), "User was not added to project");

            redmineClient.RemoveUserFromProject(TestEnvironment.ProjectId, TestEnvironment.UserId2,
                TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void AddUserToProjectUsingObject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<ProjectUser> projectUsers = redmineClient.GetUsersInProject(TestEnvironment.ProjectId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsFalse(projectUsers.Any(pu => pu.UserLogin == TestEnvironment.UserLogin2), "User already in project befor update");

            object userRoles = new List<string> { "Reporter" };
            ProjectUser addedProjectUser = redmineClient.AddUserToProject(TestEnvironment.ProjectId, TestEnvironment.UserId2,
                userRoles, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(addedProjectUser.Roles, "No roles received for user");
            Assert.IsTrue(addedProjectUser.Roles.Count > 0, "Role list empty for user");
            Assert.IsTrue(addedProjectUser.Roles.Contains("Reporter"), "User has no the defined role in the project");

            IList<ProjectUser> projectUsersAfterUpdate = redmineClient.GetUsersInProject(TestEnvironment.ProjectId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsTrue(projectUsersAfterUpdate.Any(pu => pu.UserLogin == TestEnvironment.UserLogin2), "User was not added to project");

            redmineClient.RemoveUserFromProject(TestEnvironment.ProjectId, TestEnvironment.UserId2,
                TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void AddUserToProjectUsingKeys()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<ProjectUser> projectUsers = redmineClient.GetUsersInProject(TestEnvironment.ProjectIdentifier1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsFalse(projectUsers.Any(pu => pu.UserLogin == TestEnvironment.UserLogin2), "User already in project befor update");

            ProjectUser addedProjectUser = redmineClient.AddUserToProject(TestEnvironment.ProjectIdentifier1, TestEnvironment.UserLogin2,
                new List<string> { "Reporter" }, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(addedProjectUser.Roles, "No roles received for user");
            Assert.IsTrue(addedProjectUser.Roles.Count > 0, "Role list empty for user");
            Assert.IsTrue(addedProjectUser.Roles.Contains("Reporter"), "User has no the defined role in the project");

            IList<ProjectUser> projectUsersAfterUpdate = redmineClient.GetUsersInProject(TestEnvironment.ProjectIdentifier1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsTrue(projectUsersAfterUpdate.Any(pu => pu.UserLogin == TestEnvironment.UserLogin2), "User was not added to project");

            redmineClient.RemoveUserFromProject(TestEnvironment.ProjectIdentifier1, TestEnvironment.UserLogin2,
                TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetRolesOfUserInProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<string> userRoles = redmineClient.GetUserRoles(TestEnvironment.ProjectId, TestEnvironment.UserId1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(userRoles, "No roles received for user");
            Assert.IsTrue(userRoles.Count > 0, "Role list empty for user");
            Assert.IsTrue(userRoles.Contains("Developer"), "User has no the defined role in the project");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetRolesOfUserInProjectUsingKeys()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<string> userRoles = redmineClient.GetUserRoles(TestEnvironment.ProjectIdentifier1, TestEnvironment.UserLogin1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(userRoles, "No roles received for user");
            Assert.IsTrue(userRoles.Count > 0, "Role list empty for user");
            Assert.IsTrue(userRoles.Contains("Developer"), "User has no the defined role in the project");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateRolesOfUserInProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<string> userRoles = redmineClient.GetUserRoles(TestEnvironment.ProjectId, TestEnvironment.UserId1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(userRoles, "No roles received for user");
            Assert.IsTrue(userRoles.Count > 0, "Role list empty for user");
            Assert.IsFalse(userRoles.Contains("Reporter"), "User has role befoer update");

            userRoles.Add("Reporter");
            ProjectUser updatedUser = redmineClient.UpdateUserRoles(TestEnvironment.ProjectId, TestEnvironment.UserId1, userRoles, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(updatedUser, "No project user received");
            Assert.IsNotNull(updatedUser.Roles, "No roles received for user");
            Assert.IsTrue(updatedUser.Roles.Count > 0, "Role list empty for user");
            Assert.IsTrue(updatedUser.Roles.Contains("Reporter"), "Role was not added in returned object");

            IList<string> updatedUserRoles = redmineClient.GetUserRoles(TestEnvironment.ProjectId, TestEnvironment.UserId1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(updatedUserRoles, "No roles received for user");
            Assert.IsTrue(updatedUserRoles.Count > 0, "Role list empty for user");
            Assert.IsTrue(updatedUserRoles.Contains("Reporter"), "Role was not added correctly");

            userRoles.Remove("Reporter");
            redmineClient.UpdateUserRoles(TestEnvironment.ProjectId, TestEnvironment.UserId1, userRoles, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateRolesOfUserInProjectUseObject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<string> userRoles = redmineClient.GetUserRoles(TestEnvironment.ProjectId, TestEnvironment.UserId1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(userRoles, "No roles received for user");
            Assert.IsTrue(userRoles.Count > 0, "Role list empty for user");
            Assert.IsFalse(userRoles.Contains("Reporter"), "User has role befoer update");

            userRoles.Add("Reporter");
            object userRolseObj = userRoles;
            ProjectUser updatedUser = redmineClient.UpdateUserRoles(TestEnvironment.ProjectId, TestEnvironment.UserId1, userRolseObj, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(updatedUser, "No project user received");
            Assert.IsNotNull(updatedUser.Roles, "No roles received for user");
            Assert.IsTrue(updatedUser.Roles.Count > 0, "Role list empty for user");
            Assert.IsTrue(updatedUser.Roles.Contains("Reporter"), "Role was not added in returned object");

            IList<string> updatedUserRoles = redmineClient.GetUserRoles(TestEnvironment.ProjectId, TestEnvironment.UserId1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(updatedUserRoles, "No roles received for user");
            Assert.IsTrue(updatedUserRoles.Count > 0, "Role list empty for user");
            Assert.IsTrue(updatedUserRoles.Contains("Reporter"), "Role was not added correctly");

            userRoles.Remove("Reporter");
            redmineClient.UpdateUserRoles(TestEnvironment.ProjectId, TestEnvironment.UserId1, userRoles, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateRolesOfUserInProjectUsingKeys()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<string> userRoles = redmineClient.GetUserRoles(TestEnvironment.ProjectIdentifier1, TestEnvironment.UserLogin1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(userRoles, "No roles received for user");
            Assert.IsTrue(userRoles.Count > 0, "Role list empty for user");
            Assert.IsFalse(userRoles.Contains("Reporter"), "User has role befoer update");

            userRoles.Add("Reporter");
            ProjectUser updatedUser = redmineClient.UpdateUserRoles(TestEnvironment.ProjectIdentifier1, TestEnvironment.UserLogin1, userRoles, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(updatedUser, "No project user received");
            Assert.IsNotNull(updatedUser.Roles, "No roles received for user");
            Assert.IsTrue(updatedUser.Roles.Count > 0, "Role list empty for user");
            Assert.IsTrue(updatedUser.Roles.Contains("Reporter"), "Role was not added in returned object");

            IList<string> updatedUserRoles = redmineClient.GetUserRoles(TestEnvironment.ProjectIdentifier1, TestEnvironment.UserLogin1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(updatedUserRoles, "No roles received for user");
            Assert.IsTrue(updatedUserRoles.Count > 0, "Role list empty for user");
            Assert.IsTrue(updatedUserRoles.Contains("Reporter"), "Role was not added correctly");

            userRoles.Remove("Reporter");
            redmineClient.UpdateUserRoles(TestEnvironment.ProjectIdentifier1, TestEnvironment.UserLogin1, userRoles, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void RemoveUserFromProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<ProjectUser> projectUsers = redmineClient.GetUsersInProject(TestEnvironment.ProjectId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            ProjectUser addedProjectUser = redmineClient.AddUserToProject(TestEnvironment.ProjectId, TestEnvironment.UserId2,
                new List<string> { "Reporter" }, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<ProjectUser> projectUsersAfterAdd = redmineClient.GetUsersInProject(TestEnvironment.ProjectId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsTrue(projectUsersAfterAdd.Any(pu => pu.UserLogin == TestEnvironment.UserLogin2), "User not in project befor remove");

            bool success = redmineClient.RemoveUserFromProject(TestEnvironment.ProjectId, TestEnvironment.UserId2,
                TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsTrue(success, "Did not receive success");
            IList<ProjectUser> projectUsersAfterRemove = redmineClient.GetUsersInProject(TestEnvironment.ProjectId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsFalse(projectUsersAfterRemove.Any(pu => pu.UserLogin == TestEnvironment.UserLogin2), "User in project after remove");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void RemoveUserFromProjectUsingKeys()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<ProjectUser> projectUsers = redmineClient.GetUsersInProject(TestEnvironment.ProjectIdentifier1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            ProjectUser addedProjectUser = redmineClient.AddUserToProject(TestEnvironment.ProjectIdentifier1, TestEnvironment.UserLogin2,
                new List<string> { "Reporter" }, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<ProjectUser> projectUsersAfterAdd = redmineClient.GetUsersInProject(TestEnvironment.ProjectIdentifier1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsTrue(projectUsersAfterAdd.Any(pu => pu.UserLogin == TestEnvironment.UserLogin2), "User not in project befor remove");

            bool success = redmineClient.RemoveUserFromProject(TestEnvironment.ProjectIdentifier1, TestEnvironment.UserLogin2,
                TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsTrue(success, "Did not receive success");
            IList<ProjectUser> projectUsersAfterRemove = redmineClient.GetUsersInProject(TestEnvironment.ProjectIdentifier1, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
            Assert.IsFalse(projectUsersAfterRemove.Any(pu => pu.UserLogin == TestEnvironment.UserLogin2), "User in project after remove");
        }

        #endregion Memberships

        #region Load Items Source Objects

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueStateList()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<IssueStatus> states = redmineClient.GetIssueStates(TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(states, "No states received");
            Assert.IsTrue(states.Count > 0, "List of states is empty");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueStateByName()
        {
            string stateName = "In Progress";
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IssueStatus state = redmineClient.GetIssueStateByName(stateName, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(state, "No state received");
            Assert.AreEqual(stateName, state.Name, "Wrong state returned");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueStateInvalidName()
        {
            string stateName = "NotAState";
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                IssueStatus state = redmineClient.GetIssueStateByName(stateName, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssuePriorityList()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<IssuePriority> priorities = redmineClient.GetIssuePriorities(TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(priorities, "No priorities received");
            Assert.IsTrue(priorities.Count > 0, "List of priorities is empty");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssuePriorityByName()
        {
            string priorityName = "Urgent";
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IssuePriority priority = redmineClient.GetIssuePriorityByName(priorityName, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(priority, "No priority received");
            Assert.AreEqual(priorityName, priority.Name, "Wrong priority returned");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssuePriorityInvalidName()
        {
            string priorityName = "NotAPriority";
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                IssuePriority priority = redmineClient.GetIssuePriorityByName(priorityName, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetTrackerList()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<Tracker> trackers = redmineClient.GetTrackers(TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(trackers, "No trackers received");
            Assert.IsTrue(trackers.Count > 0, "List of trackers is empty");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetTrackerByName()
        {
            string trackerName = "Bug";
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Tracker tracker = redmineClient.GetTrackerByName(trackerName, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(tracker, "No tracker received");
            Assert.AreEqual(trackerName, tracker.Name, "Wrong tracker returned");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetTrackerInvalidName()
        {
            string trackerName = "NotATracker";
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                Tracker tracker = redmineClient.GetTrackerByName(trackerName, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetRoleList()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<Role> roles = redmineClient.GetRoles(TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(roles, "No roles received");
            Assert.IsTrue(roles.Count > 0, "List of roles is empty");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetRoleByName()
        {
            string roleName = "Developer";
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Role role = redmineClient.GetRoleByName(roleName, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(role, "No role received");
            Assert.AreEqual(roleName, role.Name, "Wrong role returned");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetRoleInvalidName()
        {
            string roleName = "NotARole";
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                Role role = redmineClient.GetRoleByName(roleName, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Not Found"));
            }
        }

        #endregion Load Items Source Objects
    }
}
