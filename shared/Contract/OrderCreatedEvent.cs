namespace shared.Contract;

public class OrderCreatedEvent
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
