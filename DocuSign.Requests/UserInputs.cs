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

    }
}
