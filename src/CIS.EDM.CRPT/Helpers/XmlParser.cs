using System;
using System.Collections.Generic;
using System.Xml;
using CIS.EDM.CRPT.Models;
using CIS.EDM.Models;
using CIS.EDM.Models.Reference;

namespace CIS.EDM.CRPT.Helpers
{
    internal class XmlParser
    {
        /// <summary>
        /// Парсинг XML документа продавца для заполнения информации в файле покупателя.
        /// </summary>
        internal static SellerDocumentInfo SellerInfoFromXml(SignedDocumentInfo sellerDocumentInfo)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(sellerDocumentInfo.Content);

            var fileHeaderNode = (XmlElement)xmlDocument.GetElementsByTagName("Файл")[0];
            var sellerInfo = new SellerDocumentInfo
            {
                FileId = fileHeaderNode.GetAttribute("ИдФайл")
            };

            var documentNode = (XmlElement)fileHeaderNode.GetElementsByTagName("Документ")[0];
            sellerInfo.DateCreation = documentNode.GetAttribute("ДатаИнфПр");
            sellerInfo.TimeCreation = documentNode.GetAttribute("ВремИнфПр");
            sellerInfo.DocumentName = documentNode.GetAttribute("НаимДокОпр");
            sellerInfo.Function = Enum.Parse<UniversalTransferDocumentFunction>(documentNode.GetAttribute("Функция"));

            var documentInfoNode = (XmlElement)documentNode.GetElementsByTagName("СвСчФакт")[0];

            sellerInfo.DocumentDate = documentInfoNode.GetAttribute("ДатаСчФ");
            sellerInfo.DocumentNumber = documentInfoNode.GetAttribute("НомерСчФ");

            sellerInfo.EdsBodyList = new List<string> { sellerDocumentInfo.DetachedSignature };

            var revisionNodeList = documentInfoNode.GetElementsByTagName("ИспрСчФ");
            foreach (XmlElement revisionNode in revisionNodeList)
            {
                sellerInfo.IsUPDi = revisionNode.HasAttribute("НомИспрСчФ");
            }

            return sellerInfo;
        }
    }
}
