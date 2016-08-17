using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;

namespace ReswCodeGen.CustomTool
{
    [Guid("6C6AC14F-9B11-47C1-BC90-DFBFB89B1CB8")]
    [ComVisible(true)]
    public class ReswFileVisualBasicCodeGeneratorInternal : ReswFileCodeGenerator
    {
        public ReswFileVisualBasicCodeGeneratorInternal()
            : base(new VBCodeProvider(), TypeAttributes.NestedAssembly)
        {
        }

        public override int DefaultExtension(out string defaultExtension)
        {
            defaultExtension = ".vb";
            return 0;
        }
    }
}