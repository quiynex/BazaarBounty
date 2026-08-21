using System.IO;
using System.Reflection;
using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace BazaarBounty.Utils
{
	internal static class PathUtils
	{
		public static string ExecutingAssemblyDirectory
		{
			get
			{
                string codeBase;
                UriBuilder uri;
                try{
                    codeBase = Assembly.GetExecutingAssembly().Location;
                    uri = new UriBuilder(codeBase);
                }
                catch {
                    string executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
                    string currentDirectory = Environment.CurrentDirectory;
                    codeBase = MakeRelativePath(currentDirectory, executingAssemblyPath);
                    uri = new UriBuilder(codeBase);
                }
				string path = Uri.UnescapeDataString(uri.Path);
				return Path.GetDirectoryName(path);
			}
		}
        private static string MakeRelativePath(string fromPath, string toPath)
        {
            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);
            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            return Uri.UnescapeDataString(relativeUri.ToString());
        }
	}
}