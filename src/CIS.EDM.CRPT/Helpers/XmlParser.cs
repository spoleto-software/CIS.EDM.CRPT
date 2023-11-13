using System;
using System.Collections.Generic;
using System.Xml;
using CIS.EDM.Helpers;
using CIS.EDM.Models.Buyer;
using CIS.EDM.Models.Reference;

namespace CIS.EDM.CRPT.Helpers
{
    internal class XmlParser
    {
        /// <summary>
        /// Парсинг XML документа продавца для заполнения информации в файле покупателя.
        /// </summary>
        internal static SellerDocumentInfo SellerInfoFromXml(string sellerDocumentBody, string certificateThumbprint)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(sellerDocumentBody);

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

            var contentIn64 = HttpHelper.ConvertToBase64(sellerDocumentBody, XmlHelper.DefaultEncoding);
            var signature = documentNode.GetAttribute("ЭП");//Cryptography.CryptographyHelper.SignBase64Data(contentIn64, detached: true, thumbprint: certificateThumbprint);
            sellerInfo.EdsBodyList = new List<string> { signature };

            var revisionNodeList = documentInfoNode.GetElementsByTagName("ИспрСчФ");
            foreach (XmlElement revisionNode in revisionNodeList)
            {
                sellerInfo.IsUPDi = revisionNode.HasAttribute("НомИспрСчФ");
            }

            return sellerInfo;
        }
    }
}
