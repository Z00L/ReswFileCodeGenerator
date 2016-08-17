using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ReswCodeGen.CustomTool
{
	public class ResourceParser : IResourceParser
	{
		public ResourceParser(string reswFileContents)
		{
			ReswFileContents = reswFileContents;
		}

		public string ReswFileContents { get; set; }

		public IEnumerable<ResourceItem> Parse()
		{
			var doc = XDocument.Parse(ReswFileContents);

			foreach (var element in doc.Descendants("data"))
			{
				//if (element.Attributes().All(x => x.Name != "name"))
				//	continue;

				var nameAttribute = element.Attribute("name");
				var name = nameAttribute?.Value;
				if (string.IsNullOrWhiteSpace(name))
					continue;

				//string value;
				//if (element.Descendants().Any(x => x.Name == "value"))
				//{
				var valueElement = element.Descendants("value").FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Value));
				var value = valueElement?.Value;
				//}

				//string comment;
				//if (element.Descendants().Any(x => x.Name == "comment"))
				//{
				var commentElement = element.Descendants("comment").FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Value));
				var comment = commentElement?.Value;
				//}

				yield return new ResourceItem(name, value, comment);
			}
		}
	}
}