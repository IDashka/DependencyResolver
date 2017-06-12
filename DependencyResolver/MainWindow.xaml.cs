using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DependencyResolver.FileHelpers;

namespace DependencyResolver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        readonly ProjectCollection projects = new ProjectCollection();
        private IList<string> includedProjectsPaths;
        private IList<string> allProjectPaths;

        readonly IList<string> commandsList = new List<string>
        {
            "cls",
            "help",
            "vcs",
            "load",
            "stat",
            "exit",
            "tofix",
            "max",
            "conf",
            "print",
            "analyse",
            "projlist",
            "errors",
            "badlist"
        }; 

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Init(textBoxOutput);
            ReadConfiguration();
            textBoxCmd.Focus();
        }

        private void ReadConfiguration() {
            listBoxTargets.Items.Clear();
            allProjectPaths = ConfigurationManager.AppSettings.Get("ProjectListLocation").GetAllFileLines();
            includedProjectsPaths = ConfigurationManager.AppSettings.Get("ProjectsToInvestigate").GetAllFileLines();
            textBoxVcsFolder.Text = projects.VcsRootFolder = ConfigurationManager.AppSettings.Get("VcsRootFolder");
            projects.AssemblyLocations = ConfigurationManager.AppSettings.Get("AssemblyLocations").GetAllFileLines();
            projects.KnownSolutions = ConfigurationManager.AppSettings.Get("KnownSolutions").GetAllFileLines();

            foreach (var path in includedProjectsPaths)
                listBoxTargets.Items.Add(path);
        }

        private void listBoxTargets_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            var projectPath = listBoxTargets.SelectedItem.ToString();
            PrintProjectData(projectPath);
            Analyse(projectPath);
        }

        private void Analyse(string projectPath) {
            var analyser = new DependencyAnalyser(projects.Items);
            var result = analyser.Analyse(new List<string> {projectPath});
            Logger.Log(projectPath + " depends on " + result.Count + " projects.");
            Logger.Log($"Reference errors total count is {result.SelectMany(x=>x.Errors).Count()}");
            Logger.Log($"Different reference errors: {result.SelectMany(x=>x.Errors).Distinct().Count()}");
            foreach (var projectRef in result.OrderBy(x => x))
                listBoxProjects.Items.Add(projectRef.ProjectPath);
        }

        private void Analyse(int index) {
            if (index < 0 || index >= listBoxTargets.Items.Count)
                Logger.Log("Invalid index");
            else
                Analyse(listBoxTargets.Items[index].ToString());
        }

        private void AnalyseAll()
        {
            var analyser = new DependencyAnalyser(projects.Items);
            var items = (from object item in listBoxTargets.Items select item.ToString()).ToList();

            var result = analyser.Analyse(items);
            Logger.Log("Everything depends on " + result.Count + " projects.");
            Logger.Log($"Reference errors total count is {result.SelectMany(x => x.Errors).Count()}");
            Logger.Log($"Different reference errors: {result.SelectMany(x => x.Errors).Distinct().Count()}");

            foreach (var projectRef in result)
                listBoxProjects.Items.Add(projectRef.ProjectPath);
        }

        private void PrintProjectData(string projectPath) {
            var project = projects.GetByPath(projectPath);
            if (project == null)
            {
                Logger.Log("Cannot find " + projectPath);
                return;
            }

            var details = project.GetDetails();
            labelDetails.Content = details;
            Logger.LogToFile(string.Empty);
            Logger.LogToFile("-------------------------------------");
            Logger.LogToFile("Details");
            Logger.LogToFile("-------------------------------------");
            Logger.LogToFile(details);
        }

        private void buttonLoad_Click(object sender, RoutedEventArgs e) {
            LoadProjects();
        }

        private void LoadProjects() {
            projects.Clear();
            foreach (var path in allProjectPaths)
                projects.Add(new Project(path));

            projects.ProcessAllReferences();

            buttonLoad.Visibility = Visibility.Hidden;
            PrintStatistics();
        }

        private void PrintReferencesToFix() {
            Logger.Log(string.Empty);
            Logger.Log("References to fix");
            Logger.Log("-----------------");
            var total = 0;
            foreach (var project in projects.Items
                .SelectMany(x => x.ReferencesToFix)
                .GroupBy(x => x)
                .Select(x => new {x.Key, Count = x.Count()})
                .OrderBy(x => x.Count))
            {
                total = +project.Count;
                Logger.Log($"{project.Count} occurrencies of {project.Key}");
            }

            Logger.Log("Total: " + total);
        }

        private void PrintVcsErrors() {
            Logger.Log(string.Empty);
            Logger.Log("Outside VCS");
            Logger.Log("-----------------");
            var total = 0;
            foreach (var project in projects.Items
                .SelectMany(x => x.OutsideReferences)
                .GroupBy(x => x)
                .Select(x => new {x.Key, Count = x.Count()})
                .OrderBy(x => x.Count))
            {
                total += project.Count;
                Logger.Log($"{project.Count} occurrencies of {project.Key}");
            }

            Logger.Log("Total: " + total);
        }

        private void PrintReferencesErrors() {
            Logger.Log(string.Empty);
            Logger.Log("Errors");
            Logger.Log("-----------------");
            var total = 0;

            foreach (var project in projects.Items
                .SelectMany(x => x.Errors)
                .GroupBy(x => x)
                .Select(x => new {x.Key, Count = x.Count()})
                .OrderBy(x => x.Count))
            {
                total += project.Count;
                Logger.Log($"{project.Count} occurrencies of {project.Key}");
            }

            Logger.Log("Total: " + total);
        }

        private void PrintStatistics() {
            Logger.Log(projects.PrintStatistics());
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.Finalise();
        }

        private void textBoxCmd_KeyDown(object sender, KeyEventArgs e) {
            var commandLine = textBoxCmd.Text.Trim();

            if (e.Key != Key.Enter || string.IsNullOrWhiteSpace(commandLine))
                return;

            var command = commandLine.Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries);
            if (!commandsList.Contains(command[0].ToLower()))
                Logger.Log("Unknown command " + commandLine);
            else
            {
                switch (command[0].ToLower())
                {
                    case "cls":
                        Logger.Clear();
                        break;
                    case "stat":
                        PrintStatistics();
                        break;
                    case "load":
                        LoadProjects();
                        break;
                    case "vcs":
                        PrintVcsErrors();
                        break;
                    case "tofix":
                        PrintReferencesToFix();
                        break;
                    case "errors":
                        PrintReferencesErrors();
                        break;
                    case "exit":
                        Close();
                        break;
                    case "help":
                        DisplayHelp();
                        break;
                    case "max":
                        WindowState = WindowState.Maximized;
                        break;
                    case "conf":
                        ReadConfiguration();
                        break;
                    case "projlist":
                        PrintProjects();
                        break;
                    case "badlist":
                        PrintBadProjects();
                        break;
                    case "print":
                        PrintProjectData(command[1]);
                        break;
                    case "analyse":
                        if (command[1] != "all")
                        {
                            int value;
                            var result = int.TryParse(command[1], out value);
                            if (!result)
                                Analyse(command[1]);
                            else
                                Analyse(value);
                        }
                        else
                            AnalyseAll();
                        break;
                    default:
                        Logger.Log("Command tool ERROR");
                        break;
                }
            }

            textBoxCmd.Clear();
            textBoxCmd.Focus();
        }
        
        private void PrintProjects() {
            foreach (var project in projects.Items.OrderBy(x=>x.ProjectPath))
                Logger.Log(project.ProjectPath);

            Logger.Log($"Total: {projects.Items.Count} projects.");
        }

        private void PrintBadProjects() {
            foreach (var project in projects.InvalidProjects.OrderBy(x=>x.ProjectPath))
                Logger.Log(project.ProjectPath);

            Logger.Log($"Total: {projects.InvalidProjects.Count} invalid projects.");
        }

        private void DisplayHelp() {
            foreach (var command in commandsList)
                Logger.Log(command);
        }
    }
}
