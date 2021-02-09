﻿using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Output;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(IInitializer))]
    internal class Initializer : IInitializer
    {
        private readonly IFCCEngine fccEngine;

        [ImportingConstructor]
        public Initializer(IFCCEngine fccEngine)
        {
            this.fccEngine = fccEngine;
        }
        public void Initialize(IServiceProvider _serviceProvider)
        {
            try
            {
                Logger.Initialize(_serviceProvider);

                fccEngine.Initialize(_serviceProvider);
                InitializePackageAndToolWindow(_serviceProvider);

                Logger.Log($"Initialized");
            }
            catch (Exception exception)
            {
                Logger.Log($"Failed Initialization", exception);
            }
        }
        [SuppressMessage("Usage", "VSTHRD102:Implement internal logic asynchronously")]
        private void InitializePackageAndToolWindow(IServiceProvider serviceProvider)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
                {
                    var packageToBeLoadedGuid = new Guid(OutputToolWindowPackage.PackageGuidString);
                    shell.LoadPackage(ref packageToBeLoadedGuid, out var package);

                    var outputWindowInitializedFile = Path.Combine(fccEngine.AppDataFolder, "outputWindowInitialized");

                    if (File.Exists(outputWindowInitializedFile))
                    {
                        OutputToolWindowCommand.Instance.FindToolWindow();
                    }
                    else
                    {
                        // for first time users, the window is automatically docked 
                        OutputToolWindowCommand.Instance.ShowToolWindow();
                        File.WriteAllText(outputWindowInitializedFile, string.Empty);
                    }
                }
            });
        }
    }

}
