/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.Suppression
{
    public sealed class SonarQubeIssuesProvider : ISonarQubeIssuesProvider, IDisposable
    {
        private const double MillisecondsToWaitBetweenRefresh = 1000 * 60 * 10; // 10 minutes

        private readonly ITimer refreshTimer;
        private readonly ISonarQubeService sonarQubeService;
        private readonly string boundProjectKey;

        private IList<SonarQubeIssue> cachedSuppressedIssues;
        private bool isDisposed;
        private CancellationTokenSource cancellationTokenSource;

        public SonarQubeIssuesProvider(ISonarQubeService sonarQubeService, ITimerFactory timerFactory, string boundProjectKey)
        {
            if (sonarQubeService == null)
            {
                throw new ArgumentNullException(nameof(sonarQubeService));
            }
            if (timerFactory == null)
            {
                throw new ArgumentNullException(nameof(timerFactory));
            }

            this.sonarQubeService = sonarQubeService;
            this.boundProjectKey = boundProjectKey;

            this.refreshTimer = timerFactory.Create();
            this.refreshTimer.AutoReset = true;
            this.refreshTimer.Interval = MillisecondsToWaitBetweenRefresh;
            this.refreshTimer.Elapsed += OnRefreshTimerElapsed;

            SynchronizeSuppressedIssues();

            this.refreshTimer.Start();
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            refreshTimer.Elapsed -= OnRefreshTimerElapsed;
            refreshTimer.Dispose();
            this.isDisposed = true;
        }

        public IEnumerable<SonarQubeIssue> GetSuppressedIssues(string projectGuid, string filePath)
        {
            // TODO: Block the call while the cache is being built + handle multi-threading

            // TODO: ensure we've got data to enable end to end testing
            if (this.cachedSuppressedIssues == null)
            {
                SynchronizeSuppressedIssues().Wait(30000);
            }

            if (this.cachedSuppressedIssues == null)
            {
                return Enumerable.Empty<SonarQubeIssue>();
            }

            var moduleKey = $"{this.boundProjectKey}:{this.boundProjectKey}:{projectGuid}";

            return this.cachedSuppressedIssues.Where(x =>
                x.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase) &&
                x.ModuleKey.Equals(moduleKey, StringComparison.OrdinalIgnoreCase));
        }

        private async void OnRefreshTimerElapsed(object sender, TimerEventArgs e)
        {
            await SynchronizeSuppressedIssues();
        }

        private async Task SynchronizeSuppressedIssues()
        {
            if (!this.sonarQubeService.IsConnected)
            {
                return;
            }

            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            // TODO: Handle race conditions
            this.cachedSuppressedIssues = await this.sonarQubeService.GetSuppressedIssuesAsync(this.boundProjectKey,
                cancellationTokenSource.Token);
        }
    }
}
