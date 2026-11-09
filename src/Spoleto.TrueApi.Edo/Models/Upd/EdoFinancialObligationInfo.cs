using Spoleto.TrueApi.Edo.Enums;

namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Информация для сведений о денежном обязательстве (ИнфСведДенОбяз).
    /// </summary>
    public record EdoFinancialObligationInfo
    {
        /// <summary>
        /// Номер строки таблицы информации продавца (НомСтр).
        /// </summary>
        public required int RowNumber { get; set; }

        /// <summary>
        /// Код объекта капитального строительства федеральной адресной инвестиционной программы или код мероприятия по информатизации (КодОбъектФАИП).
        /// </summary>
        public string? ObjectFAIPCode { get; set; }

        /// <summary>
        /// Вид средств (ВидСредств).
        /// </summary>
        public required EdoFundType FundType { get; set; }

        /// <summary>
        /// Код по бюджетной классификации покупателя (КодПокБюджКласс).
        /// </summary>
        public required string BuyerBudgetClassCode { get; set; }

        /// <summary>
        /// Код цели покупателя (КодЦелиПокуп).
        /// </summary>
        public string? BuyerTargetCode { get; set; }

        /// <summary>
        /// Сумма перечисленного аванса (СумАванс), при отсутствии - 0.
        /// </summary>
        public decimal AmountAdvance { get; set; }
    }
}
