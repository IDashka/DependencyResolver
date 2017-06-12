using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DependencyResolver.FileHelpers {
    public static class ProjectFileExtensions {
        private static readonly XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";

        public static string GetOutputType(this XElement projectNode) {
            return projectNode.Descendants(ns + "PropertyGroup").Descendants(ns + "OutputType").First().Value;
        }

        public static string GetAssemblyName(this XElement projectNode) {
            return projectNode.Descendants(ns + "PropertyGroup").Descendants(ns + "AssemblyName").First().Value;
        }

        public static string GetOutputPath(this XElement projectNode) {
            return projectNode.Descendants(ns + "PropertyGroup").Descendants(ns + "OutputPath").First().Value;
        }

        //public static string GetNetVersion(this XElement projectNode) {
        //    return projectNode.Descendants(ns + "PropertyGroup").Descendants(ns + "TargetFrameworkVersion").First().Value;
        //}

        public static Guid GetProjectId(this XElement projectNode) {
            return Guid.Parse(projectNode.Descendants(ns + "PropertyGroup").Descendants(ns + "ProjectGuid").First().Value);
        }

        public static IList<string> GetReferencePaths(this XElement projectNode) {
            return projectNode.Descendants(ns + "ItemGroup").Descendants(ns + "Reference").Descendants(ns + "HintPath").Select(x=>Path.GetFullPath(x.Value)).ToList();
        }

        public static IList<Guid> GetReferenceProjectIds(this XElement projectNode) {
            return projectNode.Descendants(ns + "ItemGroup").Descendants(ns + "Project").Select(x=>Guid.Parse(x.Value)).ToList();
        }

        public static bool IsValidProject(this XElement projectNode) {
            return projectNode.Descendants(ns + "PropertyGroup").Descendants(ns + "OutputType").Any();
        }
    }
}