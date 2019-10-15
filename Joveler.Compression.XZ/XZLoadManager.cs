using Joveler.DynLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joveler.Compression.XZ
{
    internal class XZLoadManager : LoadManagerBase<XZLoader>
    {
        protected override string ErrorMsgInitFirst => "Please call XZInit.GlobalInit() first!";
        protected override string ErrorMsgAlreadyLoaded => "Joveler.Compression.XZ is already initialized.";

        protected override XZLoader CreateLoader() => new XZLoader();

        protected override XZLoader CreateLoader(string libPath) => new XZLoader(libPath);
    }
}
