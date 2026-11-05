using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _Project.Core.Configs;
using _Project.Editor.Configs;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Balance
{
    public class BalanceSimulationLauncher
    {
        private const string LogPrefix = "[Balance]";
        private const string ProjectFolder = "..";

        public void Launch()
        {
            GameConfigCheck configCheck = new GameConfigCheck();
            IReadOnlyList<ConfigError> errors = configCheck.Run();

            if (errors.Count > 0)
            {
                configCheck.Report(errors);

                return;
            }

            BalanceSimulationSettings settings = LoadSettings();
            IReadOnlyList<BalanceRun> runs = new BalanceSimulationRunner().Run(settings);
            string report = new BalanceReportWriter(settings).Write(runs, DateTime.Now);
            string reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, ProjectFolder, settings.ReportPath));

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report, new UTF8Encoding(false));
            Debug.Log($"{LogPrefix} Отчёт записан в {settings.ReportPath}");
        }

        private BalanceSimulationSettings LoadSettings()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(BalanceSimulationSettings)}");

            if (guids.Length != 1)
            {
                throw new InvalidOperationException(nameof(BalanceSimulationSettings));
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);

            return AssetDatabase.LoadAssetAtPath<BalanceSimulationSettings>(path);
        }
    }
}
