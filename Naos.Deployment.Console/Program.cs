// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    using System;

    using CLAP;

    /// <summary>
    /// Harness to run deployments if not wrapping in your own service.
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
    }
}
