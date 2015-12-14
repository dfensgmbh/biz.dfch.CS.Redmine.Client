using System;
using System.Collections.Generic;
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
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateProject()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void DeleteProject()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        #endregion Projects

        #region Issues

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueList()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueListOfProject()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueListInvalidProjectId()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssue()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void GetIssueInvalidId()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateIssue()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateIssue()
        {
            Assert.IsTrue(false, "Not yet implemented");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void DelteIssue()
        {
            Assert.IsTrue(false, "Not yet implemented");
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
        public void DelteAttachement()
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
