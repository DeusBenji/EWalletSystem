using AutoMapper;
using IdentityService.Domain.Model;
using IdentityService.Application.DTOs;

namespace IdentityService
{
    public class MappingProfile : Profile
    {
        public MappingProfile() 
        {
            CreateMap<Account, AccountDto>();
            CreateMap<AccountDto, Account>();

            CreateMap<MitID_Account, MitIdAccountDto>();
            CreateMap<MitIdAccountDto, MitID_Account>();

        }


    }
}
