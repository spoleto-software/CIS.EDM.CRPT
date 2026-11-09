namespace Spoleto.TrueApi.Edo.Enums
{
    /// <summary>
    /// Справочник «Типы универсальных документов».
    /// </summary>
    public enum EdoDocumentType
    {
        /// <summary>
        /// Уведомление об уточнении (DP_UVUTOCH).
        /// </summary>
        ClarificationNotice = 110,

        /// <summary>
        /// Квитанция подтверждения даты получения (DP_PDPOL).
        /// </summary>
        ConfirmationOfReceiptDate = 111,

        /// <summary>
        /// Квитанция подтверждения даты отправки (DP_PDOTPR).
        /// </summary>
        ConfirmationOfShipmentDate = 112,

        /// <summary>
        /// Квитанция извещение о получении файла продавцом (DP_IZVPOL).
        /// </summary>
        NotificationOfReceiptOfFileBySeller = 113,

        /// <summary>
        /// Квитанция предложения об аннулировании документа (DP_PRANNUL).
        /// </summary>
        ProposalForCancellationOfDocument = 114,

        /// <summary>
        /// Квитанция извещение о получении файла покупателем (DP_IZVPOL).
        /// </summary>
        NotificationOfReceiptOfFileByBuyer = 115,

        /// <summary>
        /// УКД с функцией ДИС (корректировка накладной).
        /// </summary>
        UKDWithDISWaybillCorrection = 200,

        /// <summary>
        /// УКД с функцией ДИС информация покупателя (корректировка накладной).
        /// </summary>
        UKDWithDISBuyerInformationWaybillCorrection = 201,

        /// <summary>
        /// УКД с функцией КСЧФ (корректировка счет-фактуры).
        /// </summary>
        UKDWithKSCHFInvoiceCorrection = 202,

        /// <summary>
        /// УКД с функцией КСЧФДИС (корректировка счет-фактуры+накладная).
        /// </summary>
        UKDWithKSCHFInvoiceWaybillCorrection = 204,

        /// <summary>
        /// УКД с функцией КСЧФДИС информация покупателя (корректировка счет-фактуры+накладная).
        /// </summary>
        UKDWithKSCHFBuyerInformationInvoiceWaybillCorrection = 205,

        /// <summary>
        /// УКД(и) с функцией ДИС (Накладная).
        /// </summary>
        UKDiWithDISWaybill = 400,

        /// <summary>
        /// УПД с функцией ДОП (Накладная).
        /// </summary>
        UPDWithDOPWaybill = 500,

        /// <summary>
        /// УПД с функцией ДОП информация покупателя (Накладная).
        /// </summary>
        UPDWithDOPBuyerInformationWaybill = 501,

        /// <summary>
        /// УПД с функцией СЧФ (Счёт-фактура).
        /// </summary>
        UPDWithSCHFInvoice = 502,

        /// <summary>
        /// УПД с функцией СЧФДОП (Счёт-фактура+Накладная).
        /// </summary>
        UPDWithSCHFDOPInvoiceWaybill = 504,

        /// <summary>
        /// УПД с функцией СЧФДОП информация покупателя (Счёт-фактура+Накладная).
        /// </summary>
        UPDWithSCHFDOPBuyerInformationInvoiceWaybill = 505,

        /// <summary>
        /// УПД с функцией ДОП (Накладная) (ЕД-7-26/970@).
        /// </summary>
        UPD970WithDOPWaybill = 520,

        /// <summary>
        /// УПД с функцией ДОП титул покупателя (Накладная) (ЕД-7-26/970@).
        /// </summary>
        UPD970WithDOPBuyerTitleWaybill = 521,

        /// <summary>
        /// УПД с функцией СЧФ (Счёт-фактура) (ЕД-7-26/970@).
        /// </summary>
        UPD970WithSCHFInvoice = 522,

        /// <summary>
        /// УПД с функцией СЧФДОП (Счёт-фактура+Накладная) (ЕД-7-26/970@).
        /// </summary>
        UPD970WithSCHFDOPInvoiceWaybill = 524,

        /// <summary>
        /// УПД с функцией СЧФДОП титул покупателя (Счёт-фактура+Накладная) (ЕД-7-26/970@).
        /// </summary>
        UPD970WithSCHFDOPBuyerTitleInvoiceWaybill = 525,

        /// <summary>
        /// УКД ДИС (Накладная).
        /// </summary>
        UKDDISWaybill = 600,

        /// <summary>
        /// УКД КСЧФ (Корректировочный счёт-фактура).
        /// </summary>
        UKDKSCHFInvoiceCorrection = 602,

        /// <summary>
        /// УКД КСФДИС (Корректировочный счёт-фактура+Документ об изменении стоимости).
        /// </summary>
        UKDKSFDISInvoiceCorrectionPriceChangeDocument = 604,

        /// <summary>
        /// УКД(и) ДИС (Накладная).
        /// </summary>
        UKDiDISWaybill = 700,

        /// <summary>
        /// УКД(и) КСЧФ (Корректировочный счёт-фактура).
        /// </summary>
        UKDiKSCHFInvoiceCorrection = 702,

        /// <summary>
        /// УКД(и) КСФДИС (Корректировочный счёт-фактура+Документ об изменении стоимости).
        /// </summary>
        UKDiKSFDISInvoiceCorrectionPriceChangeDocument = 704,

        /// <summary>
        /// УПДи с функцией ДОП (Накладная исправленная).
        /// </summary>
        UPDiWithDOPWaybillCorrected = 800,

        /// <summary>
        /// УПДи с функцией ДОП информация покупателя (Накладная исправленная).
        /// </summary>
        UPDiWithDOPBuyerInformationWaybillCorrected = 801,

        /// <summary>
        /// УПДи с функцией СЧФ (Счёт-фактура).
        /// </summary>
        UPDiSCHFInvoice = 802,

        /// <summary>
        /// УПДи с функцией СЧФДОП (Счёт-фактура исправленный + Накладная).
        /// </summary>
        UPDiWithSCHFDOPInvoiceCorrectedWaybill = 804,

        /// <summary>
        /// УПДи с функцией СЧФДОП информация покупателя (Счёт-фактура исправленный + Накладная).
        /// </summary>
        UPDiWithSCHFDOPBuyerInformationInvoiceCorrectedWaybill = 805,

        /// <summary>
        /// УПДи с функцией ДОП (Накладная исправленная) (ЕД-7-26/970@).
        /// </summary>
        UPDi970WithDOPWaybillCorrected = 820,

        /// <summary>
        /// УПДи с функцией ДОП титул покупателя (Накладная исправленная) (ЕД-7-26/970@).
        /// </summary>
        UPDi970WithDOPBuyerTitleWaybillCorrected = 821,

        /// <summary>
        /// УПДи с функцией СЧФ (Счёт-фактура) (ЕД-7-26/970@).
        /// </summary>
        UPDi970SCHFInvoice = 822,

        /// <summary>
        /// УПДи с функцией СЧФДОП (Счёт-фактура исправленный + Накладная) (ЕД-7-26/970@).
        /// </summary>
        UPDi970WithSCHFDOPInvoiceCorrectedWaybill = 824,

        /// <summary>
        /// УПДи с функцией СЧФДОП титул покупателя (Счёт-фактура исправленный + Накладная) (ЕД-7-26/970@).
        /// </summary>
        UPDi970WithSCHFDOPBuyerTitleInvoiceCorrectedWaybill = 825
    }
}
