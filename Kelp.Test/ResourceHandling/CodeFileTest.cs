namespace Kelp.Test.ResourceHandling
{
	using System;
	using Kelp.ResourceHandling;

	public abstract class CodeFileTest
	{
		static CodeFileTest()
		{
			Configuration.Current.TemporaryDirectory =
				Util.MapPath(Configuration.Current.TemporaryDirectory);
		}
	}
}
