using System.ComponentModel.DataAnnotations;

namespace Universalis.Entities
{
    public class Materia
    {
        [Key]
        public string InternalId { get; set; }

        public uint SlotId { get; set; }

        public uint MateriaId { get; set; }
    }
}