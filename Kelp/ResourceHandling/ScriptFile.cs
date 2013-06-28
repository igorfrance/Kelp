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
	using System;
	using System.Diagnostics;
	using System.Diagnostics.Contracts;
	using System.Text;

	using Kelp.Extensions;

	using log4net;
	using Microsoft.Ajax.Utilities;

	/// <summary>
	/// Implements a JS file merger/processor, optionally minifying and obfuscating them.
	/// </summary>
	[ResourceFile(ResourceType.Script, "text/javascript", "js")]
	public class ScriptFile : CodeFile
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(ScriptFile));
		private readonly Stopwatch sw = new Stopwatch();

		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptFile"/> class.
		/// </summary>
		public ScriptFile()
			: base(ResourceHandling.Configuration.Current.Script)
		{
			this.ContentType = "text/javascript";
			this.ResourceType = ResourceType.Script;
		}

		/// <inheritdoc/>
		protected override string PostProcess(string sourceCode)
		{
			if (this.Configuration.MinificationEnabled)
			{
				log.DebugFormat("Minification of '{0}' took {1}ms", this.AbsolutePath,
					sw.TimeMilliseconds(() => sourceCode = this.Minify(this.content.ToString())));
			}

			return sourceCode;
		}

		/// <summary>
		/// Removes all comments and white-space and optimizes the source code for minimum size.
		/// </summary>
		/// <param name="sourceCode">The source code.</param>
		/// <returns>The minified source code.</returns>
		public string Minify(string sourceCode)
		{
			Minifier min = new Minifier();
			ScriptFileConfiguration scriptConfiguration = (ScriptFileConfiguration) this.Configuration;
			string minified = min.MinifyJavaScript(sourceCode, scriptConfiguration.Settings);

			if (min.ErrorList.Count == 0)
			{
				return minified;
			}

			// error handling:
			StringBuilder messages = new StringBuilder();
			foreach (var msg in min.ErrorList)
			{
				messages.AppendFormat("Line {0}, Col {1}: {2}\n", msg.StartLine, 
					msg.StartColumn, msg.Message);
			}

			log.ErrorFormat("Minifying javascript file '{0}' resulted in errors:\n{1}", this.AbsolutePath, messages);
			return sourceCode;
		}
	}
}
