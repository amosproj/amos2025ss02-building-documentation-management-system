namespace BUILD.ING.Entities
{
    public class BuildingDocumentRelation
    {
        public int BuildingId { get; set; }
        public Building Building { get; set; }

        public int DocumentId { get; set; }
        public Document Document { get; set; }

        public string RelationType { get; set; }
    }
}
