using Source1Solutions.DocuSign.Sync;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        SyncProcess syncProcess = new SyncProcess(args);
        string result = syncProcess.Sync() ? "Success" : "Failed";

    }
}