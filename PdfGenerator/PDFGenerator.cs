using ImageProcessor;
using ImageProcessor.Imaging.Formats;
using PdfGenerator.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Telerik.Documents.Core;
using Telerik.Documents.Core.Fonts;
using Telerik.Documents.Primitives;
using Telerik.Windows.Documents.Core.Fonts;
using Telerik.Windows.Documents.Extensibility;
using Telerik.Windows.Documents.Fixed.FormatProviders.Pdf;
using Telerik.Windows.Documents.Fixed.Model;
using Telerik.Windows.Documents.Fixed.Model.ColorSpaces;
using Telerik.Windows.Documents.Fixed.Model.Editing;
using Telerik.Windows.Documents.Fixed.Model.Editing.Flow;
using Telerik.Windows.Documents.Fixed.Model.Editing.Tables;
using Telerik.Windows.Documents.Fixed.Model.Fonts;
using Telerik.Windows.Documents.Fixed.Model.Text;

namespace PdfGenerator
{
    public class FontsProvider : FontsProviderBase
    {
        public override byte[] GetFontData(FontProperties fontProperties)
        {
            string fontFileName = fontProperties.FontFamilyName + ".ttf";
            string fontFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            string targetPath = Path.Combine(fontFolder, fontFileName);

            DirectoryInfo directory = new DirectoryInfo(fontFolder);
            FileInfo[] fontFiles = directory.GetFiles("*.ttf");
            if (fontFiles.Any(s => s.Name.ToLower() == fontFileName.ToLower()))
            {
                using (FileStream fileStream = File.OpenRead(targetPath))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        fileStream.CopyTo(memoryStream);
                        return memoryStream.ToArray();
                    }
                }
            }

