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
			var getUserById = nameof(Controllers.UsersController.GetUserByIdAsync);

            config = new MapperConfiguration(cfg =>
            {
				cfg.CreateMap<UserDto, UserRest>()
                .ForMember(dest => dest.Self, opt => opt.MapFrom(src =>
                    Link.To(getUserById, new { userId = src.Id })));
            });

            //config.AssertConfigurationIsValid();
        }
    }
}
