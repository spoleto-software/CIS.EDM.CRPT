namespace Spoleto.TrueApi.Edo.Enums
{
    /// <summary>
    /// Итог приемки товаров (работ, услуг, прав), атрибут КодИтога информации покупателя.
    /// </summary>
    public enum EdoOperationNameCode
    {
        /// <summary>
        /// Товары (работы, услуги, права) приняты без расхождений (претензий).
        /// </summary>
        Accepted = 1,

        /// <summary>
        /// Товары (работы, услуги, права) приняты с расхождениями (претензией).
        /// </summary>
        AcceptedWithDiscrepancies = 2,

        /// <summary>
        /// Товары (работы, услуги, права) не приняты. Не допускается в ответ на УПДи.
        /// </summary>
        NotAccepted = 3
    }
}
