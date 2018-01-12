﻿/*
 * SonarQube Client
 * Copyright (C) 2016-2018 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarQube.Client.RequestGenerator
{
    public class ServiceRunner
    {
        private readonly ISonarQubeService service;

        public string OutputPath { get; set; }

        public Uri SonarQubeUrl { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Project { get; set; }

        public string Organization { get; set; }

        public string RoslynQualityProfile { get; set; }

        public ServiceRunner(ISonarQubeService service)
        {
            this.service = service;
        }

        public async Task Run(string[] args, CancellationToken token)
        {
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }

            await SerializeResult(nameof(service.ConnectAsync) + "-wrong-login", $"user: {Username}, pass: <new guid>",
                service.ConnectAsync(new ConnectionInformation(SonarQubeUrl, Username, ToSecureString(Guid.NewGuid().ToString())), token));

            await SerializeResult(nameof(service.ConnectAsync), $"user: {Username}, pass: {Password}",
                service.ConnectAsync(new ConnectionInformation(SonarQubeUrl, Username, ToSecureString(Password)), token));

            await SerializeResult(nameof(service.GetSuppressedIssuesAsync), $"project: {Project}",
                service.GetSuppressedIssuesAsync(Project, token));

            // some old date, ensure to return notifications
            var since = new DateTimeOffset(2016, 10, 19, 13, 0, 0, TimeSpan.Zero);
            await SerializeResult(nameof(service.GetNotificationEventsAsync), $"project: {Project}, since: {since}",
                service.GetNotificationEventsAsync(Project, since, token));

            await SerializeResult(nameof(service.GetAllOrganizationsAsync), $"",
                service.GetAllOrganizationsAsync(token));

            await SerializeResult(nameof(service.GetAllPluginsAsync), $"",
                service.GetAllPluginsAsync(token));

            await SerializeResult(nameof(service.GetAllProjectsAsync), $"organization: {Organization}",
                service.GetAllProjectsAsync(Organization, token));

            await SerializeResult(nameof(service.GetAllPropertiesAsync), $"",
                service.GetAllPropertiesAsync(token));

            await SerializeResult(nameof(service.GetQualityProfileAsync), $"project: {Project}, organization: {Organization}",
                service.GetQualityProfileAsync(Project, Organization, SonarQubeLanguage.CSharp, token));

            // this should get the default profile for the organization
            await SerializeResult(nameof(service.GetQualityProfileAsync) + "-default", $"project: {Project}, organization: {Organization}",
                service.GetQualityProfileAsync(Guid.NewGuid().ToString(), Organization, SonarQubeLanguage.CSharp, token));

            await SerializeResult(nameof(service.GetRoslynExportProfileAsync), $"quality profile: {RoslynQualityProfile}",
                service.GetRoslynExportProfileAsync(RoslynQualityProfile, Organization, SonarQubeLanguage.CSharp, token));
        }

        private static SecureString ToSecureString(string str)
        {
            return str.Aggregate(new SecureString(), (s, c) => { s.AppendChar(c); return s; });
        }

        private static async Task<string> GetResult(Task task)
        {
            await task.ConfigureAwait(false);
            return "OK";
        }

        private async Task SerializeResult(string fileName, string message, Task task)
        {
            await SerializeResult(fileName, message, GetResult(task));
        }

        private async Task SerializeResult<T>(string fileName, string message, Task<T> task)
        {
            Console.WriteLine($"Executing {fileName} with {message}");

            string serialized;
            try
            {
                var result = await task;

                serialized = JsonConvert.SerializeObject(result);
            }
            catch (Exception e)
            {
                serialized = e.Message;
            }

            using (var writer = File.CreateText(Path.Combine(OutputPath, fileName)))
            {
                writer.WriteLine(serialized);
            }
        }
    }
}
