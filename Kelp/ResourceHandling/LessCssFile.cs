namespace Kelp.ResourceHandling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	using dotless.Core.Parser;
	using dotless.Core.Parser.Infrastructure;
	using dotless.Core.Parser.Tree;

	/// <summary>
	/// Class CssLessFile
	/// </summary>
	[ResourceFile(ResourceType.LessCss, "text/css", "less")]
	public class LessCssFile : CssFile
	{
		/// <inheritdoc/>
		protected override string PostProcess(string sourceCode)
		{
			Parser parser = new Parser();
			Ruleset result = parser.Parse(sourceCode, this.AbsolutePath);

			sourceCode = result.ToCSS(new Env());
			return base.PostProcess(sourceCode);
		}
	}
}
