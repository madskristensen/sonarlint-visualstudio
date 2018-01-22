﻿/*
 * SonarLint for Visual Studio
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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using SonarQube.Client;
using SonarQube.Client.Api.Requests;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.MefServices
{
    /// <summary>
    ///     This class only purposes is to avoid bringing MEF composition to the SonarQube.Client assembly which
    ///     can be used in contexts where it is not required.
    /// </summary>
    [Export(typeof(ISonarQubeService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [ExcludeFromCodeCoverage] // Simply provides MEF export
    public class MefSonarQubeService : SonarQubeService
    {
        public MefSonarQubeService()
            : base(new HttpClientHandler(), DefaultConfiguration.Configure(new RequestFactory()))
        {
        }
    }
}
