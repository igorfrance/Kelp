/**
 * Copyright 2012 Igor France
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
namespace Kelp.ResourceHandling
{
	using System.Diagnostics;
	using System.Text.RegularExpressions;

	using Kelp.Extensions;

	using Microsoft.Ajax.Utilities;

	using log4net;

	/// <summary>
	/// Implements a CSS file merger/processor.
	/// </summary>
	[ResourceFile(ResourceType.Css, "text/css", "css")]
	public class CssFile : CodeFile
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(CssFile).FullName);
		private readonly Stopwatch sw = new Stopwatch();
		private static readonly Regex rxpReferencePath =
			new Regex(@"(url\s*\((?:""|')?)(.*?)((?:""|')?\))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		/// <summary>
		/// Initializes a new instance of the <see cref="CssFile"/> class.
		/// </summary>
		public CssFile()
			: base(ResourceHandling.Configuration.Current.Css)
		{
			this.ContentType = "text/css";
			this.ResourceType = ResourceType.Css;
		}

		/// <summary>
		/// Removes all comments and white-space and optimizes the source code for minimum size.
		/// </summary>
		/// <param name="sourceCode">The source code.</param>
		/// <returns>The minified source code.</returns>
		public string Minify(string sourceCode)
		{
			Minifier min = new Minifier();
			CssFileConfiguration cssConfiguration = (CssFileConfiguration) this.Configuration;
			return min.MinifyStyleSheet(sourceCode, cssConfiguration.Settings);
		}

		/// <inheritdoc/>
		protected override string PreProcess(string sourceCode, string relativePath)
		{
			return this.FixRelativePaths(sourceCode, relativePath);
		}

		/// <inheritdoc/>
		protected override string PostProcess(string sourceCode)
		{
			if (this.Configuration.MinificationEnabled)
			{
				log.DebugFormat("Minification of '{0}' took {1}ms", this.AbsolutePath,
					sw.TimeMilliseconds(() => sourceCode = this.Minify(sourceCode)));
			}

			return sourceCode;
		}

		private static string CombineUrls(string folderUrl, string fileUrl)
		{
			if (folderUrl == string.Empty)
				return fileUrl;

			var filePath = string.Concat(folderUrl, "/", fileUrl).Replace("//", "/");
			while (Regex.Match(filePath, @"[^/]+/\.\./").Success)
			{
				filePath = Regex.Replace(filePath, @"[^/]+/\.\./", string.Empty);
			}

			filePath = Regex.Replace(filePath, "/{2,}", "/");
			return filePath;
		}

		private string FixRelativePaths(string sourceCode, string relativePath)
		{
			if (string.IsNullOrEmpty(relativePath))
				return sourceCode;

			var fileFolder = relativePath.Contains("/")
				? this.RelativePath.Substring(0, relativePath.LastIndexOf("/") + 1)
				: string.Empty;

			var processed = rxpReferencePath.Replace(sourceCode, delegate(Match m)
			{
				string fileName = m.Groups[2].Value;
				if (fileName.StartsWith("http") || fileName.StartsWith("/"))
					return m.Groups[0].Value;

				return string.Concat(m.Groups[1].Value, CombineUrls(fileFolder, fileName), m.Groups[3].Value);
			});

			return processed;
		}
	}
}
