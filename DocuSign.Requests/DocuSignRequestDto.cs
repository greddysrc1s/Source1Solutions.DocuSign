using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocuSign.Requests
{
    public class DocuSignRequestDto
    {
        public List<SignerDto> Signers { get; set; } = new List<SignerDto>();
        public List<AttachmentDto> SelectedAttachments { get; set; } = new List<AttachmentDto>();
        public DateTime RequestDateTime { get; set; } = DateTime.Now;
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        public string RequestFrom { get; set; } = string.Empty;

        public string Key_1 { get; set; } = string.Empty;

        public string Key_2 { get; set; } = string.Empty;
    }

    public class SignerDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int SignerOrder { get; set; }
    }

    public class AttachmentDto
    {
        public string AttachmentID { get; set; } = string.Empty;
        public string OrigFileName { get; set; } = string.Empty;
        public string FormName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AddedBy { get; set; } = string.Empty;
        public DateTime AddDate { get; set; }
        public string UniqueAttchID { get; set; } = string.Empty;
    }
}
