using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CSharp;

namespace ReswCodeGen.CustomTool
{
    [Guid("151F74CA-404D-4188-B994-D7683C32ACF4")]
    [ComVisible(true)]
    public class ReswFileCSharpCodeGeneratorInternal : ReswFileCodeGenerator
    {
        public ReswFileCSharpCodeGeneratorInternal()
            : base(new CSharpCodeProvider(), TypeAttributes.NestedAssembly)
        {
        }

        public override int DefaultExtension(out string defaultExtension)
        {
            defaultExtension = ".cs";
            return 0;
        }
    }
}