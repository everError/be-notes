using System.ComponentModel.DataAnnotations;

namespace data_service.Models;

public class Record
{
    public int Id { get; set; }

    public int Count { get; set; }

    [ConcurrencyCheck]
    public Guid Version { get; set; } = Guid.NewGuid();
}


