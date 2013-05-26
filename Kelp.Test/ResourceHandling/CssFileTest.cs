﻿// <auto-generated>Marked as auto-generated so StyleCop will ignore BDD style tests</auto-generated>
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
namespace Kelp.Test.ResourceHandling
{
	using System;
	using System.IO;

	using Kelp.ResourceHandling;
	using Machine.Specifications;

	[Subject(typeof(CssFile)), Tags(Categories.ResourceHandling)]
	public class When_requesting_a_non_minified_css_file : CodeFileTest
	{
		private static readonly string scriptPath = Utilities.GetStylePath("stylesheet1.css");
		private static string contents;
		private static CssFile proc;

		private Establish ctx = () =>
		{
			var tempDirectory = Util.MapPath(Configuration.Current.TemporaryDirectory);
			if (Directory.Exists(tempDirectory))
				Directory.Delete(tempDirectory, true);

			proc = new CssFile(scriptPath, "stylesheet1.css");
		};

		private Because of = () => contents = proc.Content;

		private It Should_not_be_empty = () => contents.ShouldNotBeEmpty();
		private It Should_contain_child_stylesheets = () => contents.ShouldContain("#example1");
		private It Should_contain_child_stylesheets2 = () => contents.ShouldContain("url(image1.jpg)");
		private It Should_still_contain_empty_styles = () => contents.ShouldContain("#empty");
	}

	[Subject(typeof(CssFile)), Tags(Categories.ResourceHandling)]
	public class When_requesting_a_minified_css_file : CodeFileTest
	{
		private static readonly string scriptPath = Utilities.GetStylePath("stylesheet1.css");
		private static string contents;
		private static CssFile proc;

		private Establish ctx = () =>
		{
			var tempDirectory = Util.MapPath(Configuration.Current.TemporaryDirectory);
			if (Directory.Exists(tempDirectory))
				Directory.Delete(tempDirectory, true);

			proc = new CssFile(scriptPath, "stylesheet1.css");
		};

		private Because of = () => contents = proc.Content;

		private It Should_not_be_empty = () => contents.ShouldNotBeEmpty();
		private It Should_contain_child_stylesheets = () => contents.ShouldContain("#example1");
		private It Should_contain_child_stylesheets2 = () => contents.ShouldContain("url(image1.jpg)");
	}
}
