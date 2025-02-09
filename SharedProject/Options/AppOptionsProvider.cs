﻿using System;
using System.ComponentModel.Composition;
using System.Reflection;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Options
{
    [Export(typeof(IAppOptionsProvider))]
    [Export(typeof(IAppOptionsStorageProvider))]
    internal class AppOptionsProvider : IAppOptionsProvider, IAppOptionsStorageProvider
    {
        private readonly ILogger logger;
        private readonly IWritableSettingsStoreProvider writableSettingsStoreProvider;
        private readonly IJsonConvertService jsonConvertService;
        private readonly PropertyInfo[] appOptionsPropertyInfos;

        public event Action<IAppOptions> OptionsChanged;

        [ImportingConstructor]
        public AppOptionsProvider(
            ILogger logger, 
            IWritableSettingsStoreProvider writableSettingsStoreProvider,
            IJsonConvertService jsonConvertService
            )
        {
            this.logger = logger;
            this.writableSettingsStoreProvider = writableSettingsStoreProvider;
            this.jsonConvertService = jsonConvertService;
            appOptionsPropertyInfos =typeof(IAppOptions).GetPublicProperties();
        }

        public void RaiseOptionsChanged(IAppOptions appOptions)
        {
            OptionsChanged?.Invoke(appOptions);
        }

        public IAppOptions Get()
        {
            var options = new AppOptions();
            LoadSettingsFromStorage(options);
            return options;
        }

        private IWritableSettingsStore EnsureStore()
        {
            var settingsStore = writableSettingsStoreProvider.Provide();
            if (!settingsStore.CollectionExists(Vsix.Code))
            {
                settingsStore.CreateCollection(Vsix.Code);
            }
            return settingsStore;
        }

        private void AddDefaults(IAppOptions appOptions)
        {
            appOptions.NamespacedClasses = true;
            appOptions.ThresholdForCrapScore = 15;
            appOptions.ThresholdForNPathComplexity = 200;
            appOptions.ThresholdForCyclomaticComplexity = 30;
            appOptions.RunSettingsOnly = true;
            appOptions.RunWhenTestsFail = true;
            appOptions.ExcludeByAttribute = new[] { "GeneratedCode" };
            appOptions.IncludeTestAssembly = true;
            appOptions.ExcludeByFile = new[] { "**/Migrations/*" };
            appOptions.Enabled = true;
            appOptions.ShowCoverageInOverviewMargin = true;
            appOptions.ShowCoveredInOverviewMargin = true;
            appOptions.ShowPartiallyCoveredInOverviewMargin = true;
            appOptions.ShowUncoveredInOverviewMargin = true;
        }

        public void LoadSettingsFromStorage(IAppOptions instance)
        {
            AddDefaults(instance);

            var settingsStore = EnsureStore();

            foreach (var property in appOptionsPropertyInfos)
            {
                try
                {
                    if (!settingsStore.PropertyExists(Vsix.Code, property.Name))
                    {
                        continue;
                    }

                    var strValue = settingsStore.GetString(Vsix.Code, property.Name);

                    if (string.IsNullOrWhiteSpace(strValue))
                    {
                        continue;
                    }

                    var objValue = jsonConvertService.DeserializeObject(strValue, property.PropertyType);
                    
                    property.SetValue(instance, objValue);
                }
                catch (Exception exception)
                {
                    logger.Log($"Failed to load '{property.Name}' setting", exception);
                }
            }
        }

        public void SaveSettingsToStorage(IAppOptions appOptions)
        {
            var settingsStore = EnsureStore();

            foreach (var property in appOptionsPropertyInfos)
            {
                try
                {
                    var objValue = property.GetValue(appOptions);
                    var strValue = jsonConvertService.SerializeObject(objValue);

                    settingsStore.SetString(Vsix.Code, property.Name, strValue);
                }
                catch (Exception exception)
                {
                    logger.Log($"Failed to save '{property.Name}' setting", exception);
                }
            }
            RaiseOptionsChanged(appOptions);
        }
    }

    internal class AppOptions : IAppOptions
    {
        public string[] Exclude { get; set; }

        public string[] ExcludeByAttribute { get; set; }

        public string[] ExcludeByFile { get; set; }

        public string[] Include { get; set; }

        public bool RunInParallel { get; set; }

        public int RunWhenTestsExceed { get; set; }

        public string ToolsDirectory { get; set; }

        public bool RunWhenTestsFail { get; set; }

        public bool RunSettingsOnly { get; set; }

        public bool CoverletConsoleGlobal { get; set; }

        public string CoverletConsoleCustomPath { get; set; }

        public bool CoverletConsoleLocal { get; set; }

        public string CoverletCollectorDirectoryPath { get; set; }

        public string OpenCoverCustomPath { get; set; }

        public string FCCSolutionOutputDirectoryName { get; set; }

        public int ThresholdForCyclomaticComplexity { get; set; }

        public int ThresholdForNPathComplexity { get; set; }

        public int ThresholdForCrapScore { get; set; }

        public bool CoverageColoursFromFontsAndColours { get; set; }

        public bool ShowCoverageInOverviewMargin { get; set; }
        
        public bool ShowCoveredInOverviewMargin { get; set; }
        
        public bool ShowUncoveredInOverviewMargin { get; set; }
        
        public bool ShowPartiallyCoveredInOverviewMargin { get; set; }

        public bool StickyCoverageTable { get; set; }

        public bool NamespacedClasses { get; set; }

        public bool HideFullyCovered { get; set; }

        public bool AdjacentBuildOutput { get; set; }

        public RunMsCodeCoverage RunMsCodeCoverage { get; set; }
        public string[] ModulePathsExclude { get; set; }
        public string[] ModulePathsInclude { get; set; }
        public string[] CompanyNamesExclude { get; set; }
        public string[] CompanyNamesInclude { get; set; }
        public string[] PublicKeyTokensExclude { get; set; }
        public string[] PublicKeyTokensInclude { get; set; }
        public string[] SourcesExclude { get; set; }
        public string[] SourcesInclude { get; set; }
        public string[] AttributesExclude { get; set; }
        public string[] AttributesInclude { get; set; }
        public string[] FunctionsInclude { get; set; }
        public string[] FunctionsExclude { get; set; }

        public bool Enabled { get; set; }

        public bool IncludeTestAssembly { get; set; }

        public bool IncludeReferencedProjects { get; set; }
    }
}
