using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Telerik.Documents.Core.Fonts;
using Telerik.Documents.Primitives;
using Telerik.Windows.Documents.Fixed.FormatProviders.Pdf;
using Telerik.Windows.Documents.Fixed.Model;
using Telerik.Windows.Documents.Fixed.Model.ColorSpaces;
using Telerik.Windows.Documents.Fixed.Model.Editing;
using Telerik.Windows.Documents.Fixed.Model.Editing.Flow;
using Telerik.Windows.Documents.Fixed.Model.Editing.Tables;
using Telerik.Windows.Documents.Fixed.Model.Fonts;

namespace PdfGenerator
{
    class IssuerReportGenerator
    {
        public static void GenerateIssuerReport(Case Case, List<Card> Cards)
        {
            RadFixedDocument doc = new RadFixedDocument();
            RadFixedDocumentEditor editor = new RadFixedDocumentEditor(doc);
            if (Cards.Count > 0)
            {
                TimeZoneInfo CurrentTimeZone = TimeZoneInfo.FindSystemTimeZoneById(Case.Client.TimeZoneID);
                string CurrentTime = DateTimeOffset.UtcNow.ToOffset(CurrentTimeZone.GetUtcOffset(DateTimeOffset.UtcNow)).ToString("ddd MMM d yyyy h:mm:ss tt zzz");
                string strTemp = string.Empty;

                //var fontAwesomeIcon = BaseFont.CreateFont(Server.MapPath(Url.Content("~/Resources/fonts/fontawesome-webfont.ttf")), BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                //Font fontAwe = new Font(fontAwesomeIcon, 8);


                var pTable1 = new Table();
                pTable1.LayoutType = TableLayoutType.FixedWidth;
                int _priorProviderID = -1;
                string _priorProviderName = "!!!!";
                double dbBalanceTotal = 0.0;
                int i = 0;

                string strEncryptionString = "0Fnvwk0qzbn6sQ+qUbGDyz0MmpBt5e3=";
                var pTableCards = new Table(/*new float[] { 150f, 200f, 70f, 160f, 120f }*/);
                pTableCards.LayoutType = TableLayoutType.FixedWidth;
                //var pTableCards = new PdfPTable(new float[] { 150f, 200f, 70f, 160f, 120f });
                string BinList = "";

                foreach (Card c in Cards)
                {
                    pTable1.LayoutType = TableLayoutType.FixedWidth;
                    var rowpTableCards = pTableCards.Rows.AddTableRow();
                    var rowpTable1 = pTable1.Rows.AddTableRow();
                    #region Get BinBase / IAFCI Info -- used based on which data is available: priority is Provider Info, Bin Base, IAFCI

                    var BinBase = c.BinBase;

                    var IAFCI = new IAFCI();

                    var Override = new BinBaseOverride();

                    if (BinBase == null)
                    {
                        BinBase = new BinBase();
                    }
                    if (IAFCI == null)
                    {
                        IAFCI = new IAFCI();
                    }
                    if (Override == null)
                    {
                        Override = new BinBaseOverride();
                    }

                    #endregion

                    #region Determine Providers Name: priority is Provider Info, Bin Base, IAFCI

                    //Determine Provider Name
                    string ProviderName = "";
                    if (c.Provider != null)
                    {
                        ProviderName = (c.RetailerName != null ? c.RetailerName : c.Provider.Name);
                    }
                    else
                    {
                        ProviderName = (string.IsNullOrEmpty(Override.IssuingOrg) ? null : Override.IssuingOrg) ??
                                        (string.IsNullOrEmpty(IAFCI.IssuerName) ? null : IAFCI.IssuerName) ??
                                        (string.IsNullOrEmpty(BinBase.IssuingOrg) ? null : BinBase.IssuingOrg) ??
                                        "Unavailable";
                    }

                    #endregion
                    Block block;
                    #region See if iteration has a new provider (each provider gets a new heading)

                    if (ProviderName != _priorProviderName)
                    {
                        #region New Provider, close out previous table if there was one (add summary, add page break)

                        if (i > 0)
                        {
                            //try
                            //{
                            //    ((Table)pTable1.Rows[5].Cells[0]).Rows[0].GetCells()[1].Phrase[0] = new Phrase(BinList, fArial14);
                            //    pTable1.Rows[5].Cells[1].
                            //}
                            //catch (Exception) { }

                            ////blank row
                            rowpTableCards = pTableCards.Rows.AddTableRow();
                            var pCellClosing = rowpTableCards.Cells.AddTableCell();
                            block = pCellClosing.Blocks.AddBlock();
                            block.InsertText("Balance Inquiry Amount:");
                            //total row
                            pCellClosing.Padding = new Thickness(0f, 0f, 0f, 5f);
                            pCellClosing.ColumnSpan = 3;
                            block.HorizontalAlignment = HorizontalAlignment.Left;

                            string Balance = "";
                            if (_priorProviderID == 0)
                            {
                                Balance = "N/A";
                            }
                            else
                            {
                                Balance = dbBalanceTotal.ToString("c");
                            }
                            pCellClosing = rowpTableCards.Cells.AddTableCell();
                            block = pCellClosing.Blocks.AddBlock();
                            block.TextProperties.FontSize = 14;
                            block.InsertText(Balance);
                            //pCellClosing = new PdfPCell(new Phrase(Balance, fArialBold14));
                            pCellClosing.Padding = new Thickness(0f, 0f, 0f, 5f);
                            pCellClosing.ColumnSpan = 2;
                            //pCellClosing.Border = 0;
                            block.HorizontalAlignment = HorizontalAlignment.Right;
                            //pTableCards.AddCell(pCellClosing);

                            //add cards table to document
                            rowpTable1 = pTable1.Rows.AddTableRow();
                            pCellClosing = rowpTable1.Cells.AddTableCell();
                            pCellClosing.Blocks.Add(pTableCards);
                            //pCellClosing = new PdfPCell(pTableCards);
                            pCellClosing.Padding = new Thickness(0);
                            //pTable1.AddCell(pCellClosing);

                            //Insert Page Break
                            editor.InsertTable(pTable1);
                            editor.InsertPageBreak();
                            pTable1 = new Table();
                            pTable1.LayoutType = TableLayoutType.FixedWidth;
                        }

                        #endregion

                        //Reset Values
                        pTableCards = new Table();//5 column
                        pTableCards.LayoutType = TableLayoutType.FixedWidth;
                        dbBalanceTotal = 0;

                        #region Provider Header (Transaction Type, Balance Inquiry, Client ID, User ID, Case #,  Timestamp)
                        rowpTable1 = pTable1.Rows.AddTableRow();
                        var pCell = rowpTable1.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 16f;
                        block.InsertText(Case.ClientGroup != null ? Case.ClientGroup.Name : "");
                        block.InsertLineBreak();
                        block.InsertText(c.Case.Client.Name);
                        block.InsertLineBreak();
                        block.InsertText((c.Case.Client.Address1 + " " + c.Case.Client.Address2).Trim());
                        block.InsertLineBreak();
                        block.InsertText(c.Case.Client.City + ", " + c.Case.Client.State + ", " + c.Case.Client.Zipcode);
                        pCell.Padding = new Thickness(0f, 0f, 0f, 10f);

                        //pCell.Border = 0;
                        block.HorizontalAlignment = HorizontalAlignment.Center;
                        //pTable1.AddCell(pCell);

                        //provider title
                        rowpTable1 = pTable1.Rows.AddTableRow();
                        pCell = rowpTable1.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 18;
                        block.InsertText(ProviderName);
                        //pCell = new PdfPCell(new Phrase(ProviderName, fArialBold16));
                        pCell.Padding = new Thickness(0f, 0f, 15f, 20f);
                        //pCell.Border = 0;
                        block.HorizontalAlignment = HorizontalAlignment.Center;
                        //pTable1.AddCell(pCell);
                        pTable1.Rows.AddTableRow();
                        var pTable2 = new Table();//2 column
                        pTable2.LayoutType = TableLayoutType.FixedWidth;
                        var rowpTable2 = pTable2.Rows.AddTableRow();
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14;
                        block.InsertText("Transaction Type:");
                        //pCell = new PdfPCell(new Phrase("Transaction Type:", fArial14));

                        pCell.Padding = new Thickness(0f, 0f, 15f, 5f);

                        block.HorizontalAlignment = HorizontalAlignment.Left;
                        //pCell.Border = 0;
                        //pTable2.AddCell(pCell);
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14;
                        block.InsertText("Balance Inquiry");
                        //pCell = new PdfPCell(new Phrase("Balance Inquiry", fArial14));
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);

                        //pCell.Border = 0;
                        //pTable2.AddCell(pCell);

                        //blank row
                        rowpTable2 = pTable2.Rows.AddTableRow();
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.InsertText("");
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.InsertText("");
                        //pCell = new PdfPCell(new Phrase(""));
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);

                        //pCell.Border = 0;
                        //pTable2.AddCell(pCell);
                        //pTable2.AddCell(pCell);

                        if (c.Case.ClientGroup != null)
                        {
                            rowpTable2 = pTable2.Rows.AddTableRow();
                            pCell = rowpTable2.Cells.AddTableCell();
                            block = pCell.Blocks.AddBlock();
                            block.TextProperties.FontSize = 14;
                            block.InsertText("Group:");

                            pCell.Padding = new Thickness(0f, 0f, 15f, 5f);

                            block.HorizontalAlignment = HorizontalAlignment.Left;
                            //pCell.Border = 0;
                            //pTable2.AddCell(pCell);
                            pCell = rowpTable2.Cells.AddTableCell();
                            block = pCell.Blocks.AddBlock();
                            block.TextProperties.FontSize = 14;
                            block.InsertText(c.Case.ClientGroup.Name);
                            pCell.Padding = new Thickness(0f, 0f, 0f, 5f);

                            //pCell.Border = 0;
                            //pTable2.AddCell(pCell);
                        }
                        rowpTable2 = pTable2.Rows.AddTableRow();
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14;
                        block.InsertText("Client:");

                        pCell.Padding = new Thickness(0f, 0f, 15f, 5f);

                        block.HorizontalAlignment = HorizontalAlignment.Left;
                        //pCell.Border = 0;
                        //pTable2.AddCell(pCell);
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14;
                        block.InsertText(c.Case.Client.Login.ToString());
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);
                        //pCell.Border = 0;
                        //pTable2.AddCell(pCell);
                        rowpTable2 = pTable2.Rows.AddTableRow();
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14;
                        block.InsertText("User:");
                        pCell.Padding = new Thickness(0f, 0f, 15f, 15f);

                        block.HorizontalAlignment = HorizontalAlignment.Left;
                        //pCell.Border = 0;
                        //pTable2.AddCell(pCell);
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14;
                        block.InsertText(c.Case.AssignedUser.DisplayName);
                        pCell.Padding = new Thickness(0f, 0f, 0f, 15f);

                        //pCell.Border = 0;
                        //pTable2.AddCell(pCell);
                        rowpTable2 = pTable2.Rows.AddTableRow();
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14;
                        block.InsertText("Case #:");

                        pCell.Padding = new Thickness(0f, 0f, 15f, 15f);

                        block.HorizontalAlignment = HorizontalAlignment.Left;
                        //pCell.Border = 0;
                        //pTable2.AddCell(pCell);
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14;
                        block.InsertText(c.Case.CaseNumber ?? "");
                        pCell.Padding = new Thickness(0f, 0f, 0f, 15f);

                        //pCell.Border = 0;
                        //pTable2.AddCell(pCell);

                        //add the list table to the main table
                        rowpTable1 = pTable1.Rows.AddTableRow();
                        pCell = rowpTable1.Cells.AddTableCell();
                        pCell.Blocks.Add(pTable2);
                        pCell.Padding = new Thickness(0f);

                        //main content - paragraph 4 header
                        rowpTable1 = pTable1.Rows.AddTableRow();
                        pCell = rowpTable1.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14;
                        block.InsertText(CurrentTime);
                        pCell.Padding = new Thickness(0f, 0f, 0f, 15f);

                        #endregion


                        #region Issuer Details (Bin, Card Type, Card Level, Isuser Name)

                        pTable2 = new Table();//2 column
                        pTable2.LayoutType = TableLayoutType.FixedWidth;
                        //Provider Details
                        rowpTable1 = pTable1.Rows.AddTableRow();
                        pCell = rowpTable1.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 15;
                        block.InsertText("Issuer Details:");
                        //pCell = new PdfPCell(new Phrase("Issuer Details:", fArialBold14));
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);

