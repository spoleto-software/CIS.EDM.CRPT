using System;
using CIS.EDM.Models.Common.Reference;

namespace CIS.EDM.CRPT.Extensions
{
    internal static class TaxRateExtension
    {
        /// <summary>
        /// Получить название налоговой ставки.
        /// </summary>
        public static string GetTaxRateName(this TaxRate taxRate)
            => taxRate switch
            {
                TaxRate.WithoutVat => "без НДС",

                TaxRate.Zero => "0%",

                TaxRate.FivePercent => "5%",

                TaxRate.SevenPercent => "7%",

                TaxRate.TenPercent => "10%",

                TaxRate.EighteenPercent => "18%",

                TaxRate.TwentyPercent => "20%",

                TaxRate.TenFraction => "10/110",

                TaxRate.EighteenFraction => "18/118",

                TaxRate.TwentyFraction => "20/120",

                TaxRate.TaxedByAgent => "НДС исчисляется налоговым агентом",

                _ => throw new NotImplementedException()
            };
    }
}
