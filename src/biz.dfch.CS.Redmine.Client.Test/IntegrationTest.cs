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
using biz.dfch.CS.Redmine.Client.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Redmine.Net.Api.Types;
using System.IO;
﻿using System.Linq;

namespace biz.dfch.CS.Redmine.Client.Test
{
    [TestClass]
    public class IntegrationTest
    {
        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateProjects()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedmineUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            for (int i = 0; i < TestEnvironment.NumberOfProjectsToCreate; i++)
            {
                Project project = new Project()
                {
                    Name = TextCreator.GetProjectName(),
                    Description = TextCreator.GetProjectDesctiption(),
                    Identifier = Guid.NewGuid().ToString(),
                    IsPublic = false,

                };
                Project createdProject = redmineClient.CreateProject(project, null, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

                Assert.IsNotNull(createdProject, "No project received");
                Assert.IsTrue(createdProject.Id > 0, "No Id defined in returned project");
                Assert.AreEqual(project.Description, createdProject.Description, "Description was not set correctly");
                Assert.AreEqual(project.Identifier, createdProject.Identifier, "Identifier was not set correctly");
                Assert.AreEqual(project.IsPublic, createdProject.IsPublic, "IsPublic was not set correctly");
                Assert.AreEqual(project.Name, createdProject.Name, "Name was not set correctly");
                Assert.IsNull(project.Parent, "Project was created as child project");
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateUserForProjects()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedmineUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<Project> projects = redmineClient.GetProjects(TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            foreach (Project project in projects)
            {
                if ((project.Identifier != TestEnvironment.ProjectIdentifier1) && (project.Identifier != TestEnvironment.ProjectIdentifier2))
                {
                    User user = new User()
                    {
                        Email = string.Format("userP{0}@serv.ch", project.Id),
                        FirstName = "User",
                        LastName = project.Name,
                        Login = string.Format("userP{0}", project.Id),
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

                    redmineClient.AddUserToProject(project.Identifier, createdUser.Login, new List<string> { "Developer", "Reporter" });
                }
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateIssuesForProjects()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedmineUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<Project> projects = redmineClient.GetProjects(TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            string[] priorities = { "Low", "Normal", "High", "Urgent", "Immediate" };
            string[] trackers = { "Feature", "Bug" };

            foreach (Project project in projects)
            {
                if ((project.Identifier != TestEnvironment.ProjectIdentifier1) && (project.Identifier != TestEnvironment.ProjectIdentifier2))
                {
                    for (int i = 0; i < TestEnvironment.NumberOfIssuesToCreate; i++)
                    {
                        Issue issue = new Issue()
                        {
                            Subject = TextCreator.GetIssueName(),
                            Description = TextCreator.GetIssueDescription(),
                            DueDate = DateTime.Today.AddDays(3),
                            IsPrivate = true,
                        };
                        IssueMetaData metaData = new IssueMetaData()
                        {
                            AssignedToLogin = string.Format("userP{0}", project.Id),
                            PriorityName = priorities[i % priorities.Length],
                            TrackerName = trackers[i % trackers.Length],
                            StateName = "New",
                            ProjectIdentifier = project.Identifier,
                            Notes = string.Format("This is very important as we want to {0}", issue.Description),
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
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void UpdateAttachments()
        {
            var redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedmineUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            var attachments = redmineClient.GetAttachments(TestEnvironment.IssueId);
            var attachment = attachments.FirstOrDefault(p => p.Id == TestEnvironment.AttachmentId);

            Assert.IsNotNull(attachment, "No attachment found");

            var name = string.Format("UPDATED_FILE_NAME_{0}.txt", attachment.Id);
            var description = string.Format("UPDATED_FILE_DESCRIPTION_{0}", attachment.Id);

            attachment.FileName = name;
            attachment.Description = description;

            redmineClient.UpdateAttachment(TestEnvironment.IssueId, attachment);

            var updateAttachment = redmineClient.GetAttachment(TestEnvironment.AttachmentId);

            Assert.IsNotNull(updateAttachment, "updated attachment not found");
            Assert.IsTrue(updateAttachment.FileName == name, "Did not receive success");
            Assert.IsTrue(true, "Did not receive success");
            Assert.IsTrue(true, "Did not receive success");
            Assert.IsTrue(true, "Did not receive success");
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void CreateAttachmentForIssue()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedmineUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Issue issue = redmineClient.GetIssue(TestEnvironment.IssueId, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            Assert.IsNotNull(issue, "No issue received");

            var contentType = "text/plain";
            var fileName = "TestAttachment.txt";
            var description = "Uploadet via API";
            var notes = "Note for the attachment";

            var attachmentData = new AttachmentData
            {
                Content = File.ReadAllBytes(TestEnvironment.AttachmentFilePath),
                ContentType = contentType,
                FileName = fileName,
                Description = description,
                Notes = notes,
                PrivateNotes = true
            };

            redmineClient.CreateAttachment(issue.Id, attachmentData);

            IList<Attachment> attachments = redmineClient.GetAttachments(TestEnvironment.IssueId);

            Assert.IsNotNull(attachments, "No attachments received");
            Assert.AreEqual(1, attachments.Count, 0, "No attachment or wrong count");
            Assert.AreEqual(attachments[0].ContentType, contentType);
            Assert.AreEqual(attachments[0].Description, description);
            Assert.AreEqual(attachments[0].FileName, fileName);
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void DeleteUsers()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedmineUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<User> users = redmineClient.GetUsers(TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            foreach (User user in users)
            {
                if ((user.Login != TestEnvironment.UserLogin1) && (user.Login != TestEnvironment.UserLogin2))
                {
                    bool success = redmineClient.DeleteUser(user.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                    Assert.IsTrue(success, "Did not receive success");
                }
            }
        }

        [TestMethod]
        [TestCategory("SkipOnTeamCity")]
        public void DeleteProjects()
        {
            RedmineClient redmineClient = new RedmineClient();
            redmineClient.Login(TestEnvironment.RedmineUrl, TestEnvironment.RedmineLogin, TestEnvironment.RedminePassword, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            IList<Project> projects = redmineClient.GetProjects(TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);

            foreach (Project project in projects)
            {
                if ((project.Identifier != TestEnvironment.ProjectIdentifier1) && (project.Identifier != TestEnvironment.ProjectIdentifier2))
                {
                    bool success = redmineClient.DeleteProject(project.Id, TestEnvironment.TotalAttempts, TestEnvironment.BaseRetryIntervallMilliseconds);
                    Assert.IsTrue(success, "Did not receive success");
                }
            }
        }
    }
}