                        rowpTable2 = pTable2.Rows.AddTableRow();
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14;
                        block.InsertText("Bin Number:");

                        pCell.Padding = new Thickness(0f, 0f, 15f, 5f);

                        block.HorizontalAlignment = HorizontalAlignment.Left;

                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        BinList = c.BinBaseBIN != null ? c.BinBaseBIN : "";
                        block.TextProperties.FontSize = 14;
                        block.InsertText(c.BinBaseBIN ?? "");
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);


                        if (BinBase != null && BinBase.BIN != null && BinBase.BIN.Length > 0)
                        {
                            rowpTable2 = pTable2.Rows.AddTableRow();
                            pCell = rowpTable2.Cells.AddTableCell();
                            block = pCell.Blocks.AddBlock();
                            block.TextProperties.FontSize = 14;
                            block.InsertText("Card Type:");
                            pCell.Padding = new Thickness(0f, 0f, 15f, 5f);

                            block.HorizontalAlignment = HorizontalAlignment.Left;

                            pCell = rowpTable2.Cells.AddTableCell();
                            block = pCell.Blocks.AddBlock();
                            block.TextProperties.FontSize = 14;
                            block.InsertText(BinBase.TypeOfCard);
                            pCell.Padding = new Thickness(0f, 0f, 0f, 5f);


                            rowpTable2 = pTable2.Rows.AddTableRow();
                            pCell = rowpTable2.Cells.AddTableCell();
                            block = pCell.Blocks.AddBlock();
                            block.TextProperties.FontSize = 14;
                            block.InsertText("Card Level:");
                            pCell.Padding = new Thickness(0f, 0f, 15f, 5f);

                            block.HorizontalAlignment = HorizontalAlignment.Left;

                            pCell = rowpTable2.Cells.AddTableCell();
                            block = pCell.Blocks.AddBlock();
                            block.TextProperties.FontSize = 14;
                            block.InsertText(BinBase.Category);
                            pCell.Padding = new Thickness(0f, 0f, 0f, 5f);

                        }
                        rowpTable2 = pTable2.Rows.AddTableRow();
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14f;
                        block.InsertText("Issuer Name:");

