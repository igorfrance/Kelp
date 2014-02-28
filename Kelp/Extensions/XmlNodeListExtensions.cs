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
namespace Kelp.Extensions
{
	using System;
	using System.Collections.Generic;
	using System.Xml;

	/// <summary>
	/// Provides extensions to <see cref="XmlNodeList"/>.
	/// </summary>
	public static class XmlNodeListExtensions
	{
		/// <summary>
		/// Converts this instance to an enumerable list.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <returns>List{XmlNode}.</returns>
		public static List<XmlNode> ToList(this XmlNodeList instance)
		{
			var result = new List<XmlNode>();
			foreach (XmlNode node in instance)
			{
				result.Add(node);
			}

			return result;
		}
	}
}
