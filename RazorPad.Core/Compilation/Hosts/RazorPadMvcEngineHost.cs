﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Razor;

namespace RazorPad.Compilation.Hosts
{
    public class RazorPadMvcEngineHost : System.Web.Mvc.Razor.MvcWebPageRazorHost
    {
        public RazorPadMvcEngineHost(RazorCodeLanguage language = null)
            : base("~/WebPage.cshtml", null)
        {
            // ReSharper disable DoNotCallOverridableMethodsInConstructor
            DefaultClassName = "WebPage";
            DefaultNamespace = "RazorPad";
            CodeLanguage = language ?? CodeLanguage;
            // ReSharper restore DoNotCallOverridableMethodsInConstructor
        }

        [Obsolete("This method is obsolete, use the override which takes a CodeGeneratorContext instead")]
        public override void PostProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            base.PostProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);

            AddGlobalApplicationClassToCompiledPage(codeCompileUnit);

            AddAssemblyReferences(codeCompileUnit);
        }

        protected virtual void AddAssemblyReferences(CodeCompileUnit codeCompileUnit)
        {
            IEnumerable<string> referencedAssemblies =
                new[] { // .NET Framework Assemblies (by name)
                    "System.dll", 
                    "System.Core.dll", 
                    "System.Net.dll", 
                    "System.Web.dll", 
                    "Microsoft.CSharp.dll", // For dynamic stuff
                }
                // "Third party" assemblies (by location)
                .Union(new[] {
                    Assembly.ReflectionOnlyLoad("System.Web.Mvc, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31BF3856AD364E35"),
                    Assembly.ReflectionOnlyLoad("System.Web.WebPages, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31BF3856AD364E35"),
                }.Select(x => x.Location));

            codeCompileUnit.ReferencedAssemblies.AddRange(referencedAssemblies.ToArray());
        }

        public static void AddGlobalApplicationClassToCompiledPage(CodeCompileUnit codeCompileUnit)
        {
            var ASPNamespace = codeCompileUnit.Namespaces.Cast<CodeNamespace>().FirstOrDefault(x => x.Name == "ASP");

            if (ASPNamespace == null)
            {
                ASPNamespace = new CodeNamespace("ASP");
                codeCompileUnit.Namespaces.Add(ASPNamespace);
            }

            var globalApplicationClass = new CodeTypeDeclaration("global_asax")
            {
                IsClass = true,
                TypeAttributes = TypeAttributes.Public
            };
            globalApplicationClass.BaseTypes.Add(new CodeTypeReference("System.Web.HttpApplication"));

            ASPNamespace.Types.Add(globalApplicationClass);
        }

    }
}
