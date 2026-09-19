using System;
using System.Collections.Generic;
using System.IO;

namespace StoicGoose.Common.Utilities
{
	public static class ApplicationPaths
	{
		public static string GetDataDirectory(string productName)
		{
			if (string.IsNullOrWhiteSpace(productName))
				productName = "StoicGoose";

			foreach (var candidate in EnumerateCandidates(productName))
			{
				if (TryEnsureDirectory(candidate))
					return candidate;
			}

			return Path.Combine(Path.GetTempPath(), productName);
		}

		static IEnumerable<string> EnumerateCandidates(string productName)
		{
			var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (IsUsableRoot(documents))
				yield return Path.Combine(documents, productName);

			var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
			if (IsUsableRoot(xdgDataHome))
				yield return Path.Combine(xdgDataHome, productName);

			var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			if (IsUsableRoot(userProfile))
				yield return Path.Combine(userProfile, ".local", "share", productName);

			var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			if (IsUsableRoot(appData))
				yield return Path.Combine(appData, productName);
		}

		static bool IsUsableRoot(string root)
		{
			if (string.IsNullOrWhiteSpace(root))
				return false;

			if (File.Exists(root))
				return false;

			return Path.IsPathRooted(root);
		}

		static bool TryEnsureDirectory(string path)
		{
			if (string.IsNullOrWhiteSpace(path) || File.Exists(path))
				return false;

			try
			{
				Directory.CreateDirectory(path);
				return Directory.Exists(path);
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
