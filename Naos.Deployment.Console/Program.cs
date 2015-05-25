// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    using System;

    using CLAP;

    /// <summary>
    /// Harness to run deployments if not wrapping in your own service.
    /// </summary>
    public class Program
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
                WriteAsciiArt();
                Parser.Run<Deployer>(args);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Empty);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(string.Empty);
                return 1;
            }
        }

        private static void WriteAsciiArt()
        {
            Console.WriteLine(@"<:::::::::::::::::::::::::::::::::::::::::}]xxxx()o             ");
            Console.WriteLine(@"  _   _          ____   _____  _____             _              ");
            Console.WriteLine(@" | \ | |   /\   / __ \ / ____||  __ \           | |             ");
            Console.WriteLine(@" |  \| |  /  \ | |  | | (___  | |  | | ___ _ __ | | ___  _   _  ");
            Console.WriteLine(@" | . ` | / /\ \| |  | |\___ \ | |  | |/ _ \ '_ \| |/ _ \| | | | ");
            Console.WriteLine(@" | |\  |/ ____ \ |__| |____) || |__| |  __/ |_) | | (_) | |_| | ");
            Console.WriteLine(@" |_| \_/_/    \_\____/|_____(_)_____/ \___| .__/|_|\___/ \__, | ");
            Console.WriteLine(@"                                          | |             __/ | ");
            Console.WriteLine(@"                                          |_|            |___/  ");
            Console.WriteLine(@"             o()xxxx[{:::::::::::::::::::::::::::::::::::::::::>");
            Console.WriteLine(string.Empty);
            Console.WriteLine(string.Empty);
        }
    }
}
