using DocuSign.eSign.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocuSign.Requests
{
    public class UserInputs
    {
        public string ConnectionString { get; set; }

        public string AttachmentDBConnection { get; set; }

        public string DocuSignClientId { get; set; }

        public string DocuSignAuthServer { get; set; }

        public string DocuSignImpersonatedUserID { get; set; }

        public string DocuSignPrivateKeyFile { get; set; }

        public string DocuSignAccountID { get; set; }

        public string DocuSignApiBaseUrl { get; set; }

        // Signature Anchor Settings
        public string SignatureAnchorPrimaryText { get; set; } = "Vendor Signature:";

        public string SignatureAnchorSecondaryPattern { get; set; } = "Signer {0} Signature:";

        public string SignatureAnchorXOffset { get; set; } = "100";

        public string SignatureAnchorYOffset { get; set; } = "0";

        public string SignatureAnchorUnits { get; set; } = "pixels";

        public string SignatureAnchorIgnoreIfNotPresent { get; set; } = "true";

        public string SignatureAnchorCaseSensitive { get; set; } = "false";
    }
}
