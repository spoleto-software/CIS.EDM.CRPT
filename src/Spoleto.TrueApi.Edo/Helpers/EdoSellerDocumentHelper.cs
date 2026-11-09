using System.Xml.Linq;
using Spoleto.TrueApi.Edo.Enums;
using Spoleto.TrueApi.Edo.Models;

namespace Spoleto.TrueApi.Edo.Helpers
{
    /// <summary>
    /// Разбор файла информации продавца УПД / УПДи формата 970 (приказ ФНС № ЕД-7-26/970@).
    /// </summary>
    internal static class EdoSellerDocumentHelper
    {
        /// <summary>
        /// Сведения из файла информации продавца.
        /// </summary>
        public static EdoSellerDocumentInfo ReadSellerDocumentInfo(EdoSignedDocumentInfo document)
        {
            var xml = XDocument.Parse(document.Content);

            var fileElement = xml.Root;
            if (fileElement?.Name.LocalName != "Файл")
            {
                throw new InvalidDataException($"Файл {document.FileName}: корневой элемент «{fileElement?.Name.LocalName}», ожидался «Файл».");
            }

            var documentElement = GetElement(document, fileElement, "Документ");
            var invoiceElement = GetElement(document, documentElement, "СвСчФакт");

            return new EdoSellerDocumentInfo
            {
                FileId = GetAttribute(document, fileElement, "ИдФайл"),
                DateCreation = GetAttribute(document, documentElement, "ДатаИнфПр"),
                TimeCreation = GetAttribute(document, documentElement, "ВремИнфПр"),
                DocumentName = documentElement.Attribute("НаимДокОпр")?.Value,
                Function = ParseFunction(document, GetAttribute(document, documentElement, "Функция")),
                DocumentNumber = invoiceElement.Attribute("НомерДок")?.Value,
                DocumentDate = GetAttribute(document, invoiceElement, "ДатаДок"),
                DocumentUid = documentElement.Attribute("УИД")?.Value,
                IsUPDi = invoiceElement.Element("ИспрДок")?.Attribute("НомИспр") != null,
                Signature = document.Signature
            };
        }

        private static XElement GetElement(EdoSignedDocumentInfo document, XElement parent, string name)
            => parent.Element(name)
                ?? throw new InvalidDataException($"Файл {document.FileName}: в элементе «{parent.Name.LocalName}» нет элемента «{name}».");

        private static string GetAttribute(EdoSignedDocumentInfo document, XElement element, string name)
            => element.Attribute(name)?.Value
                ?? throw new InvalidDataException($"Файл {document.FileName}: у элемента «{element.Name.LocalName}» нет атрибута «{name}».");

        private static EdoDocumentFunction ParseFunction(EdoSignedDocumentInfo document, string value) => value switch
        {
            "СЧФ" => EdoDocumentFunction.Invoice,
            "СЧФДОП" => EdoDocumentFunction.InvoiceAndPrimaryDocument,
            "ДОП" => EdoDocumentFunction.PrimaryDocument,
            "СвРК" => EdoDocumentFunction.CommissionSale,
            "СвЗК" => EdoDocumentFunction.CommissionPurchase,
            _ => throw new InvalidDataException($"Файл {document.FileName}: неизвестная функция документа «{value}».")
        };
    }
}
