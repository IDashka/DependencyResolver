using System;
using System.Collections.Generic;
using System.Linq;

namespace DependencyResolver {
    public class DependencyAnalyser {
        public IList<Project> ValidProjects { get; set; } 
        private readonly IList<Project> analysisDone  = new List<Project>();
        private readonly IList<Project> analysisInProgress  = new List<Project>();

        public DependencyAnalyser(IList<Project> validProjects) {
            ValidProjects = validProjects;
        }

        public IList<Project> Analyse(IList<string> projectsToAnalyse) {
            Logger.Log("-----------");
            Logger.Log("Analysis");
            Logger.Log("-----------");
            analysisDone.Clear();

            foreach (var projectPath in projectsToAnalyse)
            {
                var project = ValidProjects.FirstOrDefault(x => x.ProjectPath == projectPath);
                if (project == null)
                    Logger.Log("Project not found " + projectPath);

                try
                {
                    Analyse(project);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
                
            }

            return analysisDone;
        }

        private void Analyse(Project project) {
            Logger.LogToFile("Analysing project " + project.ProjectPath);
            if (analysisDone.Contains(project))
                return;

            if (analysisInProgress.Contains(project))
                throw new ApplicationException("Circular reference to " + project.ProjectPath);

            analysisInProgress.Add(project);
            foreach (var reference in project.InternalReferences)
                Analyse(reference);

            foreach (var reference in project.ReferencesToFix)
                Analyse(reference);

            foreach (var error in project.Errors)
                Logger.LogToFile($"This project {project.ProjectPath} has error {error}");

            analysisInProgress.Remove(project);
            analysisDone.Add(project);
        }
    }
}