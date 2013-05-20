namespace Kelp.Test.ResourceHandling
{
	using System;
	using Kelp.ResourceHandling;

	public abstract class CodeFileTest
	{
		static CodeFileTest()
		{
			Configuration.Current.TemporaryDirectory =
				Utilities.MapPath(Configuration.Current.TemporaryDirectory);
		}
	}
}
