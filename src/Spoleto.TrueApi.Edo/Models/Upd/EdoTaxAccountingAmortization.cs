namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Данные налогового учета для расчета амортизации по передаваемому основному средству, бывшему в употреблении (НалУчАморт).
    /// </summary>
    public record EdoTaxAccountingAmortization
    {
        /// <summary>
        /// Амортизационная группа (АмГруппа).
        /// </summary>
        public required string AmortizationGroup { get; set; }

        /// <summary>
        /// Код вида основных фондов по ОКОФ (КодОКОФ).
        /// </summary>
        public required string OkofCode { get; set; }

        /// <summary>
        /// Установленный продавцом срок полезного использования (СрПолИспОС).
        /// </summary>
        public required string EstablishedUsefulLife { get; set; }

        /// <summary>
        /// Фактический срок использования продавцом, в месяцах (ФактСрокИсп).
        /// </summary>
        public required string ActualUsefulLifeMonths { get; set; }
    }
}
