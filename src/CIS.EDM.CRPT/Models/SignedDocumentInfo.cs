namespace CIS.EDM.CRPT.Models
{
	/// <summary>
	/// Тело документа + его открепленная подпись.
	/// </summary>
	public class SignedDocumentInfo
	{
		/// <summary>
		/// Тело документа.
		/// </summary>
		public string Content { get; set; }

		/// <summary>
		/// Открепленная цифровую подпись документа. 
		/// </summary>
		public string DetachedSignature { get; set; }
	}
}
