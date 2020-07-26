using System;
using System.Collections.Generic;
using System.Xml.Linq;

// ReSharper disable VirtualMemberCallInConstructor

namespace IFSBrowser.Handlers {

	public class Node : IDisposable {
		private static readonly Dictionary<string, string> Escapes = new Dictionary<string, string> {
			{"_E", "."},
			{"__", "_"}
		};

		public readonly IFS.FileBlob IFSData;
		public readonly XDocument Manifest;
		public readonly Node Parent;
		protected internal readonly string Path;

		protected Node(IFS.FileBlob ifsData, XDocument manifest, Node parent = null, string path = "", string name = "") {
			IFSData = ifsData;
			Manifest = manifest;
			Parent = parent;
			Path = path;
			Name = name;
		}

		public string Name { get; protected internal set; }

		public virtual void Dispose() { }

		protected static string FixName(string name) {
			foreach (var (key, value) in Escapes) name = name.Replace(key, value);

			if (name[0] == '_' && char.IsDigit(name[1])) name = name[1..];

			return name;
		}
	}

}