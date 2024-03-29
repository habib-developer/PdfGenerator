using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PdfGenerator
{
    public partial class BinBaseOverride
    {
        public BinBaseOverride()
        {
        }
        [Key]
        public string BIN { get; set; }
        public string CardBrand { get; set; }
        public string IssuingOrg { get; set; }
        public string TypeOfCard { get; set; }
        public string Category { get; set; }
        public string IssuingCountryName { get; set; }
        public string IssuingCountryCodeA2 { get; set; }
        public string IssuingCountryCodeA3 { get; set; }
        public string IssuingCountryISONumber { get; set; }
        public string IssuingOrgWebsite { get; set; }
        public string IssuingOrgPhone { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset ModifiedDate { get; set; } 
	}
}
