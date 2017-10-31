// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    using System;

    using CLAP;

    using Its.Log.Instrumentation;

    /// <summary>
    /// Exmaple of a main entry point of the application, just delete your 'Program.cs' is setup.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">Arguments for application.</param>
        /// <returns>Exit code.</returns>
        public static int Main(string[] args)
        {
            try
            {
                WriteAsciiArt(Console.WriteLine);

                /*---------------------------------------------------------------------------*
                 * This is just a pass through to the CLAP implementation of the harness,    *
                 * it will parse the command line arguments and provide multiple entry       *
                 * points as configured.  It is easiest to derive from the abstract class    *
                 * 'CommandLinAbstractionBase' as 'ExampleCommandLineAbstraction' does which *
                 * provides an example of the minimum amount of work to get started.  It is  *
                 * installed as a recipe for easy reference and covers help, errors, etc.    *
                 *---------------------------------------------------------------------------*
                 * For an example of config files you can install the package                *
                 * 'Naos.Recipes.Console.ExampleConfig' which has examples of the directory  *
                 * structure, 'LogProcessorSettings' settings for console and file, as well  *
                 * as an App.Config it not using the environment name as a parameter.        *
                 *---------------------------------------------------------------------------*
                 * Must update the code below to use your custom abstraction class.          *
                 *---------------------------------------------------------------------------*/
                var exitCode = Parser.Run<CommandLineAbstraction>(args);
                return exitCode;
            }
            catch (Exception ex)
            {
                /*---------------------------------------------------------------------------*
                 * This should never be reached but is here as a last ditch effort to ensure *
                 * errors are not lost.                                                      *
                 *---------------------------------------------------------------------------*/
                Console.WriteLine(string.Empty);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(string.Empty);
                Log.Write(ex);

                return 1;
            }
        }

        private static void WriteAsciiArt(Action<string> announcer)
        {
            announcer(@"<:::::::::::::::::::::::::::::::::::::::::}]xxxx()o             ");
            announcer(@"  _   _          ____   _____  _____             _              ");
            announcer(@" | \ | |   /\   / __ \ / ____||  __ \           | |             ");
            announcer(@" |  \| |  /  \ | |  | | (___  | |  | | ___ _ __ | | ___  _   _  ");
            announcer(@" | . ` | / /\ \| |  | |\___ \ | |  | |/ _ \ '_ \| |/ _ \| | | | ");
            announcer(@" | |\  |/ ____ \ |__| |____) || |__| |  __/ |_) | | (_) | |_| | ");
            announcer(@" |_| \_/_/    \_\____/|_____(_)_____/ \___| .__/|_|\___/ \__, | ");
            announcer(@"                                          | |             __/ | ");
            announcer(@"                                          |_|            |___/  ");
            announcer(@"             o()xxxx[{:::::::::::::::::::::::::::::::::::::::::>");
            announcer(string.Empty);
        }
    }
}