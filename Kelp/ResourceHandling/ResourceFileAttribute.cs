namespace Kelp.ResourceHandling
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Class CodeFileAttribute
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class ResourceFileAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ResourceFileAttribute"/> class.
		/// </summary>
		/// <param name="resourceType">The <see cref="ResourceType"/> of the code file.</param>
		/// <param name="contentType">The content type of this code file.</param>
		/// <param name="extensions">The extensions of this code file.</param>
		public ResourceFileAttribute(ResourceType resourceType, string contentType, params string[] extensions)
		{
			this.ResourceType = resourceType;
			this.ContentType = contentType;
			this.Extensions = extensions;
		}

		/// <summary>
		/// Gets the <see cref="ResourceType"/> associated with this <see cref="CodeFile"/>.
		/// </summary>
		public ResourceType ResourceType { get; private set; }

		/// <summary>
		/// Gets the content-type associated with this <see cref="CodeFile"/>.
		/// </summary>
		public string ContentType { get; private set; }

		/// <summary>
		/// Gets the file extensions associated with this <see cref="CodeFile"/>.
		/// </summary>
		public IEnumerable<string> Extensions { get; private set; }
	}
}
