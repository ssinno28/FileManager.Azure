namespace Azure.BlobFileManager.Models
{
    public class SummaryInfo
    {
        public long Size { get; set; }
        public int Files { get; set; }
        public int Folders { get; set; }
        public long SizeLimit { get; set; }
    }
}