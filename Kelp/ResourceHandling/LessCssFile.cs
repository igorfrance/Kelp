namespace Kelp.ResourceHandling
{
	using System.IO;

	using dotless.Core.Importers;
	using dotless.Core.Input;
	using dotless.Core.Parser;
	using dotless.Core.Parser.Infrastructure;
	using dotless.Core.Parser.Tree;

	/// <summary>
	/// Represents a LESS CSS file. 
	/// </summary>
	[ResourceFile(ResourceType.LessCSS, "text/css", "less")]
	public class LessCssFile : CssFile, IPathResolver
	{
		/// <summary>
		/// Gets the full path.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns>System.String.</returns>
		/// <exception cref="System.NotImplementedException"></exception>
		public string GetFullPath(string path)
		{
			if (path.Contains(":"))
				return path;

			if (path.StartsWith("/") || path.StartsWith("~/"))
			{
				return Util.MapPath(path);
			}

			return Path.Combine(new FileInfo(this.AbsolutePath).DirectoryName, path);
		}

		/// <inheritdoc/>
		protected override string PostProcess(string sourceCode)
		{
			Parser parser = new Parser();

			((FileReader) ((Importer) parser.Importer).FileReader).PathResolver = this;

			Ruleset result = parser.Parse(sourceCode, this.AbsolutePath);

			Env env = new Env();
			sourceCode = result.ToCSS(env);
			return base.PostProcess(sourceCode);
		}
	}
}
