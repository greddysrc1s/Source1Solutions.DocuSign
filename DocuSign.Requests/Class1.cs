using System.ComponentModel;

namespace DocuSign.Requests
{
    public class Class1
    {

    }

    public enum ExamplesApiType
    {
        /// <summary>
        /// Rooms API
        /// </summary>
        [Description("reg")]
        Rooms = 0,

        /// <summary>
        /// ESignature API
        /// </summary>
        [Description("eg")]
        ESignature = 1,

        /// <summary>
        /// Click API
        /// </summary>
        [Description("ceg")]
        Click = 2,

        /// <summary>
        /// Monitor API
        /// </summary>
        [Description("meg")]
        Monitor = 3,

        /// <summary>
        /// Admin API
        /// </summary>
        [Description("aeg")]
        Admin = 4,

        /// <summary>
        /// Connect API
        /// </summary>
        [Description("con")]
        Connect = 5,

        /// <summary>
        /// Web Forms API
        /// </summary>
        [Description("web")]
        WebForms = 6,

        /// <summary>
        /// Notary API
        /// </summary>
        [Description("neg")]
        Notary = 7,

        /// <summary>
        /// Navigator
        /// </summary>")]
        [Description("nav")]
        Navigator = 8,
    }
}
