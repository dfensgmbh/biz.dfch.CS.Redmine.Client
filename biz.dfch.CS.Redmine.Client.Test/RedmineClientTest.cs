using System;
using System.Collections.Generic;
using biz.dfch.CS.Redmine.Client.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Redmine.Net.Api.Types;

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
            bool success = redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsTrue(success, "Could not log in.");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void LoginWrongServerUrl()
        {
            RedmineClient redmineClient = new RedmineClient();

            try
            {
                bool success = redmineClient.Login("http://notAServer:8080/redmine", TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                Assert.IsTrue(false, "Should throw an exception and never reach this line");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Equals("User could not be authorized"));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void LoginInvalidApiKey()
        {
            RedmineClient redmineClient = new RedmineClient();

            try
            {
                bool success = redmineClient.Login(TestEnvironment.RedminUrl, "NotAnApiKey", 3, 100);
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
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<Project> projects = redmineClient.GetProjects(TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(projects, "No projects received");
            Assert.IsTrue(projects.Count > 0, "Project list is empty");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Project project = redmineClient.GetProject(TestEnvironment.ProjectId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(project, "No project received");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetProjectInvalidId()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

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
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetProjectInvalidIdentifier()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Project project = new Project()
            {
                Description = "This project was created via API",
                Identifier = Guid.NewGuid().ToString(),
                IsPublic = false,
                Name = "Created via API",
            };
            Project createdProject = redmineClient.CreateProject(project, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(createdProject, "No project received");
            Assert.IsTrue(createdProject.Id > 0, "No Id defined in returned project");
            Assert.AreEqual(project.Description, createdProject.Description, "Description was not set correctly");
            Assert.AreEqual(project.Identifier, createdProject.Identifier, "Identifier was not set correctly");
            Assert.AreEqual(project.IsPublic, createdProject.IsPublic, "IsPublic was not set correctly");
            Assert.AreEqual(project.Name, createdProject.Name, "Name was not set correctly");

            redmineClient.DeleteProject(createdProject.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Project project = new Project()
            {
                Description = "This project must be updated",
                Identifier = Guid.NewGuid().ToString(),
                IsPublic = false,
                Name = "To Update",
            };
            Project createdProject = redmineClient.CreateProject(project, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            createdProject.Description = "This project was updated via API";
            createdProject.Name = "Update Project";
            createdProject.IsPublic = true;

            Project updatedProject = redmineClient.UpdateProject(createdProject, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(updatedProject, "No project received");
            Assert.AreEqual(createdProject.Description, updatedProject.Description, "CreatedOn was not set correctly");
            Assert.AreEqual(createdProject.IsPublic, updatedProject.IsPublic, "CreatedOn was not set correctly");
            Assert.AreEqual(createdProject.Name, updatedProject.Name, "CreatedOn was not set correctly");

            redmineClient.DeleteProject(createdProject.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void DeleteProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Project project = new Project()
            {
                Description = "This project was created via API",
                Identifier = Guid.NewGuid().ToString(),
                IsPublic = false,
                Name = "Created via API",
            };
            Project createdProject = redmineClient.CreateProject(project, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

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
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<Issue> issues = redmineClient.GetIssues(null, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(issues, "No issues received");
            Assert.IsTrue(issues.Count > 0, "Issue list is empty");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueListOfProject()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<Issue> issues = redmineClient.GetIssues(TestEnvironment.ProjectId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(issues, "No issues received");
            Assert.IsTrue(issues.Count > 0, "Issue list is empty");
            foreach (Issue issue in issues)
            {
                Assert.AreEqual(TestEnvironment.ProjectId, issue.Project.Id, "Issue from wrong project loaded");
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueListInvalidProjectId()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            try
            {
                IList<Issue> issues = redmineClient.GetIssues(int.MaxValue, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
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
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Issue issue = redmineClient.GetIssue(TestEnvironment.IssueId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(issue, "No issues received");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueInvalidId()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

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
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Issue issue = new Issue()
            {
                Subject = "Created via API",
                Description = "This issue was created via API",
                DueDate = DateTime.Now.AddDays(3),
            };
            IssueMetaData metaData = new IssueMetaData()
            {
                AssignedToLogin = TestEnvironment.UserLogin2,
                AuthorLogin = TestEnvironment.UserLogin1,
                PriorityName = "High",
                TrackerName = "Feature",
                StateName = "New",
                ProjectIdentifier = TestEnvironment.ProjectIdentifier,
            };
            Issue createdIssue = redmineClient.CreateIssue(issue, metaData, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(createdIssue, "No issue received");
            Assert.IsTrue(createdIssue.Id > 0, "No Id defined in returned issue");
            Assert.AreEqual(issue.Subject, createdIssue.Subject, "Subject was not set correctly");
            Assert.AreEqual(issue.Description, createdIssue.Description, "Description was not set correctly");

            //Assert.AreEqual(metaData.AuthorLogin, createdIssue.Author.Name, "Author was not set correctly"); //Compare user name -> Implement GetUserByLogin
            //Assert.AreEqual(metaData.AssignedToLogin, createdIssue.AssignedTo.Name, "AssignedTo was not set correctly"); //Compare user name -> Implement GetUserByLogin
            Assert.AreEqual(metaData.PriorityName, createdIssue.Priority.Name, "Priority was not set correctly");
            Assert.AreEqual(metaData.TrackerName, createdIssue.Tracker.Name, "Tracker was not set correctly");
            Assert.AreEqual(metaData.StateName, createdIssue.Status.Name, "Status was not set correctly");
            //Assert.AreEqual(metaData.ProjectIdentifier, createdIssue.Project.Name, "Project was not set correctly"); //Compare project name -> Implement GetProjectByIdentifier

            redmineClient.DeleteIssue(createdIssue.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds); 
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateIssue()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void DeleteIssue()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedminUrl, TestEnvironment.ApiKey, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Issue issue = new Issue()
            {
                Subject = "Created via API",
                Description = "This issue was created via API",
                DueDate = DateTime.Now.AddDays(3),
            };
            IssueMetaData metaData = new IssueMetaData()
            {
                AssignedToLogin = TestEnvironment.UserLogin2,
                AuthorLogin = TestEnvironment.UserLogin1,
                PriorityName = "High",
                TrackerName = "Feature",
                StateName = "New",
                ProjectIdentifier = TestEnvironment.ProjectIdentifier,
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
        public void GetAttachementList()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetAttachementListInvalidIssueId()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetAttachement()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetAttachementInvalidId()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateAttachement()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateAttachement()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void DeleteAttachement()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        #endregion Attachments

        #region Load Items Source Objects

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetStateValueList()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetStateValueByName()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetPriorityValueList()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetPriorityValueName()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetUserList()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetUserByLogin()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        #endregion Load Items Source Objects
    }
}
