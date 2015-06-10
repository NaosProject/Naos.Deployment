// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetupStepFactorySettings.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System;

    /// <summary>
    /// Settings to be provided to the SetupStepFactory (defaults, Powershell script block, etc.)
    /// </summary>
    public class SetupStepFactorySettings
    {
        /// <summary>
        /// Gets or sets the default file size settings to use.
        /// </summary>
        public DatabaseFileSizeSettings DefaultDatabaseFileSizeSettings { get; set; }

        /// <summary>
        /// Gets or sets the script blocks to use for specific deployment steps.
        /// </summary>
        public DeploymentScriptBlockSet DeploymentScriptBlocks { get; set; }

        /// <summary>
        /// Gets or sets the server settings of the database.
        /// </summary>
        public DatabaseServerSettings DatabaseServerSettings { get; set; }
    }

    /// <summary>
    /// Settings class to provide information about the database server.
    /// </summary>
    public class DatabaseServerSettings
    {
        /// <summary>
        /// Gets or sets the Windows service name that is running SQL Server.
        /// </summary>
        public string SqlServiceName { get; set; }

        /// <summary>
        /// Gets or sets the Windows account that is running SQL Server.
        /// </summary>
        public string SqlServiceAccount { get; set; }

        /// <summary>
        /// Gets or sets the default directory to save data files.
        /// </summary>
        public string DefaultDataDirectory { get; set; }

        /// <summary>
        /// Gets or sets the default directory to backup databases to.
        /// </summary>
        public string DefaultBackupDirectory { get; set; }
    }

    /// <summary>
    /// Settings class to hold the specific set of script blocks for deployments.
    /// </summary>
    public class DeploymentScriptBlockSet
    {
        /// <summary>
        /// Gets or sets the script block to update the Its.Configuration precedence in a config file.
        /// </summary>
        public ScriptBlockDescription UpdateItsConfigPrecedence { get; set; }

        /// <summary>
        /// Gets or sets the script block to install and configure a website in IIS.
        /// </summary>
        public ScriptBlockDescription InstallAndConfigureWebsite { get; set; }

        /// <summary>
        /// Gets or sets the script block to create a new directory and grant full control to the specified user.
        /// </summary>
        public ScriptBlockDescription CreateDirectoryWithFullControl { get; set; }

        /// <summary>
        /// Gets or sets the script block to enable the SA account and change the password.
        /// </summary>
        public ScriptBlockDescription EnableSaAccountAndSetPassword { get; set; }

        /// <summary>
        /// Gets or sets the script block to unzip a file.
        /// </summary>
        public ScriptBlockDescription UnzipFile { get; set; }

        /// <summary>
        /// Gets or sets the script block to restart a windows service.
        /// </summary>
        public ScriptBlockDescription RestartWindowsService { get; set; }
    }

    /// <summary>
    /// Description of a script block.
    /// </summary>
    public class ScriptBlockDescription
    {
        /// <summary>
        /// Gets the full script as a string.
        /// </summary>
        public string ScriptText
        {
            get
            {
                return string.Join(Environment.NewLine, this.ScriptTextLines);
            }
        }

        /// <summary>
        /// Gets or sets the an array of the lines of a script (this allows it to have multiple lines in a JSON file).
        /// </summary>
        public string[] ScriptTextLines { get; set; }

        /// <summary>
        /// Gets or sets the parameter names of the script block (in order).
        /// </summary>
        public string[] ParameterNames { get; set; }
    }
}
