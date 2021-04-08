using JDWorldAPI.Models;
using JD_Hateoas.Models;
using JD_Hateoas.Form;
using AutoMapper;

namespace JDWorldAPI.Mapping
{
    public class MappingProfile : Profile
    {
        public MapperConfiguration config;

        public MappingProfile()
        {
            var getWorldById = nameof(Controllers.WorldsController.GetWorldByIdAsync);
            var createResidentForWorld = nameof(Controllers.WorldsController.CreateResidentForWorldAsync);
            var getResidentById = nameof(Controllers.ResidentsController.GetResidentByIdAsync);
			var getUserById = nameof(Controllers.UsersController.GetUserByIdAsync);
			var deleteResidentById = nameof(Controllers.ResidentsController.DeleteResidentByIdAsync);

            config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<WorldDto, WorldRest>()
                .ForMember(dest => dest.Self, opt => opt.MapFrom(src =>
                    Link.To(getWorldById, new { worldId = src.Id })))
                .ForMember(dest => dest.Assign, opt => opt.MapFrom(src =>
                    FormMetadata.FromModel(
                        new ResidentForm(),
                        Link.ToForm(
                            createResidentForWorld,
                            new { worldId = src.Id },
                            Link.PostMethod,
                            Form.CreateRelation))));

                cfg.CreateMap<ResidentDto, ResidentRest>()
                .ForMember(dest => dest.Self, opt => opt.MapFrom(src =>
                    Link.To(getResidentById, new { residentId = src.Id })))
                .ForMember(dest => dest.Cancel, opt => opt.MapFrom(src =>
                    new Link
                    {
                        RouteName = deleteResidentById,
                        RouteValues = new { residentId = src.Id },
                        Method = Link.DeleteMethod
                    }));

				cfg.CreateMap<UserDto, UserRest>()
                .ForMember(dest => dest.Self, opt => opt.MapFrom(src =>
                    Link.To(getUserById, new { userId = src.Id })));
            });

            //config.AssertConfigurationIsValid();
        }
    }
}
