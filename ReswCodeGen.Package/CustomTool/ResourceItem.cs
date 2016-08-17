namespace ReswCodeGen.CustomTool
{
	public class ResourceItem
	{
		public ResourceItem(string name, string value, string comment)
		{
			Name = name;
			Value = value;
			Comment = comment;
		}

		public string Name { get; }
		public string Value { get; }
		public string Comment { get; }
	}
}