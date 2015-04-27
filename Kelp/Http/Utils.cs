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
namespace Kelp.Http
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.Contracts;
	using System.IO;
	using System.Net;
	using System.Reflection;
	using System.Security.Cryptography;
	using System.Text;
	using System.Web;

	/// <summary>
	/// Provides several common HTTP related utility methods.
	/// </summary>
	public static class Util
	{
		/// <summary>
		/// Specifies the maximum time difference (in seconds) between the current and the cached files that
		/// will still be allowed for the files to still be considered equal.
		/// </summary>
		public const int MaxDifferenceCachedDate = 2;

		private static readonly object temp = new object();
		private static MethodInfo getMimeMapping;

		private static MethodInfo GetMimeMapping
		{
			get
			{
				if (getMimeMapping == null)
				{
					Type mimeMappingType = Assembly.GetAssembly(typeof(HttpRuntime)).GetType("System.Web.MimeMapping");
					if (mimeMappingType == null)
						throw new SystemException("Couldnt find MimeMapping type");

					getMimeMapping = mimeMappingType.GetMethod("GetMimeMapping", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
					if (getMimeMapping == null)
						throw new SystemException("Couldnt find GetMimeMapping method");
					if (getMimeMapping.ReturnType != typeof(string))
						throw new SystemException("GetMimeMapping method has invalid return type");
					if (getMimeMapping.GetParameters().Length != 1 && getMimeMapping.GetParameters()[0].ParameterType != typeof(string))
						throw new SystemException("GetMimeMapping method has invalid parameters");
				}

				return getMimeMapping;
			}
		}

		/// <summary>
		/// Determines whether the specified <paramref name="context"/> represents a no-cache request.
		/// </summary>
		/// <param name="context">The context to check.</param>
		/// <returns>
		/// <c>true</c> if the specified <paramref name="context"/> represents a no-cache request; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsNoCacheRequest(HttpContextBase context)
		{
			return context.Request.Headers["Cache-Control"] == "no-cache";
		}

		/// <summary>
		/// Determines whether the specified <paramref name="context"/> represents a cached request.
		/// </summary>
		/// <param name="context">The context to check.</param>
		/// <returns>
		/// <c>true</c> if the specified <paramref name="context"/> represents a cached request; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsCachedRequest(HttpContextBase context)
		{
			return context.Request.Headers["If-Modified-Since"] != null;
		}

		/// <summary>
		/// Determines whether the specified <paramref name="lastModified"/> date is greater than the last modified
		/// date associated with the request.
		/// </summary>
		/// <param name="context">The context to check.</param>
		/// <param name="lastModified">The last modification date to test with.</param>
		/// <returns>
		///   <c>true</c> if the specified <paramref name="lastModified"/> date is greater than the last modified
		/// date associated with the request; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsFileUpdatedSinceCached(HttpContextBase context, DateTime lastModified)
		{
			DateTime cachedDate = DateTime.Parse(context.Request.Headers["If-Modified-Since"]);

			TimeSpan elapsed = lastModified - cachedDate;
			return elapsed.TotalSeconds >= MaxDifferenceCachedDate;
		}

		/// <summary>
		/// Sends the not-modified status header to the response.
		/// </summary>
		/// <param name="context">The HTTP context that contains the request.</param>
		public static void SendNotModified(HttpContextBase context)
		{
			context.Response.SuppressContent = true;
			context.Response.StatusCode = (int) HttpStatusCode.NotModified;
			context.Response.StatusDescription = "Not Modified";
			context.Response.AddHeader("Content-Length", "0");
		}

		/// <summary>
		/// Sets the response status code to 404 for the specified <paramref name="context"/>.
		/// </summary>
		/// <param name="context">The HTTP context that contains the request.</param>
		public static void SendFileNotFound(HttpContextBase context)
		{
			context.Response.StatusCode = 404;
		}

		/// <summary>
		/// Gets the mime type for the specified <paramref name="filename"/>.
		/// </summary>
		/// <param name="filename">The filename whose mime-type to get.</param>
		/// <returns>The mime type for the specified <paramref name="filename"/></returns>
		public static string GetMimeType(string filename)
		{
			return (string) GetMimeMapping.Invoke(temp, new object[] { filename });
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
	}
}
