namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Сведения о передаче товаров, результатов работ, имущественных прав (СвПер).
    /// </summary>
    public record EdoTransferDetails
    {
        /// <summary>
        /// Содержание операции (СодОпер).
        /// </summary>
        public string OperationName { get; set; } = "Товары переданы";

        /// <summary>
        /// Вид операции (ВидОпер).
        /// </summary>
        public string? OperationType { get; set; }

        /// <summary>
        /// Дата отгрузки товаров, передачи результатов работ, имущественных прав (ДатаПер).
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        /// Дата начала периода оказания услуг, выполнения работ, поставки товаров (ДатаНачПер).
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Дата окончания периода оказания услуг, выполнения работ, поставки товаров (ДатаОконПер).
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Основания передачи, например, договор (ОснПер). Если не заданы, формируется БезДокОснПер.
        /// </summary>
        public List<EdoDocumentDetails>? TransferDocuments { get; set; }

        /// <summary>
        /// Сведения о лице, передавшем товар (СвЛицПер).
        /// </summary>
        public EdoSenderPerson? SenderPerson { get; set; }

        /// <summary>
        /// Транспортировка (Тран).
        /// </summary>
        public EdoTransportation? Transportation { get; set; }

        /// <summary>
        /// Сведения о передаче вещи, изготовленной по договору подряда (СвПерВещи).
        /// </summary>
        public EdoCreatedThingTransferInfo? CreatedThingInfo { get; set; }

        public override string ToString() => OperationName;
    }
}
