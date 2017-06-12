using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using DependencyResolver.FileHelpers;

namespace DependencyResolver {
    public class Project {
        private string outputName;
        private string projectFolder;
        private bool isProcessed;
        private bool hasProcessError;
        private bool hasShortOutputPath;
        //private string dotNetVersion;
        public string ProjectPath { get; }
        public IList<Project> InternalReferences { get; }
        public IList<Project> ReferencesToFix { get; }
        public IList<string> ReferencesFixProposed { get; private set; }
        public IList<string> AssemblyReferences { get; }
        public IList<string> OutsideReferences { get; private set; } 
        public IList<string> Errors { get; } 
        public IList<Project> ReferencedBy { get; } 

        public string OutputPath
        {
            get
            {
                if (IsLibrary)
                {
                    var binDebug = hasShortOutputPath ? "bin\\" : "bin\\Debug\\";
                    return string.Format("{0}\\{2}{1}.dll", projectFolder, outputName, binDebug);
                }
                if (IsApplication)
                    return $"{projectFolder}\\bin\\Debug\\{outputName}.exe";

                //todo handle win exe
                return string.Empty;
            }
        }

        private bool IsLibrary { get; set; }
        private bool IsApplication { get; set; }
        public Guid? ProjectId { get; private set; }
        public IList<string> ReferencePaths { get; private set; }
        public IList<Guid> ReferenceProjectIds { get; private set; }
        public bool IsValid => isProcessed && !hasProcessError;

        public Project(string projectPath) {
            InternalReferences = new List<Project>();
            AssemblyReferences = new List<string>();
            OutsideReferences = new List<string>();
            ReferencedBy = new List<Project>();
            ReferencesToFix = new List<Project>();
            ReferencesFixProposed = new List<string>();
            Errors = new List<string>();
            ProjectPath = projectPath;
            ProcessProjectFile();
        }

        public override string ToString() {
            return ProjectPath;
        }

        private void ProcessProjectFile() {
            projectFolder = new FileInfo(ProjectPath).Directory.FullName;
            Directory.SetCurrentDirectory(projectFolder);

            XElement projectNode = XElement.Load(ProjectPath);

            if (!projectNode.IsValidProject())
            {
                hasProcessError = true;
                Logger.Error("Unsupported project file structure in " + ProjectPath);
                return;
            }

            outputName = projectNode.GetAssemblyName();
            ProjectId = projectNode.GetProjectId();
            //dotNetVersion = projectNode.GetNetVersion();
            SetOutputType(projectNode.GetOutputType());
            ReferencePaths = projectNode.GetReferencePaths();
            ReferenceProjectIds = projectNode.GetReferenceProjectIds();
            if (projectNode.GetOutputPath() == "bin\\")
                hasShortOutputPath = true;

            isProcessed = true;
        }

        private void SetOutputType(string outputType) {
            IsLibrary = false;
            IsApplication = false;

            if (outputType == "Library")
                IsLibrary = true;
            else if (outputType == "Exe")
                IsApplication = true;
            else if (outputType == "WinExe")
                //todo find out more about it
                hasProcessError = true;
            else
                Logger.Log("Unknown output type: " + ProjectPath);
        }

        public string GetDetails()
        {
            var projectFolder = new FileInfo(ProjectPath).Directory.FullName;
            Directory.SetCurrentDirectory(projectFolder);

            var builder = new StringBuilder();
            builder.AppendLine("Project file: " + ProjectPath);
            builder.AppendLine("Project output: " + OutputPath);
            builder.AppendLine("Project Guid: " + ProjectId);

            builder.AppendLine();
            builder.AppendLine("Errors:");
            foreach (var error in Errors)
                builder.AppendLine(error);

            builder.AppendLine();
            builder.AppendLine("References to fix:");
            foreach (var reference in ReferencesToFix)
                builder.AppendLine(reference.ProjectPath);

            builder.AppendLine();
            builder.AppendLine("Internal references:");
            foreach (var reference in InternalReferences)
                builder.AppendLine(reference.ProjectPath);

            builder.AppendLine();
            builder.AppendLine("Framework references:");
            foreach (var path in AssemblyReferences)
                builder.AppendLine(path);

            builder.AppendLine();
            builder.AppendLine("Referenced by:");
            foreach (var reference in ReferencedBy)
                builder.AppendLine(reference.ProjectPath);

            var content = builder.ToString();
            return content;
        }
    }
}