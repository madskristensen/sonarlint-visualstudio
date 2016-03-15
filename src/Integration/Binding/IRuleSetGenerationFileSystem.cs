//-----------------------------------------------------------------------
// <copyright file="IRuleSetGenerationFileSystem.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration.Binding
{
    // Interface for testing purposes only
    internal interface IRuleSetFileSystem
    {
        void WriteRuleSetFile(RuleSet ruleSet, string path);

        RuleSet LoadRuleSet(string path);
    }
}
