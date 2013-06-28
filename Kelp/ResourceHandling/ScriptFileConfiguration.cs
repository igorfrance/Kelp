﻿/**
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
	using System.Collections.Specialized;
	using System.Xml;

	using Microsoft.Ajax.Utilities;

	/// <summary>
	/// Represents the processing configuration for script files.
	/// </summary>
	public class ScriptFileConfiguration : FileTypeConfiguration
	{
		private readonly List<string> byteProps = new List<string> { "IndentSize" };

		private readonly List<string> boolProps = new List<string>
		{
			"MinifyCode",
			"CollapseToLiteral",
			"EvalLiteralExpressions",
			"MacSafariQuirks",
			"ManualRenamesProperties",
			"PreserveFunctionNames",
			"RemoveFunctionExpressionNames",
			"RemoveUnneededCode",
			"StripDebugStatements",
			"InlineSafeStrings",
			"StrictMode",
		};

		private readonly List<string> enumProps = new List<string>
		{
			"EvalTreatment",
			"LocalRenaming",
			"OutputMode",
		};

		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptFileConfiguration" /> class.
		/// </summary>
		public ScriptFileConfiguration()
		{
			this.Settings = new CodeSettings
			{
				MinifyCode = false,
				OutputMode = OutputMode.SingleLine
			};
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptFileConfiguration" /> class,
		/// using the specified <paramref name="configurationElement"/>
		/// </summary>
		/// <param name="configurationElement">The configuration element.</param>
		public ScriptFileConfiguration(XmlElement configurationElement)
			: this()
		{
			this.Parse(configurationElement, typeof(CodeSettings), this.Settings);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptFileConfiguration" /> class,
		/// using the specified <paramref name="configuration"/> collection.
		/// </summary>
		/// <param name="configuration">The collection that contains the configuration settings.</param>
		public ScriptFileConfiguration(NameValueCollection configuration)
			: this()
		{
			this.Parse(configuration, typeof(CodeSettings), this.Settings);
		}

		/// <summary>
		/// Gets the <see cref="CodeSettings"/> associated with this instance.
		/// </summary>
		public CodeSettings Settings { get; internal set; }

		/// <inheritdoc/>
		protected override List<string> BoolProps
		{
			get { return boolProps; }
		}

		/// <inheritdoc/>
		protected override List<string> ByteProps
		{
			get { return byteProps; }
		}

		/// <inheritdoc/>
		protected override List<string> EnumProps
		{
			get { return enumProps; }
		}

		/// <inheritdoc/>
		public override bool MinificationEnabled
		{
			get
			{
				return this.Settings.MinifyCode;
			}

			set
			{
				this.Settings.MinifyCode = value;
			}
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.Serialize(typeof(CodeSettings), this.Settings);
		}
	}
}
