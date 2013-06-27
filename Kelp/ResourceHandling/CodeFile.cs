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
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.Contracts;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Security.Cryptography;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading;

	using Kelp.Extensions;
	using Kelp.Http;

	using log4net;

	/// <summary>
	/// Represents a code file that may include other code files.
	/// </summary>
	public abstract class CodeFile
	{
		/// <summary>
		/// A reference to the including (parent) <see cref="CodeFile"/> class, if this file 
		/// was included from another.
		/// </summary>
		protected CodeFile Parent { get; private set; }
		
		/// <summary>
		/// The file's fully processed source code.
		/// </summary>
		protected StringBuilder content = new StringBuilder();

		private static readonly ILog log = LogManager.GetLogger(typeof(CodeFile).FullName);
		private static readonly Regex instructionExpression = new Regex(@"^\s*/\*#\s*(?'name'\w+):\s*(?'value'.*?) \*/");
		private readonly Stopwatch sw = new Stopwatch();
		private bool isFromCache;

		protected CodeFile()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CodeFile"/> class, using the specified <paramref name="configuration"/>
		/// and <paramref name="parent"/>.
		/// </summary>
		/// <param name="configuration">The processing configuration for this file.</param>
		/// <param name="parent">The script processor creating this instance (used when processing includes)</param>
		protected CodeFile(FileTypeConfiguration configuration, CodeFile parent = null)
		{
			this.Parent = parent;
			this.Configuration = configuration;
			this.CachedConfigurationSettings = string.Empty;
			this.TemporaryDirectory = configuration.TemporaryDirectory;
			this.References = new OrderedDictionary<string, string>();
			if (parent != null)
				this.Parent = parent;
		}

		/// <summary>
		/// Gets the absolute path of this code file.
		/// </summary>
		/// <exception cref="ArgumentNullException">If the supplied value is <c>null</c> or empty.</exception>
		/// <exception cref="FileNotFoundException">If the specified value could not be resolved to an existing file.</exception>
		public string AbsolutePath { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this instance has been loaded and initialized entirely from cache.
		/// </summary>
		public bool PreviouslyCached
		{
			get
			{
				if (!this.initialized)
					this.Initialize();

				return this.isFromCache;
			}
		}

		/// <summary>
		/// Gets the relative path of this code file.
		/// </summary>
		public string RelativePath { get; private set; }

		/// <summary>
		/// Gets the list of files that this <see cref="CodeFile"/> is depending on
		/// </summary>
		public List<string> Dependencies
		{
			get
			{
				return this.References.Keys.ToList();
			}
		}

		/// <summary>
		/// Gets the last modified Date and Time of this <see cref="CodeFile"/>
		/// </summary>
		/// <remarks>
		/// This is done by getting the latest modification time from all files this code file depends on.
		/// </remarks> 
		public DateTime LastModified
		{
			get
			{
				if (File.Exists(this.CacheName))
					return Util.GetDateLastModified(this.CacheName);

				return Util.GetDateLastModified(this.Dependencies);
			}
		}

		/// <summary>
		/// Gets the fully processed content of this file.
		/// </summary>
		public string Content
		{
			get
			{
				this.Initialize();
				return content.ToString();
			}

			set
			{
				this.content = new StringBuilder(value ?? string.Empty);
			}
		}

		/// <summary>
		/// Gets the raw, unprocessed content of this file.
		/// </summary>
		public string RawContent
		{
			get
			{
				this.Load();
				return rawContent;
			}
		}

		/// <summary>
		/// Gets the previously persisted list of <see cref="Includes"/>.
		/// </summary>
		/// <remarks>
		/// This list is used to determine if a previously processed file needs to be refreshed due to changes in
		/// its constituent files.
		/// </remarks>
		public OrderedDictionary<string, string> References
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets an E-tag for the file represented with this instance.
		/// </summary>
		public string ETag
		{
			get
			{
				return GetETag(this.RelativePath, this.LastModified);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to output additional debug information to the output stream.
		/// </summary>
		internal bool DebugModeOn { get; set; }

		/// <summary>
		/// Gets the full name of the temporary file where the processed version of this file will be saved.
		/// </summary>
		/// <remarks>
		/// This is an absolute path.
		/// </remarks>
		internal string CacheName
		{
			get
			{
				if (this.Configuration == null)
					return null;

				string fileName = this.AbsolutePath.ReplaceAll(@"[\\/:]", "_");
				return Path.Combine(this.Configuration.TemporaryDirectory, fileName);
			}
		}

		/// <summary>
		/// Gets or sets the content-type of this code file.
		/// </summary>
		internal string ContentType { get; set; }

		internal ResourceType ResourceType { get; set; }

		/// <summary>
		/// Gets the configuration associated with this <see refe="CodeFile"/>'s file type.
		/// </summary>
		public FileTypeConfiguration Configuration { get; protected set; }

		internal string CachedConfigurationSettings { get; private set; }

		/// <summary>
		/// Gets a value indicating whether the cached <see cref="CodeFile"/> needs to be refreshed.
		/// </summary>
		/// <value><c>true</c> if the cached file is out of date; otherwise, <c>false</c>.</value>
		/// <remarks>
		/// The cached file is considered out-of-date when any of the included files has a last-modified-date 
		/// greater than the cached file.
		/// </remarks>
		protected virtual bool NeedsRefresh(string cacheName)
		{
			var currentSettings = this.Configuration.ToString();
			if (this.CachedConfigurationSettings != currentSettings)
				return true;

			if (!File.Exists(cacheName))
				return true;

			DateTime lastModified = Util.GetDateLastModified(cacheName);
			foreach (string file in this.references.Keys)
			{
				if (!File.Exists(file))
				{
					log.DebugFormat("Refresh needed for '{0}' because referenced file '{1}' doesn't exist.", AbsolutePath, file);
					return true;
				}

				var fileModified = Util.GetDateLastModified(file);
				if (fileModified > lastModified)
				{
					log.DebugFormat("Refresh needed for '{0}' (modified: {2}) because referenced file '{1}' (modified: {3}) is newer.", AbsolutePath, file, lastModified, fileModified);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Gets a value indicating whether minification is enabled for this code file.
		/// </summary>
		protected virtual bool MinificationEnabled
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Returns an instance of <see cref="CodeFile" /> that matches the specified <paramref name="resourceType" />.
		/// </summary>
		/// <param name="resourceType">The resource type .</param>
		/// <param name="absolutePath">Optional physical path of the file to load into created <see cref="CodeFile" />.</param>
		/// <param name="relativePath">Optional relative path of the file to load.</param>
		/// <param name="parent">Optional parent <see cref="CodeFile"/> that is including this <see cref="CodeFile"/></param>
		/// <returns>The code file matching the specified resource type, optionally loaded with contents from <paramref name="absolutePath"/>.</returns>
		public static CodeFile Create(ResourceType resourceType, string absolutePath = null, string relativePath = null, CodeFile parent = null)
		{
			CodeFile result;
			if (resourceType == ResourceType.Css)
				result = new CssFile();
			else
				result = new ScriptFile();

			if (parent != null)
				result.Parent = parent;

			if (!string.IsNullOrEmpty(absolutePath))
				result.Load(absolutePath, relativePath);

			return result;
		}

		/// <summary>
		/// Returns an instance of <see cref="CodeFile" />, loaded with content from <paramref name="absolutePath"/>.
		/// </summary>
		/// <param name="absolutePath">The physical path of the file to load into created <see cref="CodeFile" />.</param>
		/// <param name="relativePath">Optional relative path of the file to load.</param>
		/// <param name="parent">Optional parent <see cref="CodeFile"/> that is including this <see cref="CodeFile"/></param>
		/// <returns>A code file appropriate for the specified <paramref name="absolutePath"/>.
		/// If the extension of the specified <paramref name="absolutePath"/> is <c>css</c>, the resulting value will be a 
		/// new <see cref="CssFile"/>. In all other cases the resulting value is a <see cref="ScriptFile"/>.</returns>
		public static CodeFile Create(string absolutePath, string relativePath = null, CodeFile parent = null)
		{
			Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(absolutePath));

			if (absolutePath.ToLower().EndsWith("css"))
				return CodeFile.Create(ResourceType.Css, absolutePath, relativePath, parent);

			return CodeFile.Create(ResourceType.Script, absolutePath, relativePath, parent);
		}


		/// <summary>
		/// Gets an E-tag for the specified <paramref name="fileName"/> and <paramref name="lastModified"/> date.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="lastModified">The last modified date of the file.</param>
		/// <returns>The E-Tag that matches the specified <paramref name="fileName"/> and <paramref name="lastModified"/> date.</returns>
		public static string GetETag(string fileName, DateTime lastModified)
		{
			Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(fileName));

			Encoder stringEncoder = Encoding.UTF8.GetEncoder();
			MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

			string fileString = fileName + lastModified +
				Assembly.GetExecutingAssembly().GetName().Version;

			// get string bytes
			byte[] bytes = new byte[stringEncoder.GetByteCount(fileString.ToCharArray(), 0, fileString.Length, true)];
			stringEncoder.GetBytes(fileString.ToCharArray(), 0, fileString.Length, bytes, 0, true);

			return BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", string.Empty).ToLower();
		}

		/// <summary>
		/// Minifies the specified <paramref name="sourceCode"/>.
		/// </summary>
		/// <param name="sourceCode">The source code string to minify.</param>
		/// <returns>
		/// The minified version of this file's content.
		/// </returns>
		public abstract string Minify(string sourceCode);

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.RelativePath;
		}

		/// <summary>
		/// Adds a file to this code file.
		/// </summary>
		/// <param name="path">The path of the file to add.</param>
		/// <returns>A value indicating whether the file exists.</returns>
		public bool AddFile(string path)
		{
			var directory = Path.GetDirectoryName(this.AbsolutePath);
			var includePath = Path.Combine(directory, Regex.Replace(path, @"\?.*$", string.Empty));

			if (File.Exists(includePath))
			{
				if (includePath.Equals(this.AbsolutePath, StringComparison.InvariantCultureIgnoreCase))
				{
					log.FatalFormat("The script cannot include itself. The script is: {0}({1})", path, includePath);
					throw new InvalidOperationException("The script cannot include itself.");
				}

				if (IsPathInIncludeChain(includePath))
				{
					log.FatalFormat("Including the referenced path would cause recursion due to it being present at a level higher above. The script is: {0}({1})", path, includePath);
					throw new InvalidOperationException("Including the referenced path would cause recursion due to it being present at a level higher above.");
				}

				var inner = CodeFile.Create(includePath, path, this);
				this.content.AppendLine(inner.Content);

				foreach (var absPath in inner.References.Keys)
				{
					this.AddReference(absPath, inner.References[absPath]);
				}

				return true;
			}

			this.AddReference(includePath, null);
			return false;
		}

		/// <summary>
		/// Determines whether the specified path exists in the list of files already included.
		/// </summary>
		/// <param name="path">The path to check.</param>
		/// <returns>
		/// <c>true</c> if the specified path exists in the list of files already included; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">If the specified <c>path</c> is <c>null</c>.</exception>
		internal bool IsPathInIncludeChain(string path)
		{
			if (path == null)
				throw new ArgumentNullException("path");

			if (parent == null)
				return false;

			bool contains = false;
			CodeFile ancestor = this;
			while (ancestor != null)
			{
				contains = ancestor.Dependencies.Any(value => 
					value.Equals(path, StringComparison.InvariantCultureIgnoreCase));

				ancestor = ancestor.parent;
			}

			return contains;
		}

		/// <summary>
		/// Initialized this <see cref="CodeFile"/>.
		/// </summary>
		protected void InitializeOld()
		{
			if (this.initialized)
				return;

			while (true)
			{
				try
				{
					this.InitializeActual();
					break;
				}
				catch (IOException)
				{
					if (++this.retryCount == MaxLoadAttempts)
						throw;

					Thread.Sleep(TimeSpan.FromMilliseconds(RetryWaitInterval));
				}
			}

			this.retryCount = 0;
		}

		private void InitializeActualOld()
		{
			if (this.initialized || this.initializing)
				return;

			this.initializing = true;
			this.Load();

			if (File.Exists(this.CacheName))
			{
				log.DebugFormat("Parsing cached file for '{0}'", this.AbsolutePath);
				this.Parse(File.ReadAllText(this.CacheName), false);
			}

			if (!this.NeedsRefresh)
			{
				log.DebugFormat("No refresh needed for '{0}', returning.", this.AbsolutePath);
				this.initialized = true;
				this.isFromCache = true;
				return;
			}

			this.references.Clear();

			log.DebugFormat("Parsing raw content for '{0}'.", this.AbsolutePath);

			this.Parse(this.rawContent, true);

			if (!string.IsNullOrEmpty(this.CacheName))
			{
				if (!Directory.Exists(this.TemporaryDirectory))
				{
					try
					{
						Directory.CreateDirectory(this.TemporaryDirectory);
					}
					catch (Exception ex)
					{
						log.ErrorFormat("Could not create temporary directory '{0}': {1}", this.TemporaryDirectory, ex.Message);
					}
				}

				try
				{
					StringBuilder persistContent = new StringBuilder();
					persistContent.AppendLine(string.Format("/*# Configuration: {0} */", this.Configuration));
					foreach (string includePath in this.references.Keys)
						persistContent.AppendLine(string.Format("/*# Reference: {0} | {1} */", includePath, this.references[includePath]));

					persistContent.AppendLine();
					persistContent.Append(this.content);

					File.WriteAllText(this.CacheName, persistContent.ToString(), Encoding.UTF8);
					log.DebugFormat("Saved the temporary contents of '{0}' to '{1}'.", AbsolutePath, this.CacheName);
				}
				catch (Exception ex)
				{
					log.ErrorFormat("Could not save the temporary file '{0}': {1}", this.CacheName, ex.Message);
				}
			}

			this.initialized = true;
			this.initializing = false;
		}

		/// <summary>
		/// Loads the file, either from cache or from its <see cref="AbsolutePath"/>.
		/// </summary>
		protected virtual void Load(string absolutePath, string relativePath = null)
		{
			Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(absolutePath));
			Contract.Requires<FileNotFoundException>(File.Exists(absolutePath));

			this.AbsolutePath = absolutePath;
			if (string.IsNullOrEmpty(relativePath))
			{
				this.RelativePath = Path.GetFileName(absolutePath);
			}
			if (this.Parent != null)
			{
				this.RelativePath = Path.Combine(Path.GetDirectoryName(Parent.RelativePath), this.RelativePath);
			}

			var cacheEntry = new CacheEntry(this.CacheName);

			if (this.NeedsRefresh(cacheEntry))
			{
				var sourceCode = File.ReadAllText(absolutePath, Encoding.UTF8);
				var result = this.Parse(sourceCode, this.MinificationEnabled);

				this.content = new StringBuilder(result.Content.ToString());

				this.References.Clear();
				foreach (KeyValuePair<string, string> reference in result.References)
					this.AddReference(reference.Key, reference.Value);

				this.References.Add(this.AbsolutePath, this.RelativePath);
			}
			else
			{
				
			}
		}

		/// <summary>
		/// Scans through the specified source code and processes it line by line.
		/// </summary>
		/// <param name="sourceCode">The source code to parse.</param>
		/// <returns>The object that contains the result of parsing the source code.</returns>
		protected virtual ParseResult Parse(string sourceCode)
		{
			Contract.Requires<ArgumentNullException>(sourceCode != null);

			ParseResult result = new ParseResult(this.ResourceType);
			string[] lines = sourceCode.Replace("\r", string.Empty).Split(new[] { '\n' });

			foreach (string line in lines)
			{
				Match match;
				if ((match = instructionExpression.Match(line)).Success)
				{
					string name = match.Groups["name"].Value;
					string value = match.Groups["value"].Value;

					switch (name.ToLower())
					{
						case "reference":
							string[] parts = value.Split('|');
							if (parts.Length == 2)
								result.AddReference(parts[0].Trim(), parts[1].Trim());

							break;

						case "configuration":
							result.Configuration = value;
							break;

						case "include":
							var exists = this.AddFile(value);
							if (!exists)
								result.Content.AppendLine(line + "/* File not found */");
							break;

						default:
							log.WarnFormat("Unrecognized processing instruction '{0}' encountered.", name);
							break;
					}
				}
				else
					result.Content.AppendLine(line);
			}

			return result;
		}

		/// <summary>
		/// Scans through the specified source code and processes it line by line.
		/// </summary>
		/// <param name="sourceCode">The source code to parse.</param>
		/// <param name="minify">If set to <c>true</c>, and the current settings indicate that the source code will be minified.</param>
		/// <returns>The object that contains the result of parsing the source code.</returns>
		protected virtual ParseResult Parse(string sourceCode, bool minify)
		{
			var result = CodeFile.Parse(sourceCode);
			if (this.Configuration.MinificationEnabled && minify)
			{
				log.DebugFormat("Minification of '{0}' took {1}ms", this.AbsolutePath,
					sw.TimeMilliseconds(() => this.content = new StringBuilder(this.Minify(this.content.ToString()))));
			}

			return result;
		}

		private bool NeedsRefresh(CacheEntry cacheEntry)
		{
			if (!cacheEntry.Exists)
				return true;

			var parsed = this.Parse(cacheEntry.Content, false);
			foreach (string filePath in parsed.References.Keys)
			{
				if (!File.Exists(filePath))
					return true;

				var fileModified = Util.GetDateLastModified(filePath);
				if (fileModified > this.LastModified)
					return true;
			}
		}

		private void AddReference(string absolutePath, string relativePath)
		{
			string referenceKey = absolutePath.ToLower().Replace("/", "\\").Replace("\\\\", "\\");
			if (this.References.ContainsKey(referenceKey))
				return;

			this.References.Add(referenceKey, relativePath);
		}

		protected class ParseResult
		{
			public StringBuilder Content = new StringBuilder();
			
			public OrderedDictionary<string, string> References = new OrderedDictionary<string, string>();
			
			public string Configuration;

			public ResourceType Type;

			public ParseResult(ResourceType type)
			{
				this.Type = type;
			}

			public bool AddFile2(string path)
			{
				if (this.ReferencesFile(path))
					return true;

				if (!File.Exists(path))
					return false;

				var parsed = CodeFile.Create(this.Type);
				File.ReadAllText(path, Encoding.UTF8));
				foreach (KeyValuePair<string, string> reference in parsed.References)
					this.AddReference(reference.Key, reference.Value);

				this.Content.AppendLine(parsed.Content);

				this.AddReference(path, null);
				return false;
			}

			/// <summary>
			/// Adds a file to this code file.
			/// </summary>
			/// <param name="path">The path of the file to add.</param>
			/// <returns>A value indicating whether the file exists.</returns>
			public bool AddFile(string path)
			{
				var directory = Path.GetDirectoryName(this.AbsolutePath);
				var includePath = Path.Combine(directory, Regex.Replace(path, @"\?.*$", string.Empty));

				if (File.Exists(includePath))
				{
					if (includePath.Equals(this.AbsolutePath, StringComparison.InvariantCultureIgnoreCase))
					{
						log.FatalFormat("The script cannot include itself. The script is: {0}({1})", path, includePath);
						throw new InvalidOperationException("The script cannot include itself.");
					}

					if (IsPathInIncludeChain(includePath))
					{
						log.FatalFormat("Including the referenced path would cause recursion due to it being present at a level higher above. The script is: {0}({1})", path, includePath);
						throw new InvalidOperationException("Including the referenced path would cause recursion due to it being present at a level higher above.");
					}

					var inner = CodeFile.Create(includePath, path, this);
					this.content.AppendLine(inner.Content);

					foreach (var absPath in inner.References.Keys)
					{
						this.AddReference(absPath, inner.References[absPath]);
					}

					return true;
				}

				this.AddReference(includePath, null);
				return false;
			}

			public void AddReference(string absolutePath, string relativePath)
			{
				string key = absolutePath.ToLower().Replace("/", "\\").Replace("\\\\", "\\");
				if (this.References.ContainsKey(key))
					return;

				this.References.Add(key, relativePath);
			}

			public bool ReferencesFile(string file)
			{
				return this.References.ContainsKey((file ?? string.Empty).ToLower());
			}
		}

		protected class CacheEntry
		{
			public bool Exists;

			public DateTime LastModified;

			public string Content;

			public ParseResult ParseResult;

			public CacheEntry(string cacheName)
			{
				if (File.Exists(cacheName))
				{
					this.Exists = true;
					this.Content = File.ReadAllText(cacheName, Encoding.UTF8);
					this.LastModified = Util.GetDateLastModified(cacheName);
				}
			}
		}
	}
}
