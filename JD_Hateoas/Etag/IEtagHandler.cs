namespace JD_Hateoas.Etag
{
    public interface IEtagHandlerFeature
    {
        bool NoneMatch(IEtaggable entity);
    }
}
