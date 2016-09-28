using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Web.Mvc.Razor;
using System.Web.Razor;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser.SyntaxTree;

namespace RazorPad.Compilation
{
    public class ViewComponentCSharpRazorCodeGenerator : MvcCSharpRazorCodeGenerator
    {
        [ImportMany(typeof(CSharpRazorCodeGeneratorSpanVisitor))]
        public IEnumerable<CSharpRazorCodeGeneratorSpanVisitor> Vistors { get; set; }


        public ViewComponentCSharpRazorCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host) 
            : base(className, rootNamespaceName, sourceFileName, host)
        {
        }

        protected  bool TryVisitSpecialSpan(Span span)
        {
            var vistors = (Vistors ?? Enumerable.Empty<CSharpRazorCodeGeneratorSpanVisitor>());

            return  vistors.FirstOrDefault(x => x.TryVisit(span)) != null;
        }
    }

    public class MvcCSharpRazorCodeGenerator : CSharpRazorCodeGenerator
    {
        private const string DefaultModelTypeName = "dynamic";

        public MvcCSharpRazorCodeGenerator(string className, string rootNamespaceName, string sourceFileName, RazorEngineHost host)
            : base(className, rootNamespaceName, sourceFileName, host)
        {
            var mvcHost = host as MvcWebPageRazorHost;
            if (mvcHost != null && !mvcHost.IsSpecialPage)
            {
                // set the default model type to "dynamic" (Dev10 bug 935656)
                // don't set it for "special" pages (such as "_viewStart.cshtml")
                SetBaseType(DefaultModelTypeName);
            }
        }

        private void SetBaseType(string modelTypeName)
        {
            var baseType = new CodeTypeReference(Context.Host.DefaultBaseClass + "<" + modelTypeName + ">");
            Context.GeneratedClass.BaseTypes.Clear();
            Context.GeneratedClass.BaseTypes.Add(baseType);
        }
    }
}
