namespace Spoleto.TrueApi.Edo.Enums
{
    /// <summary>
    /// Категория товаров в документе: признаки N3-N6 в имени файла ON_NSCHFDOPPR_…_N1_N2_N3_N4_N5_N6_N7.
    /// </summary>
    /// <remarks>
    /// Признака прослеживаемости (N2) нет: «ЭДО Лайт» не принимает УПД с прослеживаемыми товарами.
    /// </remarks>
    public enum EdoGoodsCategory
    {
        /// <summary>
        /// Товары, подлежащие маркировке (N3).
        /// </summary>
        Marked,

        /// <summary>
        /// Алкогольная продукция, подлежащая маркировке (N4).
        /// </summary>
        Alcohol,

        /// <summary>
        /// Табачная продукция, сырье, никотинсодержащая продукция и никотиновое сырье (N5).
        /// </summary>
        Tobacco,

        /// <summary>
        /// Нефтепродукты (N6).
        /// </summary>
        Oil
    }
}
