namespace BUILD.ING.Models
{
    public class DocumentTagRelation
    {
        public int DocumentId { get; set; }
        public Document Document { get; set; }

        public int TagId { get; set; }
        public DocumentTag Tag { get; set; }
    }
}