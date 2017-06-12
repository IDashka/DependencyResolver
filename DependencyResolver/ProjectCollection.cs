using System.Collections.Generic;
using System.Linq;
using System.Text;
using DependencyResolver.FileHelpers;

namespace DependencyResolver {
    public class ProjectCollection {
        private readonly IList<Project> projects = new List<Project>();
        public IList<Project> Items => projects;
        public IList<string> KnownSolutions { get; set; } 

        public string VcsRootFolder { get; set; }
        public IList<Project> InvalidProjects { get; }
        public IList<string> AssemblyLocations { get; set; }

        public ProjectCollection() {
            InvalidProjects = new List<Project>();
        }

        public Project GetByPath(string path) {
            return projects.FirstOrDefault(x => x.ProjectPath == path);
        }

        public string PrintStatistics()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Valid projects: " + Items.Count);
            builder.AppendLine("Invalid projects: " + InvalidProjects.Count);

            builder.AppendLine("References outside VCS: " + Items.Sum(x => x.OutsideReferences.Count));
            builder.AppendLine("Assembly locations: " + AssemblyLocations.Count);
            builder.AppendLine("Errors: " + Items.Sum(x => x.Errors.Count));
            builder.AppendLine("References to fix: " + Items.Sum(x => x.ReferencesToFix.Count));
            builder.AppendLine("Known solutions: " + KnownSolutions.Count);
            builder.AppendLine("Known solutions will fix: " + Items.Sum(x => x.ReferencesFixProposed.Count));
            builder.AppendLine("Projects with errors: " + Items.Count(x => x.Errors.Any()));
            builder.AppendLine("Projects without errors: " + Items.Count(x => !x.Errors.Any()));
            builder.AppendLine("Projects with fixed references: " + Items.Count(x => !x.ReferencesToFix.Any()));
            builder.AppendLine("Projects with references to fix: " + Items.Count(x => x.ReferencesToFix.Any()));
            return builder.ToString();
        }


        public void Add(Project project) {
            if (project.IsValid)
                projects.Add(project);
            else
                InvalidProjects.Add(project);
        }

        public void ProcessAllReferences() {
            foreach (var project in projects)
            {
                foreach (var projectId in project.ReferenceProjectIds)
                {
                    var refProject = projects.FirstOrDefault(x => x.ProjectId == projectId);
                    if (refProject == null)
                    {
                        project.Errors.Add(projectId.ToString());
                        Logger.Log(string.Format("Not found reference {0} by guid {1}", project.ProjectPath, projectId));
                        continue;
                    }
                    project.InternalReferences.Add(refProject);
                    refProject.ReferencedBy.Add(project);
                }

                foreach (var referencePath in project.ReferencePaths)
                {
                    if (KnownSolutions.Contains(referencePath))
                        project.ReferencesFixProposed.Add(referencePath);

                    else if (!referencePath.StartsWith(VcsRootFolder))
                        project.OutsideReferences.Add(referencePath);
                    else if (referencePath.StartsWithAny(AssemblyLocations))
                        project.AssemblyReferences.Add(referencePath);
                    else
                    {

                        var refProject = projects.FirstOrDefault(x => x.OutputPath == referencePath);
                        if (refProject == null)
                        {
                            project.Errors.Add(referencePath);
                            Logger.Log(string.Format("Not found reference {0} by path {1}", project.ProjectPath, referencePath));
                        }
                        else
                        {
                            project.ReferencesToFix.Add(refProject);
                            refProject.ReferencedBy.Add(project);
                        }
                    }
                }
            }
        }

        public void Clear() {
            projects.Clear();
            //KnownSolutions.Clear();
            //vcs root
            InvalidProjects.Clear();
            //AssemblyLocations.Clear();
        }
    }
}