namespace TeensyRom.Cli.Storage.Entities
{
    public interface IViewableItem : IFileItem 
    {
        List<ViewableItemImage> Images { get; }
        
    }
}