                        pCell.Padding = new Thickness(0f, 0f, 15f, 5f);

                        block.HorizontalAlignment = HorizontalAlignment.Left;

                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14f;
                        block.InsertText(ProviderName);
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);


                        rowpTable2 = pTable2.Rows.AddTableRow();
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14f;
                        block.InsertText("Issuer Phone:");

                        pCell.Padding = new Thickness(0f, 0f, 15f, 5f);

                        block.HorizontalAlignment = HorizontalAlignment.Left;

                        string phoneNumber = "";
                        if (Override != null && Override.BIN != null && Override.BIN.Length > 0 && Override.IssuingOrgPhone != null && Override.IssuingOrgPhone.Length > 0)
                        {
                            phoneNumber = Override.IssuingOrgPhone;
                        }
                        else if (IAFCI != null && IAFCI.BIN != null && IAFCI.BIN.Length > 0 && IAFCI.PhoneNumber != null && IAFCI.PhoneNumber.Length > 0)
                        {
                            phoneNumber = IAFCI.PhoneNumber;
                        }
                        else if (BinBase != null && BinBase.BIN != null && BinBase.BIN.Length > 0 && BinBase.IssuingOrgPhone != null && BinBase.IssuingOrgPhone.Length > 0)
                        {
                            phoneNumber = BinBase.IssuingOrgPhone;
                        }
                        else if (c.Provider != null)
                        {
                            phoneNumber = (c.Provider.SubpoenaPhone != null ? "Subpoena: " + c.Provider.SubpoenaPhone : "");
                            phoneNumber += phoneNumber.Length > 0 && c.Provider.CorporatePhone != null ? ", " : "";
                            phoneNumber += c.Provider.CorporatePhone != null && c.Provider.CorporatePhone.Length > 0 ? "Corporate: " + c.Provider.CorporatePhone : "";
                        }

                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14f;
                        block.InsertText(phoneNumber);
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);

                        //add the list table to the main table
                        rowpTable1 = pTable1.Rows.AddTableRow();
                        pCell = rowpTable1.Cells.AddTableCell();
                        pCell.Blocks.Add(pTable2);
                        //pCell = new PdfPCell(pTable2);
                        pCell.Padding = new Thickness(0f);


                        //blank row
                        rowpTable1 = pTable1.Rows.AddTableRow();
                        pCell = rowpTable1.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.InsertText("");
                        //pCell = new PdfPCell(new Phrase(" "));
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);


                        #endregion


                        #region Issuer Address, Issuer Contact

                        //new table, two columns
                        pTable2 = new Table();
                        pTable2.LayoutType = TableLayoutType.FixedWidth;

                        //pTable2.WidthPercentage = 100;
                        rowpTable2 = pTable2.Rows.AddTableRow();
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 15f;
                        block.InsertText("Issuer Address:");
                        //pCell = new PdfPCell(new Phrase("Issuer Address:", fArialBold14));
                        pCell.Padding = new Thickness(0f, 0f, 5f, 5f);


                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 15f;
                        block.InsertText("Issuer Contact:");
                        //pCell = new PdfPCell(new Phrase("Issuer Contact:", fArialBold14));
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);



                        string ProviderAddress = "N/A";
                        if (c.Provider == null && IAFCI.PhysicalAddress != null)
                        {
                            ProviderAddress = IAFCI.PhysicalAddress.Replace("<br>", "\r\n").Replace("<br/>", "\r\n").Replace("<br />", "\r\n").Replace("<BR>", "\r\n").Replace("<BR/>", "\r\n").Replace("<BR />", "\r\n");
                        }
                        rowpTable2 = pTable2.Rows.AddTableRow();
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14f;
                        block.InsertText(ProviderAddress);
                        //pCell = new PdfPCell(new Phrase(ProviderAddress, fArial14));
                        pCell.Padding = new Thickness(0f, 0f, 5f, 5f);

                        string PrincipalContact = "N/A";
                        if (c.Provider == null && IAFCI.Address != null)
                        {
                            PrincipalContact = IAFCI.Address.Replace("<br>", "\r\n").Replace("<br/>", "\r\n").Replace("<br />", "\r\n").Replace("<BR>", "\r\n").Replace("<BR/>", "\r\n").Replace("<BR />", "\r\n");
                        }
                        else if (c.Provider != null)
                        {
                            PrincipalContact = (c.Provider.SubpoenaAttention != null ? c.Provider.SubpoenaAttention + "\r\n" : "") +
                                (c.Provider.SubpoenaName != null ? c.Provider.SubpoenaName + "\r\n" : "") +
                                (c.Provider.SubpoenaAddress1 != null ? c.Provider.SubpoenaAddress1 + "\r\n" : "") +
                               (c.Provider.SubpoenaAddress2 != null ? c.Provider.SubpoenaAddress2 + "\r\n" : "") +
                                (c.Provider.SubpoenaCity != null ? c.Provider.SubpoenaCity + ", " + c.Provider.SubpoenaState + " " + c.Provider.SubpoenaZip + "\r\n" : "");
                        }
                        pCell = rowpTable2.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 14f;
                        block.InsertText(PrincipalContact);
                        //pCell = new PdfPCell(new Phrase(PrincipalContact, fArial14));
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);

                        //pCell.Border = 0;
                        //pTable2.AddCell(pCell);
                        rowpTable1 = pTable1.Rows.AddTableRow();
                        pCell = rowpTable1.Cells.AddTableCell();
                        pCell.Blocks.Add(pTable2);

                        //pCell = new PdfPCell(pTable2);
                        pCell.Padding = new Thickness(0f);

                        //blank row
                        rowpTable1 = pTable1.Rows.AddTableRow();
                        pCell = rowpTable1.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.InsertText("");
                        //pCell = new PdfPCell(new Phrase(" "));
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);

                        #endregion


                        #region Card Details Table Header

                        //header
                        rowpTable1 = pTable1.Rows.AddTableRow();
                        pCell = rowpTable1.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 15f;
                        block.InsertText("Card Details");
                        pCell.Borders = new TableCellBorders(null, null, null, new Border(1f, new RgbColor(0, 0, 0)));

                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);

                        //cards table
                        rowpTableCards = pTableCards.Rows.AddTableRow();
                        pCell = rowpTableCards.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 15f;
                        block.InsertText(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, "Issuer");
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);

                        pCell = rowpTableCards.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 15f;
                        block.InsertText("Card Number");
                        //pCell = new PdfPCell(new Phrase("Card Number", fArialBold14));
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);
                        //pCell.Border = 0;
                        //pTableCards.AddCell(pCell);
                        pCell = rowpTableCards.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 15f;
                        block.InsertText("Cloned");
                        //pCell = new PdfPCell(new Phrase("Cloned", fArialBold14));
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);
                        //pCell.Border = 0;
                        //pTableCards.AddCell(pCell);
                        pCell = rowpTableCards.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 15f;
                        block.InsertText("Name");
                        //pCell = new PdfPCell(new Phrase("Name", fArialBold14));
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);
                        //pCell.Border = 0;
                        //pTableCards.AddCell(pCell);
                        pCell = rowpTableCards.Cells.AddTableCell();
                        block = pCell.Blocks.AddBlock();
                        block.TextProperties.FontSize = 15f;
                        block.InsertText("Balance");
                        //pCell = new PdfPCell(new Phrase("Balance", fArialBold14));
                        pCell.Padding = new Thickness(0f, 0f, 0f, 5f);
                        block.HorizontalAlignment = HorizontalAlignment.Right;
                        #endregion
                    }

                    #endregion

                    #region Add Card to Cards Table (row)

                    if (c.BinBaseBIN != null && c.BinBaseBIN.Length > 0 && !BinList.Contains(c.BinBaseBIN))
                    {
                        BinList += ", " + c.BinBaseBIN;
                    }

                    //Add Card
                    //card number
                    string IssuerName = ProviderName;
                    if (ProviderName.ToLower().Equals("unavailable") && c.IssuerName != null && c.IssuerName.Length > 0)
                    {
                        IssuerName = c.IssuerName;
                    }
                    rowpTableCards = pTableCards.Rows.AddTableRow();
                    var pCellCard = rowpTableCards.Cells.AddTableCell();
                    block = pCellCard.Blocks.AddBlock();
                    block.InsertText(IssuerName);
                    pCellCard.Padding = new Thickness(0f, 0f, 0f, 5f);

                    string CardNumber = "";
                    CardNumber = c.CardNumberMasked;
                    pCellCard = rowpTableCards.Cells.AddTableCell();
                    block = pCellCard.Blocks.AddBlock();
                    block.InsertText(CardNumber);
                    if (c.EntryMethod != null && c.ClonedCardParentCardId == null && c.EntryMethod.Equals("Swipe"))
                    {
                        block.InsertText(" [M]");
                    }
                    else if (c.EntryMethod != null && c.EntryMethod.Equals("Upload"))
                    {
                        block.InsertText(" [U]");
                    }
                    else if (c.EntryMethod != null && c.EntryMethod.Equals("Chip"))
                    {
                        block.InsertText(" [C]");
                    }
                    else
                    {
                        block.InsertText(" [K]");
                    }

                    pCellCard.Padding = new Thickness(0f, 0f, 0f, 5f);
                    //pTableCards.AddCell(pCellCard);

                    pCellCard = rowpTableCards.Cells.AddTableCell();
                    block = pCellCard.Blocks.AddBlock();
                    block.InsertText(c.ClonedCardParentCardId != null || c.ClonedCards.Count > 0 ? "[CL]" : "");
                    pCellCard.Padding = new Thickness(0f, 0f, 0f, 5f);
                    //pCellCard.Border = 0;
                    //pTableCards.AddCell(pCellCard);
                    pCellCard = rowpTableCards.Cells.AddTableCell();
                    block = pCellCard.Blocks.AddBlock();
                    block.TextProperties.FontSize = 14f;
                    block.InsertText(c.CardName ?? "");
                    //pCellCard = new PdfPCell(new Phrase(c.CardName, fArial14));
                    pCellCard.Padding = new Thickness(0f, 0f, 0f, 5f);

                    double dbTemp = 0.0;

                    string Amount;
                    if (double.TryParse(c.OriginalBalance.ToString(), out dbTemp))
                    {
                        Amount = dbTemp.ToString("c");
                    }
                    else
                    {
                        Amount = "N/A";
                    }
                    dbBalanceTotal += dbTemp;
                    pCellCard = rowpTableCards.Cells.AddTableCell();
                    block = pCellCard.Blocks.AddBlock();
                    block.TextProperties.FontSize = 14f;
                    block.InsertText(Amount);
                    //pCellCard = new PdfPCell(new Phrase(Amount, fArial14));
                    pCellCard.Padding = new Thickness(0f, 0f, 0f, 5f);
                    block.HorizontalAlignment = HorizontalAlignment.Right;

                    #endregion

                    //Set Current Provider
                    _priorProviderID = -1;
                    if (c.Provider != null)
                    {
                        _priorProviderID = c.ProviderID.Value;
                    }
                    _priorProviderName = ProviderName;
                    i++;
                }

                #region Close out Cards Table

                if (i > 0)
                {

                    ////blank row
                    var rowpTableCards = pTableCards.Rows.AddTableRow();
                    var pCellClosing = rowpTableCards.Cells.AddTableCell();
                    var block = pCellClosing.Blocks.AddBlock();
                    block.InsertText("");

                    //total row
                    rowpTableCards = pTableCards.Rows.AddTableRow();
                    pCellClosing = rowpTableCards.Cells.AddTableCell();
                    block = pCellClosing.Blocks.AddBlock();
                    block.TextProperties.FontSize = 14f;
                    block.InsertText("Balance Inquiry Amount:");
                    //pCellClosing = new PdfPCell(new Phrase("Balance Inquiry Amount:", fArialBold14));
                    pCellClosing.Padding = new Thickness(0f, 0f, 0f, 5f);
                    pCellClosing.ColumnSpan = 3;
                    block.HorizontalAlignment = HorizontalAlignment.Left;
                    //pTableCards.AddCell(pCellClosing);

                    string Balance = "";
                    if (_priorProviderID == 0)
                    {
                        Balance = "N/A";
                    }
                    else
                    {
                        Balance = dbBalanceTotal.ToString("c");
                    }
                    pCellClosing = rowpTableCards.Cells.AddTableCell();
                    block = pCellClosing.Blocks.AddBlock();
                    block.TextProperties.FontSize = 14f;
                    block.InsertText(Balance);
                    //pCellClosing = new PdfPCell(new Phrase(Balance, fArialBold14));
                    pCellClosing.Padding = new Thickness(0f, 0f, 0f, 5f);
                    block.HorizontalAlignment = HorizontalAlignment.Right;

                    //add cards table to document
                    var rowpTable1 = pTable1.Rows.AddTableRow();
                    pCellClosing = rowpTable1.Cells.AddTableCell();
                    pCellClosing.Blocks.Add(pTableCards);
                    pCellClosing.Padding = new Thickness(0);

                    //Insert Page Break
                    editor.InsertTable(pTable1);
                    editor.InsertPageBreak();
                    pTable1 = new Table();
                    pTable1.LayoutType = TableLayoutType.FixedWidth;
                }

                #endregion



                string fileName = "CardIssuerReport_";
                fileName = fileName + Case.CaseNumber.Replace(" ", "") + "_" + TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")).ToString("MMddyyyy") + ".pdf";

                PdfFormatProvider provider = new PdfFormatProvider();

                using (var stream = new FileStream(fileName, FileMode.OpenOrCreate))
                {
                    provider.Export(doc, stream);
                }
            }
        }
    }
}
