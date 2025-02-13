// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DownloadAndRestoreDatabaseOpTest.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using FakeItEasy;
    using Naos.Deployment.Domain;
    using Naos.FileJanitor.Domain;
    using Naos.SqlServer.Domain;
    using OBeautifulCode.Assertion.Recipes;
    using OBeautifulCode.CodeGen.ModelObject.Recipes;
    using OBeautifulCode.Type;
    using static System.FormattableString;

    public static partial class DownloadAndRestoreDatabaseOpTest
    {
        static DownloadAndRestoreDatabaseOpTest()
        {
            ConstructorArgumentValidationTestScenarios
                .RemoveAllScenarios()
                .AddScenario(() =>
                    new ConstructorArgumentValidationTestScenario<DownloadAndRestoreDatabaseOp>
                    {
                        Name = "constructor should throw ArgumentNullException when parameter 'databaseName' is null scenario",
                        ConstructionFunc = () =>
                        {
                            var referenceObject = A.Dummy<DownloadAndRestoreDatabaseOp>();

                            var result = new DownloadAndRestoreDatabaseOp(
                                                 null,
                                                 referenceObject.Timeout,
                                                 referenceObject.KeyPrefixSearchPattern,
                                                 referenceObject.MultipleKeysFoundStrategy);

                            return result;
                        },
                        ExpectedExceptionType = typeof(ArgumentNullException),
                        ExpectedExceptionMessageContains = new[] { "databaseName", },
                    })
                .AddScenario(() =>
                    new ConstructorArgumentValidationTestScenario<DownloadAndRestoreDatabaseOp>
                    {
                        Name = "constructor should throw ArgumentException when parameter 'databaseName' is white space scenario",
                        ConstructionFunc = () =>
                        {
                            var referenceObject = A.Dummy<DownloadAndRestoreDatabaseOp>();

                            var result = new DownloadAndRestoreDatabaseOp(
                                                 Invariant($"  {Environment.NewLine}  "),
                                                 referenceObject.Timeout,
                                                 referenceObject.KeyPrefixSearchPattern,
                                                 referenceObject.MultipleKeysFoundStrategy);

                            return result;
                        },
                        ExpectedExceptionType = typeof(ArgumentException),
                        ExpectedExceptionMessageContains = new[] { "databaseName", "white space", },
                    });
        }
    }
}
