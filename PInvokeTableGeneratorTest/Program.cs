//
// Program.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2021 Microsoft Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PInvokeTableGeneratorTest
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			try {
				Run ();
				Console.WriteLine ("Done");
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		static void Run ()
		{
			var generator = new PInvokeTableGenerator ();
			generator.Modules = new TaskItem [0];
			generator.OutputPath = Path.GetFullPath ("table.h");

			string rootDirectory = Path.GetDirectoryName (typeof (MainClass).Assembly.Location);
			rootDirectory = Path.GetFullPath (Path.Combine (rootDirectory, "..", "assemblies"));

			generator.Assemblies = GetAssemblyFileNames ()
				.Select (fileName => Path.Combine (rootDirectory, fileName))
				.Select (fileName => new TaskItem (fileName))
				.ToArray ();

			generator.Execute ();
		}

		static IEnumerable<string> GetAssemblyFileNames ()
		{
			yield return "Microsoft.AspNetCore.Components.dll";
			yield return "System.Net.Http.dll";
			yield return "System.Runtime.dll";
			yield return "System.Runtime.CompilerServices.Unsafe.dll";
			yield return "System.Private.Uri.dll";
			yield return "Microsoft.Extensions.DependencyInjection.dll";
			yield return "Microsoft.JSInterop.dll";
			yield return "Microsoft.AspNetCore.Components.WebAssembly.dll";
			yield return "Microsoft.AspNetCore.Components.Web.dll";
			yield return "System.Private.CoreLib.dll";
			yield return "System.Text.Json.dll";
			yield return "System.Text.Encodings.Web.dll";
			yield return "DebugBlazorApp.Client.dll";
			yield return "DebugBlazorApp.Shared.dll";
			yield return "Microsoft.Extensions.Primitives.dll";
			yield return "Microsoft.Extensions.Options.dll";
			yield return "System.Private.Runtime.InteropServices.JavaScript.dll";
			yield return "System.Net.Primitives.dll";
			yield return "Microsoft.Extensions.Logging.Abstractions.dll";
			yield return "System.ComponentModel.dll";
			yield return "System.Collections.dll";
			yield return "System.Collections.Concurrent.dll";
			yield return "Microsoft.Extensions.Logging.dll";
			yield return "Microsoft.Extensions.DependencyInjection.Abstractions.dll";
			yield return "Microsoft.Extensions.Configuration.Json.dll";
			yield return "Microsoft.Extensions.Configuration.Abstractions.dll";
			yield return "Microsoft.Extensions.Configuration.dll";
			yield return "System.Net.Http.Json.dll";
			yield return "System.Memory.dll";
			yield return "System.Linq.dll";
		}
	}
}
