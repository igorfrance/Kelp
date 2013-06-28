namespace Kelp.Test.ResourceHandling
{
	using System;
	using Kelp.ResourceHandling;

	using Machine.Specifications;

	[Subject(typeof(CodeFile))]
	public class CodeFileTest
	{
		static CodeFileTest()
		{
			Configuration.Current.TemporaryDirectory =
				Util.MapPath(Configuration.Current.TemporaryDirectory);
		}

		~CodeFileTest()
		{
			Utilities.ClearTemporaryDirectory();
		}

		It Should_support_registered_text_based_file_extensions = () =>
		{
			CodeFile.IsFileExtensionSupported("js").ShouldBeTrue();
			CodeFile.IsFileExtensionSupported("css").ShouldBeTrue();
			CodeFile.IsFileExtensionSupported("xyz").ShouldBeFalse();
			CodeFile.IsFileExtensionSupported("jpeg").ShouldBeFalse();
		};
	}
}
