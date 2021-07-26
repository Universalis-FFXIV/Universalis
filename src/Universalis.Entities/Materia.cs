using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities
{
    public class Materia
    {
        [BsonElement("slotID")]
        public uint SlotId { get; set; }

        [BsonElement("materiaID")]
        public uint MateriaId { get; set; }
    }
}