            return null;
        }
    }
    public class PDFGenerator
    {

        public static void GenerateImageReport(Case Case)
        {
            var fArial = new FontFamily("Arial");
            var border = new Border(2, RgbColors.Black);
            var lightBorder = new Border(1, RgbColors.Black);
            //FontBase fArial16 = new FontBase();
            //Set custom font provider
            FontsProviderBase fontsProvider = new FontsProvider();
            FixedExtensibilityManager.FontsProvider = fontsProvider;
            //Regisitering fonts
            FontsRepository.RegisterFont(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, fontsProvider.GetFontData(new FontProperties(fArial,FontStyles.Normal,FontWeights.Bold)));
            RadFixedDocument doc = new RadFixedDocument();
            RadFixedDocumentEditor editor = new RadFixedDocumentEditor(doc);
            //Set page orientation landscape
            editor.SectionProperties.PageSize = new Size(editor.SectionProperties.PageSize.Height, editor.SectionProperties.PageSize.Width);
            TimeZoneInfo CurrentTimeZone = TimeZoneInfo.FindSystemTimeZoneById(Case.Client.TimeZoneID);
            string CurrentTime = DateTimeOffset.UtcNow.ToOffset(CurrentTimeZone.GetUtcOffset(DateTimeOffset.UtcNow)).ToString("ddd MMM d yyyy h:mm:ss tt zzz");
            int i = 0;
            Table table = new Table();
            foreach (var card in Case.Cards)
            {
                Block block;
                TableRow row;
                TableCell cell;
                if (i % 4 == 0)
                {
                    if (i != 0)
                    {
                        #region Page header
                        block = new Block();
                        //block.TextProperties.Font = new F;
                        
                        block.HorizontalAlignment = HorizontalAlignment.Center;
                        
                        block.TextProperties.FontSize = 14f;
                        block.TextProperties.TrySetFont(fArial, FontStyles.Normal, FontWeights.Bold);
                        var textFragment = new TextFragment("Test");
                        textFragment.FontSize = 14;
                        bool success = FontsRepository.TryCreateFont(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Heavy, out FontBase font);

                        textFragment.Font = font;
                        block.Insert(textFragment);
                        block.SaveTextProperties();
                        if (Case.ClientGroup != null)
                        {
                            block.InsertText(Case.ClientGroup.Name);
                            block.InsertLineBreak();
                        }

                        block.InsertText(Case.Client.Name);
                        block.InsertLineBreak();

                        block.InsertText((Case.Client.Address1 + " " + Case.Client.Address2).Trim());
                        block.InsertLineBreak();

                        block.InsertText(Case.Client.City + ", " + Case.Client.State + ", " + Case.Client.Zipcode);
                        block.InsertLineBreak();

                        editor.InsertBlock(block);

                        block = new Block();
                        block.HorizontalAlignment = HorizontalAlignment.Center;
                        
                        
                        block.TextProperties.Font = font;
                        //block.TextProperties.TrySetFont(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Heavy);
                        block.TextProperties.FontSize = 16f;
                        block.InsertText("Card Image Report");
                        editor.InsertBlock(block);

                        block = new Block();
                        block.InsertText($"Case #: {card.Case.CaseNumber}  Total Cards: {ReportHelpers.GetCardCount(Case.Cards)}");
                        block.InsertLineBreak();
                        block.InsertText(CurrentTime);
                        block.InsertLineBreak();
                        editor.InsertBlock(block);
                        #endregion
                        var section = editor.SectionProperties;
                        
                        editor.InsertTable(table);
                        editor.InsertPageBreak();
                    }
                    //Start new page
                    //Set new table header and other things
                    table = new Table();
                    table.LayoutType = TableLayoutType.FixedWidth;
                    table.Margin = new Thickness { Left = 0, Right = 0 };
                    row = table.Rows.AddTableRow();

                    block = new Block();
                    block.InsertText(new FontFamily("Courier"), FontStyles.Normal, FontWeights.Heavy, "Images");

                    cell = row.Cells.AddTableCell();
                    cell.Padding = new Thickness(2);
                    cell.Blocks.Add(block);
                    cell.Borders = new TableCellBorders(border, border, border, border);

                    block = new Block();
                    block.InsertText("Printed Details");
                    cell = row.Cells.AddTableCell();
                    cell.Padding = new Thickness(2);
                    cell.Blocks.Add(block);
                    cell.Borders = new TableCellBorders(border, border, border, border);

                    block = new Block();
                    block.InsertText("Magstripe Details");
                    cell = row.Cells.AddTableCell();
                    cell.Padding = new Thickness(2);
                    cell.Blocks.Add(block);
                    cell.Borders = new TableCellBorders(border, border, border, border);
                }

                #region Add Row
                row = table.Rows.AddTableRow();
                cell = row.Cells.AddTableCell();

                var imageTable = new Table();
                var imageRow = imageTable.Rows.AddTableRow();

                foreach (var image in card.CardImages)
                {
                    var imageCell = imageRow.Cells.AddTableCell();
                    block = new Block();
                    block.SpacingBefore = 3f;
                    block.SpacingAfter = 3f;

                    block.InsertImage(GetCaseImage(image.ImageData), 160f, 120f);
                    imageCell.Padding = new Telerik.Documents.Primitives.Thickness(5);
                    imageCell.Blocks.Add(block);
                }

                cell.Borders = new TableCellBorders(lightBorder, lightBorder, lightBorder, lightBorder);
                cell.Padding = new Telerik.Documents.Primitives.Thickness(6);
                imageTable.LayoutType = TableLayoutType.AutoFit;
                cell.Blocks.Add(imageTable);

                cell = row.Cells.AddTableCell();
                block = new Block();
                block.HorizontalAlignment = HorizontalAlignment.Left;
                block.VerticalAlignment = VerticalAlignment.Center;
                InsertDetails(block, card);
                cell.Borders = new TableCellBorders(lightBorder, lightBorder, lightBorder, lightBorder);
                cell.Padding = new Thickness(5);
                cell.Blocks.Add(block);

                cell = row.Cells.AddTableCell();
                block = new Block();
                block.HorizontalAlignment = HorizontalAlignment.Left;

                if (card.ClonedCardParent != null)
                {
                    if (card.ClonedCardParent.TrackData != null)
                    {
                        string cardTrackUnenc = card.ClonedCardParent.TrackData.Trim();
                        CCTrackParser trackParser = new CCTrackParser();
                        var trackData = trackParser.Parse(cardTrackUnenc);

                        InsertMagstripDetails(block, trackData);
                    }
                    else
                    {
                        InsertDetails(block, card.ClonedCardParent);
                    }
                }
                else if (card.EntryMethod == "Swipe" || !card.MissingPrintedInfo)
                {
                    if (card.TrackData != null)
                    {
                        string cardTrackUnenc = card.TrackData.Trim();
                        CCTrackParser trackParser = new CCTrackParser();
                        var trackData = trackParser.Parse(cardTrackUnenc);

                        InsertMagstripDetails(block, trackData);
                    }
                    else
                    {
                        InsertDetails(block, card);
                    }
                }

                cell.Borders = new TableCellBorders(lightBorder, lightBorder, lightBorder, lightBorder);
                cell.Padding = new Thickness(5);
                cell.Blocks.Add(block);
                #endregion
                i++;
            }

            PdfFormatProvider provider = new PdfFormatProvider();
            
            using (var stream = new FileStream(@"outPDF.pdf", FileMode.OpenOrCreate))
            {
                provider.Export(doc, stream);
            }
        }

        private static void InsertDetails(Block block, Card card)
        {
            block.InsertText("Number: ", card.CardNumberMasked);
            block.InsertText("Name: ", card.CardName);
            block.InsertText("Exp: ", card.CardExpiration);
            block.InsertText("Issuer: ", card.IssuerName);
        }

        private static void InsertMagstripDetails(Block block, FullTrackDataModel trackData)
        {
            block.InsertText("T1 Number: ", trackData?.TrackOne?.MaskedPAN);
            block.InsertText("T1 Name: ", trackData?.TrackOne?.CardHolderName);
            block.InsertText("T1 Exp: ", trackData?.TrackOne?.ExpirationDate);
            //var bin1 = binLookup.LookupCardMetaData(card.BinBase);
            //block.InsertText("T1 Issuer: ",bin1.);
            block.InsertLineBreak();
            //
            block.InsertText("T2 Number: ", trackData?.TrackTwo.MaskedPAN);
            block.InsertText("T2 Name: ", "");
            block.InsertText("T2 Exp: ", trackData?.TrackTwo?.ExpirationDate);
            //var bin1 = binLookup.LookupCardMetaData(card.BinBase);
            //block.InsertText("T1 Issuer: ",bin1.);
            //
        }

        static Stream GetCaseImage(string imageString)
        {
            imageString = FormatBase64String(imageString);
            byte[] bytes = Convert.FromBase64String(imageString);
            MemoryStream outStream = new MemoryStream();
            using (MemoryStream inStream = new MemoryStream(bytes))
            {
                // Initialize the ImageFactory using the overload to preserve EXIF metadata.
                using (ImageFactory imageFactory = new ImageFactory(preserveExifData: true))
                {
                    // Load, resize, set the format and quality and save an image.
                    imageFactory.Load(inStream)
                                .Format(new JpegFormat { Quality = 70 })
                                .Save(outStream);
                }
            }
            return outStream;
        }

        private static string FormatBase64String(string base64)
        {
            try
            {
                if (base64.Length > 0)
                {
                    StringBuilder sb = new StringBuilder(base64.Length + 5);
                    sb.Append(base64);
                    if (base64.Contains("%"))
                    {
                        sb = sb.Replace("%", "=");
                    }
                    var padding = base64.Length % 4;
                    if (padding == 3)
                    {
                        sb.Append("=");
                    }
                    else if (padding == 2)
                    {
                        sb.Append("==");
                    }
                    return sb.ToString();
                }
            }
            catch (Exception e)
            {
                return "";
            }
            return "";
        }
    }
}
