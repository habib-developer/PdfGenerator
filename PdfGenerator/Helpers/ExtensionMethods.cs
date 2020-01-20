using System;
using System.Collections.Generic;
using System.Text;
using Telerik.Documents.Core.Fonts;
using Telerik.Windows.Documents.Fixed.Model.Editing;

namespace PdfGenerator.Helpers
{
    public static class ExtensionMethods
    {
        public static void InsertText(this Block block, string label, string text)
        {
            block.InsertText(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, label);
            block.InsertText(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, text??"");
            block.InsertLineBreak();
        }
    }
}
