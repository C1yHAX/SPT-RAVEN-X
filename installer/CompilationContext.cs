using System.IO.Compression;

namespace Installer;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

internal class CompilationContext(Installation installation, string projectTitle, string project)
{
	internal const string DefaultBranch = "master";

	public int Try { get; set; } = 0;
	public Installation Installation { get; set; } = installation;
	public string ProjectTitle { get; set; } = projectTitle;
	public string Project { get; set; } = project;
	public string Branch { get; set; } = DefaultBranch;
	public string[] Exclude { get; set; } = [];
	public string[] Defines { get; set; } = [];
	public Dictionary<string, MetadataReference> ProjectReferences { get; } = new(System.StringComparer.OrdinalIgnoreCase);
	public ZipArchive? Archive { get; set; }
	public bool IsFatalFailure { get; set; } = false;
	private string _language = "";
	public string Language
	{
		get => _language;
		set
		{
			var language = value?.Trim() ?? "";
			_language = language.Equals("jp", System.StringComparison.OrdinalIgnoreCase)
				|| language.Equals("ja-jp", System.StringComparison.OrdinalIgnoreCase)
				? "ja"
				: language;
		}
	}
}
