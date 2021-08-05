//
// PInvokeTableGenerator.cs
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

//using Microsoft.Build.Framework;
//using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PInvokeTableGeneratorTest
{
	class PInvokeTableGenerator
	{
		private static char[] s_charsToReplace = new char[2] {
			'.',
			'-'
		};

		//[Required]
		public ITaskItem[] Modules {
			get;
			set;
		}

		//[Required]
		public ITaskItem[] Assemblies {
			get;
			set;
		}

		//[Required]
		public string OutputPath {
			get;
			set;
		}

		public bool Execute ()
		{
			//base.Log.LogMessage (MessageImportance.Normal, "Generating pinvoke table to '" + OutputPath + "'.");
			GenPInvokeTable (Modules.Select ((ITaskItem item) => item.ItemSpec).ToArray (), Assemblies.Select ((ITaskItem item) => item.ItemSpec).ToArray ());
			return true;
		}

		public void GenPInvokeTable (string[] pinvokeModules, string[] assemblies)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string> ();
			foreach (string text in pinvokeModules) {
				dictionary[text] = text;
			}
			List<PInvoke> pinvokes = new List<PInvoke> ();
			List<PInvokeCallback> callbacks = new List<PInvokeCallback> ();
			PathAssemblyResolver resolver = new PathAssemblyResolver (assemblies);
			MetadataLoadContext metadataLoadContext = new MetadataLoadContext (resolver, "System.Private.CoreLib");
			foreach (string assemblyPath in assemblies) {
				Assembly assembly = metadataLoadContext.LoadFromAssemblyPath (assemblyPath);

				Console.WriteLine ("CollectingPInvokes from {0}", assemblyPath);
				Type[] types = assembly.GetTypes ();
				foreach (Type type in types) {
					CollectPInvokes (pinvokes, callbacks, type);
				}
			}
			using (StreamWriter w = File.CreateText (OutputPath)) {
				//EmitPInvokeTable (w, dictionary, pinvokes);
				//EmitNativeToInterp (w, callbacks);
			}
		}

		private void CollectPInvokes (List<PInvoke> pinvokes, List<PInvokeCallback> callbacks, Type type)
		{
			MethodInfo[] methods = type.GetMethods (BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods) {
				if ((methodInfo.Attributes & MethodAttributes.PinvokeImpl) != 0) {
					CustomAttributeData customAttributeData = methodInfo.CustomAttributes.First ((CustomAttributeData attr) => attr.AttributeType.Name == "DllImportAttribute");
					string module = (string)customAttributeData.ConstructorArguments[0].Value;
					string entryPoint = (string)customAttributeData.NamedArguments.First ((CustomAttributeNamedArgument arg) => arg.MemberName == "EntryPoint").TypedValue.Value;
					pinvokes.Add (new PInvoke (entryPoint, module, methodInfo));
				}
				foreach (CustomAttributeData customAttribute in CustomAttributeData.GetCustomAttributes (methodInfo)) {
					try {
						if (customAttribute.AttributeType.FullName == "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute" || customAttribute.AttributeType.Name == "MonoPInvokeCallbackAttribute") {
							callbacks.Add (new PInvokeCallback (methodInfo));
						}
					} catch {
					}
				}
			}
		}

		/*
		private void EmitPInvokeTable (StreamWriter w, Dictionary<string, string> modules, List<PInvoke> pinvokes)
		{
			w.WriteLine ("// GENERATED FILE, DO NOT MODIFY");
			w.WriteLine ();
			HashSet<string> hashSet = new HashSet<string> ();
			foreach (PInvoke item in pinvokes.OrderBy ((PInvoke l) => l.EntryPoint)) {
				if (modules.ContainsKey (item.Module)) {
					string text = GenPInvokeDecl (item);
					if (!hashSet.Contains (text)) {
						w.WriteLine (text);
						hashSet.Add (text);
					}
				}
			}
			foreach (string module in modules.Keys) {
				string str = ModuleNameToId (module) + "_imports";
				w.WriteLine ("static PinvokeImport " + str + " [] = {");
				IEnumerable<string> enumerable = from l in pinvokes
												 where l.Module == module
												 orderby l.EntryPoint
												 select l into d
												 group d by d.EntryPoint into l
												 select "{\"" + l.Key + "\", " + l.Key + "}, // " + string.Join (", ", l.Select ((PInvoke c) => c.Method.DeclaringType.Module.Assembly.GetName ().Name).Distinct ());
				foreach (string item2 in enumerable) {
					w.WriteLine (item2);
				}
				w.WriteLine ("{NULL, NULL}");
				w.WriteLine ("};");
			}
			w.Write ("static void *pinvoke_tables[] = { ");
			foreach (string key in modules.Keys) {
				string str2 = ModuleNameToId (key) + "_imports";
				w.Write (str2 + ",");
			}
			w.WriteLine ("};");
			w.Write ("static char *pinvoke_names[] = { ");
			foreach (string key2 in modules.Keys) {
				w.Write ("\"" + key2 + "\",");
			}
			w.WriteLine ("};");
			static string ModuleNameToId (string name)
			{
				if (name.IndexOfAny (s_charsToReplace) < 0) {
					return name;
				}
				string text2 = name;
				char[] array = s_charsToReplace;
				foreach (char oldChar in array) {
					text2 = text2.Replace (oldChar, '_');
				}
				return text2;
			}
		}
		*/

		private string MapType (Type t)
		{
			switch (t.Name) {
				case "Void":
					return "void";
				case "Double":
					return "double";
				case "Single":
					return "float";
				case "Int64":
					return "int64_t";
				case "UInt64":
					return "uint64_t";
				default:
					return "int";
			}
		}

		private string GenPInvokeDecl (PInvoke pinvoke)
		{
			StringBuilder stringBuilder = new StringBuilder ();
			MethodInfo method = pinvoke.Method;
			if (method.Name == "EnumCalendarInfo") {
				stringBuilder.Append ("int " + pinvoke.EntryPoint + " (int, int, int, int, int);");
				return stringBuilder.ToString ();
			}
			stringBuilder.Append (MapType (method.ReturnType));
			stringBuilder.Append (" " + pinvoke.EntryPoint + " (");
			int num = 0;
			ParameterInfo[] parameters = method.GetParameters ();
			ParameterInfo[] array = parameters;
			foreach (ParameterInfo parameterInfo in array) {
				if (num > 0) {
					stringBuilder.Append (',');
				}
				stringBuilder.Append (MapType (parameters[num].ParameterType));
				num++;
			}
			stringBuilder.Append (");");
			return stringBuilder.ToString ();
		}

		/*
		private void EmitNativeToInterp (StreamWriter w, List<PInvokeCallback> callbacks)
		{
			int num = 0;
			w.WriteLine ("InterpFtnDesc wasm_native_to_interp_ftndescs[" + callbacks.Count + "];");
			foreach (PInvokeCallback callback in callbacks) {
				MethodInfo method = callback.Method;
				if (!(method.ReturnType.FullName == "System.Void") && !IsBlittable (method.ReturnType)) {
					Error ($"The return type '{method.ReturnType.FullName}' of pinvoke callback method '{method}' needs to be blittable.");
				}
				ParameterInfo[] parameters = method.GetParameters ();
				foreach (ParameterInfo parameterInfo in parameters) {
					if (!IsBlittable (parameterInfo.ParameterType)) {
						Error ("Parameter types of pinvoke callback method '" + method?.ToString () + "' needs to be blittable.");
					}
				}
			}
			HashSet<string> hashSet = new HashSet<string> ();
			foreach (PInvokeCallback callback2 in callbacks) {
				StringBuilder stringBuilder = new StringBuilder ();
				MethodInfo method2 = callback2.Method;
				stringBuilder.Append ("typedef void ");
				stringBuilder.Append ($" (*WasmInterpEntrySig_{num}) (");
				int num2 = 0;
				if (method2.ReturnType.Name != "Void") {
					stringBuilder.Append ("int");
					num2++;
				}
				ParameterInfo[] parameters2 = method2.GetParameters ();
				foreach (ParameterInfo parameterInfo2 in parameters2) {
					if (num2 > 0) {
						stringBuilder.Append (',');
					}
					stringBuilder.Append ("int*");
					num2++;
				}
				if (num2 > 0) {
					stringBuilder.Append (',');
				}
				stringBuilder.Append ("int*");
				stringBuilder.Append (");\n");
				bool flag = method2.ReturnType.Name == "Void";
				string text = method2.DeclaringType.Module.Assembly.GetName ().Name.Replace (".", "_");
				uint metadataToken = (uint)method2.MetadataToken;
				string name = method2.DeclaringType.Name;
				string name2 = method2.Name;
				string text2 = "wasm_native_to_interp_" + text + "_" + name + "_" + name2;
				if (hashSet.Contains (text2)) {
					Error ("Two callbacks with the same name '" + name2 + "' are not supported.");
				}
				hashSet.Add (text2);
				callback2.EntryName = text2;
				stringBuilder.Append (MapType (method2.ReturnType));
				stringBuilder.Append (" " + text2 + " (");
				num2 = 0;
				ParameterInfo[] parameters3 = method2.GetParameters ();
				foreach (ParameterInfo parameterInfo3 in parameters3) {
					if (num2 > 0) {
						stringBuilder.Append (',');
					}
					stringBuilder.Append (MapType (method2.GetParameters ()[num2].ParameterType));
					stringBuilder.Append ($" arg{num2}");
					num2++;
				}
				stringBuilder.Append (") { \n");
				if (!flag) {
					stringBuilder.Append (MapType (method2.ReturnType) + " res;\n");
				}
				stringBuilder.Append ($"((WasmInterpEntrySig_{num})wasm_native_to_interp_ftndescs [{num}].func) (");
				num2 = 0;
				if (!flag) {
					stringBuilder.Append ("&res");
					num2++;
				}
				int num3 = 0;
				ParameterInfo[] parameters4 = method2.GetParameters ();
				foreach (ParameterInfo parameterInfo4 in parameters4) {
					if (num2 > 0) {
						stringBuilder.Append (", ");
					}
					stringBuilder.Append ($"&arg{num3}");
					num2++;
					num3++;
				}
				if (num2 > 0) {
					stringBuilder.Append (", ");
				}
				stringBuilder.Append ($"wasm_native_to_interp_ftndescs [{num}].arg");
				stringBuilder.Append (");\n");
				if (!flag) {
					stringBuilder.Append ("return res;\n");
				}
				stringBuilder.Append ('}');
				w.WriteLine (stringBuilder);
				num++;
			}
			w.Write ("static void *wasm_native_to_interp_funcs[] = { ");
			foreach (PInvokeCallback callback3 in callbacks) {
				w.Write (callback3.EntryName + ",");
			}
			w.WriteLine ("};");
			w.Write ("static const char *wasm_native_to_interp_map[] = { ");
			foreach (PInvokeCallback callback4 in callbacks) {
				MethodInfo method3 = callback4.Method;
				string text3 = method3.DeclaringType.Module.Assembly.GetName ().Name.Replace (".", "_");
				string name3 = method3.DeclaringType.Name;
				string name4 = method3.Name;
				w.WriteLine ("\"" + text3 + "_" + name3 + "_" + name4 + "\",");
			}
			w.WriteLine ("};");
		}
		*/

		private static bool IsBlittable (Type type)
		{
			if (type.IsPrimitive || type.IsByRef || type.IsPointer) {
				return true;
			}
			return false;
		}

		private static void Error (string msg)
		{
			throw new Exception (msg);
		}
	}

}
