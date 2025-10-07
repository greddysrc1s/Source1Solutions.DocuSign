using System;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Source1Solutions.DocuSign;

class Program
{
    static void Main(string[] args)
    {
        string argsString = string.Join(" ", args);

        string connectionString = "Server=WAP-sql.viewpointdata.cloud,4316;Database=Viewpoint;User Id=ReportBuilder;Password=SourceOne@20230816;";

        string attachmentId = "1481";
        string query = "SELECT AttachmentData, AttachmentFileType FROM [VPAttachments].[dbo].[bHQAF] WHERE AttachmentID = @AttachmentID";

        using (SqlConnection conn = new SqlConnection(connectionString))
        using (SqlCommand cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@AttachmentID", attachmentId);
            conn.Open();

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    byte[] fileData = (byte[])reader["AttachmentData"];
                    string fileType = reader["AttachmentFileType"].ToString().ToLower();
                    string tempFilePath = Path.Combine(Path.GetTempPath(), $"Attachment_{attachmentId}.{fileType}");

                    File.WriteAllBytes(tempFilePath, fileData);
                    Console.WriteLine($"File saved to: {tempFilePath}");

                    // Open the file using the default application
                    Process.Start(new ProcessStartInfo(tempFilePath) { UseShellExecute = true });
                }
                else
                {
                    Console.WriteLine("No attachment found with the given ID.");
                }
            }
        }

        //string insertQuery = "INSERT INTO DocuSign_Testing_S1S (DocuSignName, RequestedDtm) VALUES (@DocuSignName, @RequestedDtm)";

        //using (SqlConnection connection = new SqlConnection(connectionString))
        //using (SqlCommand command = new SqlCommand(insertQuery, connection))
        //{
        //    command.Parameters.AddWithValue("@DocuSignName", argsString);
        //    command.Parameters.AddWithValue("@RequestedDtm", DateTime.Now);

        //    connection.Open();
        //    int rowsAffected = command.ExecuteNonQuery();
        //    Console.WriteLine($"{rowsAffected} row(s) inserted.");

        //}
    }
}
