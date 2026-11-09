namespace Spoleto.TrueApi.Edo.Enums
{
    /// <summary>
    /// Налоговая ставка (НалСт, графа 7 счета-фактуры).
    /// </summary>
    public enum EdoTaxRate
    {
        /// <summary>
        /// «без НДС».
        /// </summary>
        WithoutVat,

        /// <summary>
        /// «0%».
        /// </summary>
        Zero,

        /// <summary>
        /// «5%».
        /// </summary>
        FivePercent,

        /// <summary>
        /// «7%».
        /// </summary>
        SevenPercent,

        /// <summary>
        /// «10%».
        /// </summary>
        TenPercent,

        /// <summary>
        /// «18%».
        /// </summary>
        EighteenPercent,

        /// <summary>
        /// «20%».
        /// </summary>
        TwentyPercent,

        /// <summary>
        /// «10/110».
        /// </summary>
        TenFraction,

        /// <summary>
        /// «18/118».
        /// </summary>
        EighteenFraction,

        /// <summary>
        /// «20/120».
        /// </summary>
        TwentyFraction,

        /// <summary>
        /// «НДС исчисляется налоговым агентом».
        /// </summary>
        TaxedByAgent,

        /// <summary>
        /// «22/122».
        /// </summary>
        TwentyTwoFraction,

        /// <summary>
        /// «22%».
        /// </summary>
        TwentyTwoPercent
    }
}
