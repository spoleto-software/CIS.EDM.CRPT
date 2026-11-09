namespace Spoleto.TrueApi.Edo.Enums
{
    /// <summary>
    /// Вид средств (ВидСредств): источник средств, за счет которого должна быть произведена кассовая выплата.
    /// </summary>
    public enum EdoFundType
    {
        /// <summary>
        /// Средства бюджета.
        /// </summary>
        Budget = 1,

        /// <summary>
        /// Средства дополнительного бюджетного финансирования.
        /// </summary>
        AdditionalBudget = 3,

        /// <summary>
        /// Средства для финансирования мероприятий по оперативно-розыскной деятельности.
        /// </summary>
        OperationalSearchActivity = 4,

        /// <summary>
        /// Средства, поступающие во временное распоряжение казенных учреждений.
        /// </summary>
        StateInstitutionsTemporary = 5,

        /// <summary>
        /// Средства юридических лиц.
        /// </summary>
        LegalEntity = 6
    }
}
